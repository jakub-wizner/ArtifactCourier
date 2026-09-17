using ArtifactCourier.Effects;
using ArtifactCourier.Enemies;
using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Traffic
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(AudioSource))]
    public sealed class TrafficVehicle : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float turnRate = 200f;
        [SerializeField] private float bumpDamage = 4f;
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private float blockedTimeBeforeUnstick = 1.0f;
        [SerializeField] private float separationSeconds = 0.45f;
        [SerializeField] private float separationSpeed = 3.2f;
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioClip hornClip;
        [SerializeField] private AudioClip destructionClip;
        [SerializeField] private Sprite[] destructionFrames;

        private Rigidbody2D body;
        private AudioSource audioSource;
        private int waypointIndex;
        [SerializeField] private int initialWaypoint;
        private int currentHealth;
        private float nextHornTime;
        private float blockedTimer;
        private float contactTimer;
        private float separatingUntil;
        private Vector2 separationDirection;
        private bool destroyed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0.25f;
            currentHealth = maxHealth;
            audioSource = GetComponent<AudioSource>();

            Collider2D vehicleCollider = GetComponent<Collider2D>();
            if (vehicleCollider != null)
            {
                PhysicsMaterial2D lowFriction = new("Traffic Low Friction")
                {
                    friction = 0f,
                    bounciness = 0.08f
                };
                vehicleCollider.sharedMaterial = lowFriction;
            }
        }

        public void SetRoute(Transform[] points,int start=0) { waypoints=points; initialWaypoint=(start+1)%points.Length; waypointIndex=initialWaypoint; }
        private TrafficIntent intent;
        private CityRoadNetwork network;
        private static readonly System.Collections.Generic.List<TrafficVehicle> active = new();
        private void OnEnable(){if(!active.Contains(this))active.Add(this);}
        private void OnDisable(){active.Remove(this);if(ownedJunction>=0 && junctionOwners.TryGetValue(ownedJunction,out var owner)&&owner==this)junctionOwners.Remove(ownedJunction);}
        private void OnCollisionExit2D(Collision2D collision){contactTimer=0;}
        private void OnCollisionStay2D(Collision2D collision)
        {
            var other=collision.collider.GetComponentInParent<TrafficVehicle>();
            if(other!=null && Time.time>=separatingUntil && body.linearVelocity.sqrMagnitude<.1f && GetInstanceID()>other.GetInstanceID())
            {contactTimer+=Time.fixedDeltaTime;if(contactTimer>1.2f){contactTimer=0;BeginRecovery();}}
        }
        private static readonly System.Collections.Generic.Dictionary<int,TrafficVehicle> junctionOwners=new();
        private int ownedJunction=-1;
        private float junctionLease;
        private bool JunctionWait()
        {
            if(network==null || network.junctions==null)return false;
            if(ownedJunction>=0)
            {
                if(Vector2.Distance(body.position,network.junctions[ownedJunction])>6.5f || Time.time>junctionLease)
                {if(junctionOwners.TryGetValue(ownedJunction,out var owner)&&owner==this)junctionOwners.Remove(ownedJunction);ownedJunction=-1;}
                else return false;
            }
            for(int i=0;i<network.junctions.Length;i++)
            {
                Vector2 delta=network.junctions[i]-body.position;
                if(delta.magnitude>6f || Vector2.Dot(delta,transform.up)<-1f)continue;
                if(junctionOwners.TryGetValue(i,out var owner)&&owner!=null&&owner!=this)return delta.magnitude>2f;
                junctionOwners[i]=this;ownedJunction=i;junctionLease=Time.time+7f;return false;
            }
            return false;
        }
        private void BeginRecovery()
        {
            blockedTimer=0;separatingUntil=Time.time+.8f;
            separationDirection=(-(Vector2)transform.up+(Vector2)transform.right*.35f).normalized;
        }
        private readonly RaycastHit2D[] sensed = new RaycastHit2D[12];
        private void Start()
        {
            waypointIndex=initialWaypoint;
            intent=GetComponent<TrafficIntent>();
            network=FindFirstObjectByType<CityRoadNetwork>();
            audioSource.clip = engineLoop;
            audioSource.loop = true;
            audioSource.volume = 0.16f;
            audioSource.spatialBlend = 0.65f;
            if (engineLoop != null) audioSource.Play();
            nextHornTime = Time.time + Random.Range(8f, 18f);
        }

        private void FixedUpdate()
        {
            if (destroyed || waypoints == null || waypoints.Length == 0) return;

            // Recovery must execute BEFORE traffic sensing, otherwise mutual blockers never separate.
            if(Time.time<separatingUntil){body.linearVelocity=separationDirection*separationSpeed;return;}
            bool red=false;Vector2 heading=transform.up;
            foreach(var signal in TrafficSignal.Active)if(signal.Stops(body.position,heading)){red=true;break;}
            bool junction=!red&&JunctionWait();bool blocked=false;TrafficVehicle blocker=null;
            int count=Physics2D.CircleCastNonAlloc(body.position+heading*1.1f,.38f,heading,sensed,2f+speed*.2f);
            for(int i=0;i<count;i++)
            {
                var collider=sensed[i].collider;
                if(collider==null || collider.isTrigger || collider.attachedRigidbody==body)continue;
                var other=collider.GetComponentInParent<TrafficVehicle>();
                if(other!=null){blocked=true;blocker=other;break;}
            }
            bool stop=red||junction||blocked;
            Vector2 nextDirection=(Vector2)waypoints[(waypointIndex+2)%waypoints.Length].position-body.position;
            intent?.Show(Vector2.SignedAngle(heading,nextDirection),stop);
            if(stop)
            {
                body.linearVelocity=Vector2.MoveTowards(body.linearVelocity,Vector2.zero,15f*Time.fixedDeltaTime);
                if(red || junction){blockedTimer=0;return;}
                blockedTimer+=Time.fixedDeltaTime;
                if(blocker!=null && blockedTimer>1.5f && Vector2.Dot(heading,blocker.transform.up)<-.25f)
                {if(GetInstanceID()>blocker.GetInstanceID())BeginRecovery();}
                else if(blockedTimer>5f && blocker!=null && Vector2.Distance(body.position,blocker.transform.position)<2.4f)
                {if(GetInstanceID()>blocker.GetInstanceID())BeginRecovery();}
                return;
            }

            Transform target = waypoints[waypointIndex];
            Vector2 toTarget = target.position - transform.position;
            Vector2 previous=waypoints[(waypointIndex+waypoints.Length-1)%waypoints.Length].position;
            bool passed=Vector2.Dot(body.position-(Vector2)target.position,(Vector2)target.position-previous)>0;
            if (toTarget.magnitude < 1.35f || (passed && toTarget.magnitude<3f))
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                target = waypoints[waypointIndex];
                toTarget = target.position - transform.position;
            }

            if (toTarget.sqrMagnitude > 0.01f)
            {
                float signed = Vector2.SignedAngle(transform.up, toTarget.normalized);
                body.MoveRotation(body.rotation + Mathf.Clamp(signed, -turnRate * Time.fixedDeltaTime, turnRate * Time.fixedDeltaTime));
                Vector2 desiredVelocity = toTarget.normalized * speed * Mathf.Lerp(1f,.35f,Mathf.Clamp01(Mathf.Abs(signed)/75f));
                body.linearVelocity = Vector2.Lerp(body.linearVelocity, desiredVelocity, 0.28f);
            }

            if (body.linearVelocity.magnitude < 0.3f)
            {
                blockedTimer += Time.fixedDeltaTime;
                if (blockedTimer >= blockedTimeBeforeUnstick)
                {
                    blockedTimer = 0f;
                    separatingUntil = Time.time + 0.35f;
                    separationDirection = -(Vector2)transform.up;
                    // Keep the current route target; skipping it can send a car across buildings.
                }
            }
            else
            {
                blockedTimer = 0f;
            }
        }

        private void Update()
        {
            if (!destroyed && hornClip != null && Time.time >= nextHornTime)
            {
                nextHornTime = Time.time + Random.Range(10f, 24f);
                AudioSource.PlayClipAtPoint(hornClip, transform.position, 0.25f);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (destroyed) return;

            PlayerHealth playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(bumpDamage);
                Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)playerHealth.transform.position).normalized;
                if (awayFromPlayer.sqrMagnitude < 0.01f) awayFromPlayer = -(Vector2)transform.up;
                separationDirection = awayFromPlayer;
                separatingUntil = Time.time + separationSeconds;
                body.AddForce(awayFromPlayer * 2.5f, ForceMode2D.Impulse);
            }

            if (collision.relativeVelocity.magnitude > 2.25f)
            {
                EnemyVehicle enemy = collision.collider.GetComponentInParent<EnemyVehicle>();
                enemy?.TakeHit(1, transform.position);
            }
        }

        public void TakeHit(int amount, Vector3 attackerPosition)
        {
            if (destroyed || amount <= 0) return;
            currentHealth -= amount;
            Vector2 away = ((Vector2)transform.position - (Vector2)attackerPosition).normalized;
            body.AddForce(away * 2.8f, ForceMode2D.Impulse);
            if (currentHealth <= 0)
            {
                DestroyVehicle();
            }
        }

        public void Configure(Transform[] route, float moveSpeed, AudioClip engine, AudioClip horn, AudioClip destroyClip, Sprite[] destroyFrames, int health = 2, float collisionDamage = 4f)
        {
            waypoints = route;
            speed = moveSpeed;
            engineLoop = engine;
            hornClip = horn;
            destructionClip = destroyClip;
            destructionFrames = destroyFrames;
            maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth;
            bumpDamage = Mathf.Max(0f, collisionDamage);
        }

        private void DestroyVehicle()
        {
            if (destroyed) return;
            destroyed = true;
            if (audioSource != null) audioSource.Stop();
            if (destructionClip != null) AudioSource.PlayClipAtPoint(destructionClip, transform.position, 0.65f);
            TransientSpriteAnimation.Spawn(destructionFrames, transform.position, 0.045f, 20, Vector3.one * 1.25f, Color.white);
            Destroy(gameObject);
        }
    }
}

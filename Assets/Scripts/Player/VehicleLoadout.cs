using System.Collections.Generic;
using ArtifactCourier.Core;
using ArtifactCourier.Enemies;
using ArtifactCourier.Effects;
using UnityEngine;

namespace ArtifactCourier.Player
{
    public sealed class VehicleLoadout : MonoBehaviour
    {
        [SerializeField] Sprite[] vehicles, attacks;
        [SerializeField] int levelIndex;
        CarController car; PlayerHealth health; SaveData data;
        Material originalMaterial;
        int kitTier; float nextKitProc;
        float nextDash; float charge; float magnetTimer; float lastDamageHealth;
        public int Kind { get; private set; }
        public float Charge => charge;
        public string ClassName => Kind==0?"NIGHTJAR / MOTORCYCLE":Kind==1?"BISON / PICKUP":"KESTREL / COUPE";
        public void Configure(Sprite[] art, Sprite[] fx, int level) { vehicles=art;attacks=fx;levelIndex=level; }
        void Start()
        {
            originalMaterial=GetComponent<SpriteRenderer>().sharedMaterial;
            data=GameSession.Instance.Data; Kind=data.progressionVersion==0?2:Mathf.Clamp(data.vehicleClass,0,2);
            car=GetComponent<CarController>(); health=GetComponent<PlayerHealth>();
            car.ConfigureClass((Kind==0?17f:Kind==1?9.5f:13f)*(1f+data.engineRank*.08f),Kind==0?25f:Kind==1?28f:20f,Kind==0?220f:Kind==1?115f:170f);
            GetComponent<Rigidbody2D>().mass=Kind==0?.85f:Kind==1?2.5f:1.3f;
            health.Configure(CombatBalance.PlayerMaxHealth(levelIndex)*(Kind==0?.65f:Kind==1?1.65f:1f));
            lastDamageHealth=health.Current;
            if (vehicles!=null && vehicles.Length==3)
            {
                var sr=GetComponent<SpriteRenderer>(); sr.sprite=vehicles[Kind];sr.flipX=true;sr.flipY=true;
                transform.localScale=Vector3.one;
                var col=GetComponent<CapsuleCollider2D>();col.size=new Vector2(Kind==0?.65f:Kind==1?1.5f:1.25f,2.25f);
            }
            GetComponent<PlayerPulseAttack>().ConfigureClass(Kind,data.combatRank);
            health.HealthChanged+=OnHealth;
            RefreshCosmetic();
        }
        public void RefreshCosmetic()
        {
            var catalog=CosmeticCatalog.Instance;if(catalog==null||data==null)return;
            int tier=CosmeticCatalog.Equipped(data,Kind);var sr=GetComponent<SpriteRenderer>();
            sr.sprite=tier==0?catalog.originals[Kind]:catalog.skins[(tier-1)*3+Kind];
            sr.sharedMaterial=tier==0?originalMaterial:catalog.keyedMaterial;
            kitTier=tier;car.SetKitNitroDuration(Kind==0&&tier==1?1.4f:1f);
        }
        void OnDestroy() { if(health!=null) health.HealthChanged-=OnHealth; }
        void OnHealth(float current,float max) { if(current<lastDamageHealth) charge=Mathf.Max(0,charge-.12f);lastDamageHealth=current; }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.G) && Time.timeScale>0 && !ArtifactCourier.UI.TutorialOverlay.IsOpen){ArtifactCourier.UI.CosmeticShop.Open();return;}
            if(data==null || Time.timeScale==0 || !car.ControlsEnabled) return;
            if(Kind==0 && kitTier==3 && car.IsBoosting && Time.time>=nextKitProc)
            {nextKitProc=Time.time+8f;health.ApplyInvulnerability(.8f);}
            if(data.engineRank>0 && Input.GetKeyDown(KeyCode.R) && Time.time>=nextDash)
            {
                nextDash=Time.time+6f;car.Knockback((Vector2)transform.up*(Kind==1?20f:9f));
                if(data.engineRank>1) health.ApplyInvulnerability(.7f);
            }
            if(Input.GetKeyDown(KeyCode.F) && charge>=1f)
            {
                charge=0f;Pulse(Kind==1?7f:5f,8+levelIndex*2,false);
                if(Kind==0) {health.ApplyInvulnerability(1.4f);car.RefillNitro(1f);}
                else if(Kind==1) health.AddShield(40f);
                else {foreach(var signal in Traffic.TrafficSignal.Active) signal.OpenGreenWave(7f);}
            }
            if(data.utilityRank>0 && Time.time>=magnetTimer)
            {
                magnetTimer=Time.time+.2f;
                foreach(var hit in Physics2D.OverlapCircleAll(transform.position,3.2f))
                {
                    var pickup=hit.GetComponent<Delivery.ArtifactPickup>();
                    if(pickup!=null) pickup.transform.position=Vector3.MoveTowards(pickup.transform.position,transform.position,.65f);
                }
            }
        }
        public void AddResonance(float amount) {charge=Mathf.Clamp01(charge+amount);car.RefillNitro(.15f);if(data.utilityRank>1)health.Heal(8);}
        public bool ClassSpecial(bool finisher)
        {
            if(Kind==2)return false; // Kestrel keeps EMP and the penetrating courier bolt.
            if(Kind==0)
            {
                health.ApplyInvulnerability(finisher?1f:.55f);
                car.Knockback((Vector2)transform.up*(finisher?12:8));
                Pulse(finisher?3.8f:2.8f,(finisher?9:4)+levelIndex,true);
            }
            else
            {
                if(!finisher)health.AddShield(22+levelIndex*2);
                Pulse(finisher?6.5f:3.8f,(finisher?13:6)+levelIndex,false);
            }
            return true;
        }
        public void BasicAttack()
        {
            int amount=(Kind==0?2:Kind==1?7:3)+levelIndex+(data.combatRank>0?1:0);
            Pulse(Kind==0?2.5f:Kind==1?3.4f:3.1f,amount,Kind==0);
        }
        System.Collections.IEnumerator Aftershock(float radius,int damage,int expectedTier)
        {
            yield return new WaitForSeconds(.35f);
            if(kitTier==expectedTier && car.ControlsEnabled)Pulse(radius+.5f,Mathf.Max(1,damage/2),false,false);
        }
        void Pulse(float range,int damage,bool cone,bool allowKit=true)
        {
            var seen=new HashSet<EnemyVehicle>();
            foreach(var hit in Physics2D.OverlapCircleAll(transform.position,range))
            {
                var enemy=hit.GetComponentInParent<EnemyVehicle>();if(enemy==null || seen.Contains(enemy))continue;
                Vector2 delta=enemy.transform.position-transform.position;
                if(cone && !VehicleKitRules.InBasicArc(Kind,kitTier,Vector2.Dot(transform.up,delta.normalized)))continue;
                seen.Add(enemy);enemy.TakeHit(damage,transform.position);
                if(data.combatRank>1)enemy.ApplyDisruption(Kind==0?1f:2f);
                if(allowKit && Kind==1 && kitTier==1)enemy.GetComponent<Rigidbody2D>()?.AddForce(delta.normalized*4f,ForceMode2D.Impulse);
                if(allowKit && Kind==2 && kitTier==2)enemy.ApplyDisruption(1.5f);
            }
            if(allowKit && seen.Count>0)
            {
                if(Kind==1 && kitTier==2 && Time.time>=nextKitProc){nextKitProc=Time.time+5f;health.AddShield(12f);}
                if(Kind==2 && kitTier==1 && Time.time>=nextKitProc){nextKitProc=Time.time+1f;car.RefillNitro(.1f);}
                if(Kind==1 && kitTier==3)StartCoroutine(Aftershock(range,damage,kitTier));
                if(Kind==2 && kitTier==3)
                {
                    int chains=0;
                    foreach(var hit in Physics2D.OverlapCircleAll(transform.position,6f))
                    {
                        var extra=hit.GetComponentInParent<EnemyVehicle>();if(extra==null||!seen.Add(extra))continue;
                        extra.TakeHit(Mathf.Max(1,damage/2),transform.position);extra.ApplyDisruption(.4f);
                        if(attacks!=null && attacks.Length==3)TransientSpriteAnimation.Spawn(new[]{attacks[2]},extra.transform.position,.15f,32,Vector3.one*.6f,Color.cyan);
                        if(++chains==2)break;
                    }
                }
            }
            if(attacks!=null && attacks.Length==3)
            {
                TransientSpriteAnimation.Spawn(new[]{attacks[Kind]},transform.position,0.22f,32,Vector3.one*(range/2f),Color.white,rotationDegrees:transform.eulerAngles.z);
            }
        }
    }
}

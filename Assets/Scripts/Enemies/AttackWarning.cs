using System.Collections;
using ArtifactCourier.Player;
using UnityEngine;
namespace ArtifactCourier.Enemies
{
    // Telegraph is fixed in world space: leaving the ring genuinely avoids the attack.
    public sealed class AttackWarning : MonoBehaviour
    {
        static Material ink;
        LineRenderer ring; Transform owner; float radius,damage,delay,slow; bool pull;
        public static void Spawn(Transform source,Vector2 center,float range,float hit,float windup,Color color,float slowFactor=1f,bool hook=false)
        {
            var go=new GameObject(hook?"Hook lock warning":"Attack danger ring");go.transform.position=center;
            var attack=go.AddComponent<AttackWarning>();attack.owner=source;attack.radius=range;attack.damage=hit;attack.delay=windup;attack.slow=slowFactor;attack.pull=hook;
            attack.ring=go.AddComponent<LineRenderer>();
            if(ink==null)ink=new Material(Shader.Find("Sprites/Default"));
            attack.ring.sharedMaterial=ink;attack.ring.useWorldSpace=false;attack.ring.loop=true;attack.ring.positionCount=64;
            attack.ring.widthMultiplier=.13f;attack.ring.startColor=attack.ring.endColor=color;attack.ring.sortingOrder=26;
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;attack.ring.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*range);}
        }
        IEnumerator Start()
        {
            yield return new WaitForSeconds(delay);
            if(owner!=null)
            {
                var enemy=owner.GetComponent<EnemyVehicle>();
                if(enemy==null || !enemy.IsDisrupted)
                {
                    var player=FindFirstObjectByType<PlayerHealth>();
                    if(player!=null && (damage>0 || pull) && Vector2.Distance(player.transform.position,transform.position)<=radius)
                    {
                        player.TakeDamage(damage);if(slow<1)player.GetComponent<PlayerStatusEffects>()?.ApplySlow(slow,1.3f);
                        Vector2 direction=pull?(Vector2)(owner.position-player.transform.position):(Vector2)(player.transform.position-transform.position);
                        player.GetComponent<CarController>()?.Knockback(direction.normalized*(pull?7:4));
                    }
                }
            }
            if(ring!=null){ring.startColor=ring.endColor=Color.white;ring.widthMultiplier=.4f;}
            yield return new WaitForSeconds(.18f);Destroy(gameObject);
        }
    }
}

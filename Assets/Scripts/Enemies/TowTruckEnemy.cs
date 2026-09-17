using UnityEngine;
namespace ArtifactCourier.Enemies
{
    public sealed class TowTruckEnemy : EnemyVehicle
    {
        [SerializeField] AudioClip hookClip;float nextHook;
        public void ConfigureTow(AudioClip clip)=>hookClip=clip;
        protected override void TickBehavior()
        {
            if(Target==null || DistanceToPlayer>6.5f || Time.time<nextHook)return;
            nextHook=Time.time+Mathf.Max(3.8f,6f-Level*.25f);
            AttackWarning.Spawn(transform,Target.position,1.7f,4+Level,1f,new Color(1,.7f,.2f),.7f,true);
            if(hookClip!=null)AudioSource.PlayClipAtPoint(hookClip,transform.position,.6f);
        }
    }
}

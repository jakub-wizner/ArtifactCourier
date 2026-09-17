using UnityEngine;
namespace ArtifactCourier.Enemies
{
    public sealed class JammerVan : EnemyVehicle
    {
        [SerializeField] AudioClip slowClip;float nextPulse;
        public void ConfigureJammer(AudioClip clip)=>slowClip=clip;
        protected override void TickBehavior()
        {
            if(Target==null || DistanceToPlayer>6 || Time.time<nextPulse)return;
            nextPulse=Time.time+Mathf.Max(3.5f,6-Level*.3f);
            AttackWarning.Spawn(transform,transform.position,4.5f+Level*.15f,3+Level,.95f,new Color(.65f,.35f,1),.5f);
            if(slowClip!=null)AudioSource.PlayClipAtPoint(slowClip,transform.position,.6f);
        }
    }
}

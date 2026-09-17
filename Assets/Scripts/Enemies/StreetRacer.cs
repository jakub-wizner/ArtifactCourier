using UnityEngine;
namespace ArtifactCourier.Enemies
{
    public sealed class StreetRacer : EnemyVehicle
    {
        protected override bool UsesCustomSteering => true;
        float nextDash,launchAt,dashUntil;Vector2 lockedDirection;bool charging;
        protected override void TickBehavior()
        {
            if(Target==null)return;
            if(charging)
            {
                DriveToward(Target.position);
                if(Time.time>=launchAt){charging=false;dashUntil=Time.time+.7f;}
            }
            else if(Time.time<dashUntil)
            {Body.linearVelocity=lockedDirection*(12f+Level*1.1f);Body.MoveRotation(Vector2.SignedAngle(Vector2.up,lockedDirection));}
            else if(DistanceToPlayer<10 && Time.time>=nextDash)
            {
                nextDash=Time.time+Mathf.Max(3.3f,5.5f-Level*.25f);launchAt=Time.time+.8f;charging=true;
                lockedDirection=((Vector2)Target.position-Body.position).normalized;
                PlayAttackAnimation(1.7f,new Color(1,.25f,.6f));
                AttackWarning.Spawn(transform,Target.position,1.4f,0,.8f,new Color(1,.25f,.6f));
            }
            else DriveToward((Vector2)Target.position+(Vector2)Target.right*Mathf.Sin(Time.time*1.4f)*2,1.05f);
        }
    }
}

using ArtifactCourier.Player;
using UnityEngine;
namespace ArtifactCourier.Enemies
{
    public sealed class PoliceInterceptor : EnemyVehicle
    {
        float nextDash,windup,dashUntil;bool warning;Vector2 intercept;
        protected override void TickBehavior()
        {
            if(Target==null)return;
            if(warning)
            {
                // Base steering continues through the windup.
                if(Time.time>=windup){warning=false;dashUntil=Time.time+.55f;}
            }
            else if(Time.time<dashUntil)
            {Body.linearVelocity=intercept*(10f+Level*.6f);Body.MoveRotation(Vector2.SignedAngle(Vector2.up,intercept));}
            else if(DistanceToPlayer<8f && Time.time>=nextDash)
            {
                nextDash=Time.time+Mathf.Max(3,5-Level*.2f);windup=Time.time+.65f;warning=true;
                var motion=Target.GetComponent<Rigidbody2D>().linearVelocity;
                intercept=((Vector2)Target.position+motion*.35f-Body.position).normalized;
                PlayAttackAnimation(1.6f,new Color(.3f,.6f,1));
            }
        }
        protected override void OnPlayerCollision(Collider2D playerCollider)
        {playerCollider.GetComponentInParent<PlayerStatusEffects>()?.ApplySlow(.72f,1f+Level*.08f);}
    }
}

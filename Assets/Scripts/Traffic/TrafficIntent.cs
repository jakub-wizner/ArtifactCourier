using UnityEngine;
namespace ArtifactCourier.Traffic
{
    public sealed class TrafficIntent : MonoBehaviour
    {
        public SpriteRenderer left,right,brake;
        public void Show(float turn,bool stopped)
        {
            bool blink=Mathf.Repeat(Time.time,0.7f)<.35f;
            if(left!=null)left.enabled=blink&&turn>12f;
            if(right!=null)right.enabled=blink&&turn< -12f;
            if(brake!=null)brake.enabled=stopped;
        }
    }
}

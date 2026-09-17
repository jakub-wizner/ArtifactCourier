using System.Collections.Generic;
using UnityEngine;
namespace ArtifactCourier.Traffic
{
    public sealed class TrafficSignal : MonoBehaviour
    {
        public static readonly List<TrafficSignal> Active = new();
        public float offset;
        public SpriteRenderer northLamp, eastLamp;
        float greenUntil;
        public bool GreenWave => Time.time<greenUntil;
        float Phase => Mathf.Repeat(Time.time+offset,14f);
        public void OpenGreenWave(float seconds) {greenUntil=Time.time+seconds;}
        void OnEnable() {if(!Active.Contains(this))Active.Add(this);}
        void OnDisable() {Active.Remove(this);}
        void Update()
        {
            float p=Phase;
            if(northLamp!=null)northLamp.color=GreenWave?Color.cyan:p<5?Color.green:p<6?Color.yellow:Color.red;
            if(eastLamp!=null)eastLamp.color=GreenWave?Color.cyan:p>=7&&p<12?Color.green:p>=12&&p<13?Color.yellow:Color.red;
        }
        public bool Stops(Vector2 position, Vector2 heading)
        {
            Vector2 delta=(Vector2)transform.position-position;
            float along=Vector2.Dot(delta,heading);
            float across=Mathf.Abs(delta.x*heading.y-delta.y*heading.x);
            // Stop before entering; vehicles already committed must clear the junction.
            if(along<3f || along>7f || across>2.4f)return false;
            float p=Phase;
            bool north=Mathf.Abs(heading.y)>Mathf.Abs(heading.x);
            // During a courier green wave, cross traffic holds while east/west flows.
            return GreenWave?north:north?p>=5:!(p>=7&&p<12);
        }
    }
}

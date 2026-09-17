using ArtifactCourier.Player;
using UnityEngine;
using UnityEngine.UI;
namespace ArtifactCourier.UI
{
    public sealed class CityResonanceRoute : MonoBehaviour
    {
        [SerializeField] Vector2[] checkpoints;
        [SerializeField] Sprite beacon;
        [SerializeField] string identity;
        VehicleLoadout player; SpriteRenderer marker; Text status;
        int next; float expires; float resetAt; bool running;
        public void Configure(Vector2[] gates,Sprite art,string city){checkpoints=gates;beacon=art;identity=city;}
        void Start()
        {
            player=FindFirstObjectByType<VehicleLoadout>();
            var go=new GameObject("Resonance beacon");go.transform.SetParent(transform,false);marker=go.AddComponent<SpriteRenderer>();marker.sprite=beacon;marker.sortingOrder=11;go.transform.localScale=Vector3.one;
            var canvas=new GameObject("Resonance HUD").AddComponent<Canvas>();canvas.transform.SetParent(transform,false);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2;
            var scale=canvas.gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);
            var label=new GameObject("Route status",typeof(RectTransform));label.transform.SetParent(canvas.transform,false);status=label.AddComponent<Text>();status.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");status.fontSize=16;status.alignment=TextAnchor.MiddleCenter;status.color=new Color(.5f,1f,1f);status.raycastTarget=false;
            var rect=status.rectTransform;rect.anchorMin=new Vector2(.18f,.02f);rect.anchorMax=new Vector2(.82f,.12f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        void Update()
        {
            if(player==null || checkpoints==null || checkpoints.Length==0 || Time.timeScale==0)return;
            if(Time.time<resetAt){marker.enabled=false;status.text=$"{player.ClassName} / RESONANCE {player.Charge:P0}\nF: CITY ABILITY   R: GARAGE DASH   G: COIN SHOP";return;}
            marker.enabled=true;marker.transform.position=checkpoints[next];
            if(running && Time.time>expires){next=0;running=false;resetAt=Time.time+6;return;}
            if(Vector2.Distance(player.transform.position,checkpoints[next])<3f)
            {
                if(!running){running=true;expires=Time.time+45f;}
                player.AddResonance(.34f);next++;
                if(next==checkpoints.Length){next=0;running=false;resetAt=Time.time+25f;}
            }
            Vector2 target=checkpoints[next];
            Vector2 direction=target-(Vector2)player.transform.position;
            string compass=Mathf.Abs(direction.x)>Mathf.Abs(direction.y)?(direction.x>0?"EAST":"WEST"):(direction.y>0?"NORTH":"SOUTH");
            status.text=$"{identity} / NEXT BEACON {compass} {direction.magnitude:0}m\n{(running?$"RESONANCE ROUTE {next}/{checkpoints.Length} / {Mathf.CeilToInt(expires-Time.time)}s":"OPTIONAL: FOLLOW THE RADIO BEACON")}   /   CHARGE {player.Charge:P0}   F: RELEASE   G: SHOP";
        }
    }
}

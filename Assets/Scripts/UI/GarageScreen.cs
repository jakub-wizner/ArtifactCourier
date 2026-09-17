using System;
using ArtifactCourier.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ArtifactCourier.UI
{
    public sealed class GarageScreen : MonoBehaviour
    {
        public static bool IsOpen {get;private set;}
        Text balance; Button[] branches=new Button[3]; Action continueAction;
        public static void Open(Action next)
        {
            if(IsOpen)return;IsOpen=true;Time.timeScale=0;
            var screen=new GameObject("Between Cities Garage").AddComponent<GarageScreen>();screen.continueAction=next;screen.Build();
        }
        void OnDestroy(){IsOpen=false;}
        void Build()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);
            gameObject.AddComponent<GraphicRaycaster>();
            var backdrop=Rect("Garage",transform,new Vector2(0,0),new Vector2(1,1));backdrop.AddComponent<Image>().color=new Color(.018f,.03f,.055f,.98f);
            Label(backdrop.transform,"THE NIGHT SHIFT GARAGE",32,new Vector2(.08f,.82f),new Vector2(.92f,.94f));
            balance=Label(backdrop.transform,"",19,new Vector2(.08f,.72f),new Vector2(.92f,.81f));
            for(int i=0;i<3;i++)
            {
                int branch=i;float x=.06f+i*.30f;
                var button=Rect("Upgrade",backdrop.transform,new Vector2(x,.29f),new Vector2(x+.28f,.68f));
                button.AddComponent<Image>().color=new Color(.08f,.17f,.22f);branches[i]=button.AddComponent<Button>();
                Label(button.transform,"",19,new Vector2(.06f,.05f),new Vector2(.94f,.95f));
                branches[i].onClick.AddListener(()=>{GameSession.Instance.BuyUpgrade(branch);Refresh();});
            }
            var shop=Rect("Coachworks",backdrop.transform,new Vector2(.32f,.22f),new Vector2(.68f,.28f));shop.AddComponent<Image>().color=new Color(.12f,.4f,.5f);
            shop.AddComponent<Button>().onClick.AddListener(CosmeticShop.Open);Label(shop.transform,"COIN SHOP / VEHICLE LOOKS",17,Vector2.zero,Vector2.one);
            var go=Rect("Continue",backdrop.transform,new Vector2(.32f,.1f),new Vector2(.68f,.21f));go.AddComponent<Image>().color=new Color(.12f,.6f,.62f);
            go.AddComponent<Button>().onClick.AddListener(()=>{IsOpen=false;Destroy(gameObject);continueAction?.Invoke();});
            Label(go.transform,"DEPART / KEEP UNUSED POINTS",18,Vector2.zero,Vector2.one);Refresh();
        }
        void Refresh()
        {
            var d=GameSession.Instance.Data;balance.text=$"{d.skillPoints} UPGRADE POINTS   /   One per first city clear   /   Tier II opens after Paris unlocks";
            string[] titles={"MOBILITY","COMBAT","COURIER"};
            string[] one={"R: directional dash\n+8% top speed","Faster, stronger attacks","Artifact attraction\n3.2 m pickup field"};
            string[] two={"Dash gains 0.7s protection\nAnother +8% speed","Hits disrupt enemy attacks","Resonance checkpoints\nrepair 8 health"};
            int[] ranks={d.engineRank,d.combatRank,d.utilityRank};
            for(int i=0;i<3;i++)
            {
                int rank=ranks[i];string state=rank==2?"COMPLETE":rank==1&&d.highestUnlockedLevelIndex<3?"TIER II LOCKED":"SPEND 1 POINT";
                branches[i].GetComponentInChildren<Text>().text=$"{titles[i]} / {rank}/2\n\nI   {one[i]}\n\nII   {two[i]}\n\n{state}";
                branches[i].interactable=d.skillPoints>0&&rank<2&&(rank==0||d.highestUnlockedLevelIndex>=3);
            }
        }
        static GameObject Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return go;}
        static Text Label(Transform parent,string value,int size,Vector2 min,Vector2 max)
        {var go=Rect("Label",parent,min,max);var t=go.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;return t;}
    }
}

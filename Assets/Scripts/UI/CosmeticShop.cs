using ArtifactCourier.Core;
using ArtifactCourier.Player;
using UnityEngine;
using UnityEngine.UI;
namespace ArtifactCourier.UI
{
    public sealed class CosmeticShop : MonoBehaviour
    {
        public static bool IsOpen {get;private set;}
        Text wallet; Button[] items=new Button[3];int selectedClass;float previousScale;
        public static void Open()
        {if(IsOpen)return;var shop=new GameObject("Coachworks Shop").AddComponent<CosmeticShop>();shop.previousScale=Time.timeScale;Time.timeScale=0;IsOpen=true;shop.Build();}
        void Update(){if(Input.GetKeyDown(KeyCode.G)||Input.GetKeyDown(KeyCode.Escape))Close();}
        void OnDestroy(){IsOpen=false;}
        void Close(){Time.timeScale=previousScale;IsOpen=false;Destroy(gameObject);FindFirstObjectByType<VehicleLoadout>()?.RefreshCosmetic();}
        void Build()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);gameObject.AddComponent<GraphicRaycaster>();
            var bg=Rect("Shop",transform,Vector2.zero,Vector2.one);bg.AddComponent<Image>().color=new Color(.015f,.025f,.045f,.99f);
            wallet=Label(bg.transform,"",22,new Vector2(.03f,.9f),new Vector2(.97f,.99f));
            selectedClass=Mathf.Clamp(GameSession.Instance.Data.vehicleClass,0,2);
            string[] names={"NIGHTJAR","BISON","KESTREL"};
            Button(bg.transform,names[selectedClass]+" / EQUIP STOCK",new Vector2(.34f,.8f),new Vector2(.66f,.87f)).onClick.AddListener(()=>{GameSession.Instance.EquipOriginal(selectedClass);Refresh();});
            for(int tier=0;tier<3;tier++)
            {
                int t=tier;float x=.035f+tier*.325f;
                var card=Button(bg.transform,"",new Vector2(x,.15f),new Vector2(x+.30f,.77f));items[tier]=card;
                var preview=Rect("Preview",card.transform,new Vector2(.12f,.51f),new Vector2(.88f,.98f)).AddComponent<Image>();
                preview.sprite=CosmeticCatalog.Instance.skins[tier*3+selectedClass];preview.material=CosmeticCatalog.Instance.keyedMaterial;preview.preserveAspect=true;preview.raycastTarget=false;
                var text=card.GetComponentInChildren<Text>();text.rectTransform.anchorMin=new Vector2(.04f,.025f);text.rectTransform.anchorMax=new Vector2(.96f,.49f);
                card.onClick.AddListener(()=>{GameSession.Instance.PurchaseCosmetic(selectedClass,t);Refresh();});
            }
            Button(bg.transform,"BACK TO DRIVING / G",new Vector2(.34f,.025f),new Vector2(.66f,.10f)).onClick.AddListener(Close);Refresh();
        }
        void Refresh()
        {
            var d=GameSession.Instance.Data;wallet.text=$"CLASS WORKSHOP / {d.coins} COINS / Each body kit equips its own capability";
            string[] names={"STREET KIT","PERFORMANCE KIT","PRESTIGE KIT"};
            for(int tier=0;tier<3;tier++)
            {
                bool owned=(d.ownedCosmetics&(1<<(tier*3+selectedClass)))!=0;
                bool unlocked=d.highestUnlockedLevelIndex+1>=CosmeticCatalog.UnlockLevel(tier);
                bool equipped=CosmeticCatalog.Equipped(d,selectedClass)==tier+1;
                string state=equipped?"EQUIPPED":owned?"EQUIP":!unlocked?"LOCKED":$"BUY / {CosmeticCatalog.Price(tier)} COINS";
                items[tier].GetComponentInChildren<Text>().text=$"{names[tier]} / LEVEL {CosmeticCatalog.UnlockLevel(tier)}\n\n{VehicleKitRules.Description(selectedClass,tier+1)}\n\n{state}";
                items[tier].interactable=!equipped&&unlocked&&(owned||d.coins>=CosmeticCatalog.Price(tier));
            }
            FindFirstObjectByType<VehicleLoadout>()?.RefreshCosmetic();
        }
        static GameObject Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return go;}
        static Text Label(Transform parent,string text,int size,Vector2 min,Vector2 max)
        {var label=Rect("Label",parent,min,max).AddComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=size;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;}
        static Button Button(Transform parent,string text,Vector2 min,Vector2 max)
        {var go=Rect("Card",parent,min,max);go.AddComponent<Image>().color=new Color(.07f,.14f,.20f);var b=go.AddComponent<Button>();Label(go.transform,text,17,Vector2.zero,Vector2.one);return b;}
    }
}

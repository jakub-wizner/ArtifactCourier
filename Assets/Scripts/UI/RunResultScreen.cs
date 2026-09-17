using ArtifactCourier.Core;
using ArtifactCourier.Driving;
using ArtifactCourier.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    // Constructed on demand: works in existing scenes as well as regenerated scenes.
    public sealed class RunResultScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        bool leaving;
        public static void Show(bool victory)
        {
            if (IsOpen) return;
            IsOpen = true;
            var screen = new GameObject("Run Result Screen").AddComponent<RunResultScreen>();
            screen.Build(victory);
        }
        void OnDestroy() { IsOpen = false; }

        void Build(bool victory)
        {
            var level = LevelController.Instance;
            var style = FindFirstObjectByType<DrivingStyle>();
            var car = FindFirstObjectByType<CarController>();
            if (car != null) car.SetControlsEnabled(false);
            style?.SaveBest();
            Time.timeScale = 0f;
            var data = GameSession.Instance.Data;
            int city = level == null ? data.currentLevelIndex : level.LevelIndex;
            int attemptScore = style == null ? 0 : style.Score;
            long total = 0;
            if (data.cityScores != null)
                for (int i = 0; i < data.cityScores.Length; i++)
                    if (victory || i != city) total += System.Math.Max(0, data.cityScores[i]);
            if (!victory) total += attemptScore;
            bool fullCampaign = (data.completedCityMask & 127) == 127;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
                new GameObject("Result Event System", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform);

            var backdrop = Rect("Backdrop", transform, Vector2.zero, Vector2.one);
            backdrop.AddComponent<Image>().color = new Color(.012f, .023f, .043f, .98f);
            var card = Rect("Result Card", backdrop.transform, new Vector2(.12f,.09f), new Vector2(.88f,.91f));
            card.AddComponent<Image>().color = new Color(.035f,.07f,.105f);
            Color accent = victory ? new Color(.3f,1f,.78f) : new Color(1f,.42f,.3f);
            Label(card.transform, "ARTIFACT COURIER / " + (victory ? "DELIVERY REPORT" : "RUN ENDED"), 18, .89f, .96f, accent);
            Label(card.transform, victory ? (fullCampaign ? "CAMPAIGN COMPLETE" : "FINAL CITY COMPLETE") : "GAME OVER", 40, .76f, .89f, Color.white);
            Label(card.transform, "FINAL SCORE", 18, .68f, .75f, accent);
            Label(card.transform, total.ToString("N0"), 64, .53f, .69f, Color.white);
            string details = (level == null ? "" : level.CityName + "  /  Deliveries " + level.DeliveredCount + "/" + level.RequiredDeliveries + "  /  Enemies defeated " + level.EnemiesDefeated + "\n")
                + "City attempt score: " + attemptScore.ToString("N0") + "  /  City personal best: " + (style == null ? 0 : style.Best).ToString("N0");
            Label(card.transform, details, 21, .38f, .53f, Color.white);
            string explanation = victory ? "Total of your best completed-city scores in this campaign.\nPlay Again starts a new campaign and resets its progress."
                : "Completed-city scores plus this attempt. Retry starts this city's score at zero.";
            if (victory && !fullCampaign) explanation += "\nSome cities were skipped; this is not a full campaign clear.";
            if (!victory) explanation += "\nYour saved upgrades and previously completed cities are kept.";
            Label(card.transform, explanation, 16, .25f, .38f, new Color(.66f,.77f,.85f));
            var primary = Button(card.transform, victory ? "PLAY AGAIN" : "RETRY CITY", .05f,.335f, accent);
            primary.onClick.AddListener(() => Leave(() => {
                if (victory) { GameSession.SelectedVehicle = data.vehicleClass; GameSession.Instance.StartNewGame(); }
                else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }));
            Button(card.transform,"MAIN MENU",.36f,.645f,new Color(.16f,.31f,.42f)).onClick.AddListener(() => Leave(GameSession.Instance.ReturnToMainMenu));
            Button(card.transform,"QUIT",.67f,.95f,new Color(.16f,.31f,.42f)).onClick.AddListener(() => {
#if UNITY_EDITOR
                Leave(() => UnityEditor.EditorApplication.isPlaying = false);
#else
                Leave(Application.Quit);
#endif
            });
            EventSystem.current?.SetSelectedGameObject(primary.gameObject);
        }
        void Leave(System.Action action)
        {
            if (leaving) return;
            leaving = true; Time.timeScale = 1f; IsOpen = false;
            action();
        }
        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin=min; r.anchorMax=max; r.offsetMin=r.offsetMax=Vector2.zero;
            return go;
        }
        static void Label(Transform parent,string text,int size,float bottom,float top,Color color)
        {
            var label=Rect("Label",parent,new Vector2(.04f,bottom),new Vector2(.96f,top)).AddComponent<Text>();
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=size;
            label.color=color;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        }
        static Button Button(Transform parent,string text,float left,float right,Color color)
        {
            var go=Rect(text,parent,new Vector2(left,.085f),new Vector2(right,.205f));
            var image=go.AddComponent<Image>();image.color=color;
            var button=go.AddComponent<Button>();button.targetGraphic=image;
            Label(go.transform,text,20,0f,1f,left < .1f ? new Color(.02f,.04f,.06f) : Color.white);return button;
        }
    }
}

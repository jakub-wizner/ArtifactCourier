using System.Collections.Generic;
using ArtifactCourier.Delivery;
using ArtifactCourier.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class MinimapController : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private Text modeHint;
        [SerializeField] private Text playerStateText;
        [SerializeField] private float worldWidth = 70f;
        [SerializeField] private float worldHeight = 44f;
        [SerializeField] private float[] verticalRoads;
        [SerializeField] private float[] horizontalRoads;
        [SerializeField] private Sprite playerMarkerSprite;
        [SerializeField] private Sprite artifactMarkerSprite;
        [SerializeField] private Sprite recipientMarkerSprite;
        [SerializeField] private Sprite playerHaloSprite;

        private Transform player;
        private RectTransform playerOuterHalo;
        private RectTransform playerInnerHalo;
        private RectTransform playerMarker;
        private Text playerLabel;
        private CanvasGroup canvasGroup;
        private bool visible;
        private readonly List<TrackedArtifact> artifacts = new();
        private readonly List<TrackedRecipient> recipients = new();

        private sealed class TrackedArtifact
        {
            public ArtifactPickup Target;
            public RectTransform Marker;
        }

        private sealed class TrackedRecipient
        {
            public DeliveryRecipient Target;
            public RectTransform Marker;
        }

        public void Configure(RectTransform panel, RectTransform rect, Text hint, Text stateText, float width, float height, float[] vertical, float[] horizontal, Sprite playerIcon, Sprite artifactIcon, Sprite recipientIcon, Sprite haloIcon)
        {
            panelRoot = panel;
            mapArea = rect;
            modeHint = hint;
            playerStateText = stateText;
            worldWidth = Mathf.Max(1f, width);
            worldHeight = Mathf.Max(1f, height);
            verticalRoads = vertical;
            horizontalRoads = horizontal;
            playerMarkerSprite = playerIcon;
            artifactMarkerSprite = artifactIcon;
            recipientMarkerSprite = recipientIcon;
            playerHaloSprite = haloIcon != null ? haloIcon : playerIcon;
        }

        private void Start()
        {
            if (mapArea == null || panelRoot == null) return;

            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            visible = false;

            Canvas.ForceUpdateCanvases();
            if (FindFirstObjectByType<ArtifactCourier.Traffic.CityRoadNetwork>() == null) DrawDistrictBlocks();
            DrawMapFrame();
            DrawRoadNetwork();
            DrawCompass();

            CarController car = FindFirstObjectByType<CarController>();
            if (car != null)
            {
                player = car.transform;
                playerOuterHalo = CreateMarker("Player Outer Halo", playerHaloSprite, new Color(0.10f, 0.92f, 1f, 0.24f), new Vector2(42f, 42f), 90);
                playerInnerHalo = CreateMarker("Player Inner Halo", playerHaloSprite, new Color(1f, 0.76f, 0.16f, 0.62f), new Vector2(30f, 30f), 91);
                playerMarker = CreateMarker("Player Marker", playerMarkerSprite, Color.white, new Vector2(22f, 22f), 92);
                playerLabel = CreateLabel("YOU", new Color(1f, 0.88f, 0.34f, 1f), 13, 93);
            }

            ArtifactPickup[] pickups = FindObjectsByType<ArtifactPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ArtifactPickup pickup in pickups)
            {
                RectTransform marker = CreateMarker($"Artifact Marker - {pickup.ArtifactId}", artifactMarkerSprite, pickup.ArtifactColor, new Vector2(16f, 16f), 70);
                artifacts.Add(new TrackedArtifact { Target = pickup, Marker = marker });
            }

            DeliveryRecipient[] targets = FindObjectsByType<DeliveryRecipient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (DeliveryRecipient target in targets)
            {
                RectTransform marker = CreateMarker($"Recipient Marker - {target.RecipientName}", recipientMarkerSprite, target.RecipientColor, new Vector2(17f, 17f), 71);
                recipients.Add(new TrackedRecipient { Target = target, Marker = marker });
            }

            UpdateModeHint();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M) && Time.timeScale > 0f)
            {
                SetMapVisible(!visible);
            }
        }

        private void LateUpdate()
        {
            if (mapArea == null) return;

            if (player != null && playerMarker != null)
            {
                Vector2 rawPosition = WorldToMap(player.position);
                bool inside = IsInsideWorld(player.position);
                Vector2 position = inside ? rawPosition : ClampToMapEdge(rawPosition, 12f);
                playerMarker.anchoredPosition = position;
                playerMarker.localRotation = Quaternion.Euler(0f, 0f, player.eulerAngles.z);

                if (playerOuterHalo != null)
                {
                    playerOuterHalo.anchoredPosition = position;
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6.5f) * 0.20f;
                    playerOuterHalo.localScale = Vector3.one * pulse;
                    playerOuterHalo.localRotation = playerMarker.localRotation;
                    playerOuterHalo.GetComponent<Image>().color = inside
                        ? new Color(0.10f, 0.92f, 1f, 0.24f)
                        : new Color(1f, 0.18f, 0.12f, 0.45f);
                }
                if (playerInnerHalo != null)
                {
                    playerInnerHalo.anchoredPosition = position;
                    float pulse = 0.92f + Mathf.Sin(Time.unscaledTime * 8.0f) * 0.07f;
                    playerInnerHalo.localScale = Vector3.one * pulse;
                    playerInnerHalo.localRotation = playerMarker.localRotation;
                }
                if (playerLabel != null)
                {
                    playerLabel.text = inside ? "YOU" : "OUTSIDE";
                    playerLabel.color = inside ? new Color(1f, 0.88f, 0.34f, 1f) : new Color(1f, 0.25f, 0.18f, 1f);
                    playerLabel.rectTransform.anchoredPosition = position + new Vector2(0f, -22f);
                }
                if (playerStateText != null)
                {
                    playerStateText.text = inside ? "● PLAYER INSIDE MAP" : "● PLAYER OUTSIDE MAP BOUNDS";
                    playerStateText.color = inside ? new Color(0.32f, 1f, 0.72f, 1f) : new Color(1f, 0.25f, 0.18f, 1f);
                }
            }

            foreach (TrackedArtifact tracked in artifacts)
            {
                bool markerVisible = tracked.Target != null && tracked.Target.gameObject.activeInHierarchy;
                if (tracked.Marker == null) continue;
                tracked.Marker.gameObject.SetActive(markerVisible);
                if (markerVisible) tracked.Marker.anchoredPosition = ClampToMapEdge(WorldToMap(tracked.Target.transform.position), 8f);
            }

            foreach (TrackedRecipient tracked in recipients)
            {
                bool markerVisible = tracked.Target != null && !tracked.Target.Completed;
                if (tracked.Marker == null) continue;
                tracked.Marker.gameObject.SetActive(markerVisible);
                if (markerVisible) tracked.Marker.anchoredPosition = ClampToMapEdge(WorldToMap(tracked.Target.transform.position), 8f);
            }
        }

        private void SetMapVisible(bool show)
        {
            visible = show;
            if (canvasGroup == null && panelRoot != null)
            {
                canvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (panelRoot != null)
            {
                panelRoot.anchorMin = new Vector2(0.10f, 0.06f);
                panelRoot.anchorMax = new Vector2(0.90f, 0.94f);
                panelRoot.offsetMin = Vector2.zero;
                panelRoot.offsetMax = Vector2.zero;
                if (show) panelRoot.SetAsLastSibling();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.blocksRaycasts = show;
                canvasGroup.interactable = show;
            }

            Canvas.ForceUpdateCanvases();
            RebuildDimensions();
            UpdateModeHint();
        }

        private void UpdateModeHint()
        {
            if (modeHint != null)
            {
                modeHint.text = visible ? "[M] CLOSE TACTICAL MAP" : "[M] OPEN TACTICAL MAP";
            }
        }

        private void DrawDistrictBlocks()
        {
            List<float> xs = new() { -worldWidth * 0.5f };
            if (verticalRoads != null) xs.AddRange(verticalRoads);
            xs.Add(worldWidth * 0.5f);
            xs.Sort();

            List<float> ys = new() { -worldHeight * 0.5f };
            if (horizontalRoads != null) ys.AddRange(horizontalRoads);
            ys.Add(worldHeight * 0.5f);
            ys.Sort();

            int index = 0;
            for (int x = 0; x < xs.Count - 1; x++)
            {
                for (int y = 0; y < ys.Count - 1; y++)
                {
                    Vector2 min = WorldToNormalized(new Vector2(xs[x], ys[y]));
                    Vector2 max = WorldToNormalized(new Vector2(xs[x + 1], ys[y + 1]));
                    if (max.x - min.x <= 0.01f || max.y - min.y <= 0.01f) continue;

                    Color fill = index % 2 == 0
                        ? new Color(0.085f, 0.105f, 0.105f, 0.96f)
                        : new Color(0.070f, 0.088f, 0.090f, 0.96f);
                    GameObject block = CreateUiRect($"District Block {index + 1:00}", fill, 2);
                    RectTransform rect = block.GetComponent<RectTransform>();
                    rect.anchorMin = min;
                    rect.anchorMax = max;
                    rect.offsetMin = new Vector2(2f, 2f);
                    rect.offsetMax = new Vector2(-2f, -2f);
                    index++;
                }
            }
        }

        private void DrawMapFrame()
        {
            for (int i = 1; i < 10; i++)
            {
                GameObject vertical = CreateUiRect("Grid Vertical", new Color(0.30f, 0.38f, 0.38f, 0.16f), 3);
                RectTransform vr = vertical.GetComponent<RectTransform>();
                vr.anchorMin = vr.anchorMax = new Vector2(i / 10f, 0.5f);
                vr.sizeDelta = new Vector2(1f, mapArea.rect.height);

                GameObject horizontal = CreateUiRect("Grid Horizontal", new Color(0.30f, 0.38f, 0.38f, 0.16f), 3);
                RectTransform hr = horizontal.GetComponent<RectTransform>();
                hr.anchorMin = hr.anchorMax = new Vector2(0.5f, i / 10f);
                hr.sizeDelta = new Vector2(mapArea.rect.width, 1f);
            }

            CreateBorder("Map Border Top", new Vector2(0.5f, 1f), new Vector2(mapArea.rect.width, 4f));
            CreateBorder("Map Border Bottom", new Vector2(0.5f, 0f), new Vector2(mapArea.rect.width, 4f));
            CreateBorder("Map Border Left", new Vector2(0f, 0.5f), new Vector2(4f, mapArea.rect.height));
            CreateBorder("Map Border Right", new Vector2(1f, 0.5f), new Vector2(4f, mapArea.rect.height));
        }

        private void CreateBorder(string name, Vector2 anchor, Vector2 size)
        {
            GameObject border = CreateUiRect(name, new Color(0.82f, 0.68f, 0.25f, 0.92f), 6);
            RectTransform rect = border.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
        }

        private void DrawRoadNetwork()
        {
            var network=FindFirstObjectByType<ArtifactCourier.Traffic.CityRoadNetwork>();
            if(network!=null && network.paths!=null)
            {
                foreach(var path in network.paths)
                    for(int i=0;i<path.points.Length-1;i++)
                    {
                        var go=CreateUiRect("Curved map road",new Color(.65f,.66f,.53f),9);
                        var segment=go.AddComponent<MapRoadSegment>();
                        segment.Configure(mapArea,WorldToNormalized(path.points[i]),WorldToNormalized(path.points[i+1]));
                    }
                return;
            }
            if (verticalRoads != null)
            {
                foreach (float x in verticalRoads)
                {
                    Vector2 normalized = WorldToNormalized(new Vector2(x, 0f));
                    GameObject casing = CreateUiRect("Minimap Vertical Road Casing", new Color(0.02f, 0.025f, 0.027f, 1f), 8);
                    RectTransform c = casing.GetComponent<RectTransform>();
                    c.anchorMin = c.anchorMax = new Vector2(normalized.x, 0.5f);
                    c.sizeDelta = new Vector2(12f, mapArea.rect.height + 2f);
                    GameObject road = CreateUiRect("Minimap Vertical Road", new Color(0.34f, 0.37f, 0.36f, 1f), 9);
                    RectTransform r = road.GetComponent<RectTransform>();
                    r.sizeDelta = new Vector2(8f, mapArea.rect.height + 2f);
                    r.anchorMin = r.anchorMax = new Vector2(normalized.x, 0.5f);
                    GameObject center = CreateUiRect("Minimap Vertical Road Center", new Color(0.86f, 0.70f, 0.25f, 0.58f), 10);
                    RectTransform m = center.GetComponent<RectTransform>();
                    m.sizeDelta = new Vector2(1.5f, mapArea.rect.height + 2f);
                    m.anchorMin = m.anchorMax = new Vector2(normalized.x, 0.5f);
                }
            }

            if (horizontalRoads != null)
            {
                foreach (float y in horizontalRoads)
                {
                    Vector2 normalized = WorldToNormalized(new Vector2(0f, y));
                    GameObject casing = CreateUiRect("Minimap Horizontal Road Casing", new Color(0.02f, 0.025f, 0.027f, 1f), 8);
                    RectTransform c = casing.GetComponent<RectTransform>();
                    c.anchorMin = c.anchorMax = new Vector2(0.5f, normalized.y);
                    c.sizeDelta = new Vector2(mapArea.rect.width + 2f, 12f);
                    GameObject road = CreateUiRect("Minimap Horizontal Road", new Color(0.34f, 0.37f, 0.36f, 1f), 9);
                    RectTransform r = road.GetComponent<RectTransform>();
                    r.sizeDelta = new Vector2(mapArea.rect.width + 2f, 8f);
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, normalized.y);
                    GameObject center = CreateUiRect("Minimap Horizontal Road Center", new Color(0.86f, 0.70f, 0.25f, 0.58f), 10);
                    RectTransform m = center.GetComponent<RectTransform>();
                    m.sizeDelta = new Vector2(mapArea.rect.width + 2f, 1.5f);
                    m.anchorMin = m.anchorMax = new Vector2(0.5f, normalized.y);
                }
            }
        }

        private void DrawCompass()
        {
            Text north = CreateLabel("N ▲", new Color(0.95f, 0.78f, 0.30f, 1f), 12, 95);
            north.name = "North Compass";
            north.rectTransform.anchorMin = north.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            north.rectTransform.anchoredPosition = new Vector2(0f, -13f);
            north.rectTransform.sizeDelta = new Vector2(50f, 18f);
        }

        private void RebuildDimensions()
        {
            foreach (RectTransform child in mapArea.GetComponentsInChildren<RectTransform>())
            {
                if (child == mapArea) continue;
                if (child.name == "Grid Vertical") child.sizeDelta = new Vector2(1f, mapArea.rect.height);
                if (child.name == "Grid Horizontal") child.sizeDelta = new Vector2(mapArea.rect.width, 1f);
                if (child.name == "Map Border Top" || child.name == "Map Border Bottom") child.sizeDelta = new Vector2(mapArea.rect.width, 4f);
                if (child.name == "Map Border Left" || child.name == "Map Border Right") child.sizeDelta = new Vector2(4f, mapArea.rect.height);
                if (child.name == "Minimap Vertical Road Casing") child.sizeDelta = new Vector2(12f, mapArea.rect.height + 2f);
                if (child.name == "Minimap Vertical Road") child.sizeDelta = new Vector2(8f, mapArea.rect.height + 2f);
                if (child.name == "Minimap Vertical Road Center") child.sizeDelta = new Vector2(1.5f, mapArea.rect.height + 2f);
                if (child.name == "Minimap Horizontal Road Casing") child.sizeDelta = new Vector2(mapArea.rect.width + 2f, 12f);
                if (child.name == "Minimap Horizontal Road") child.sizeDelta = new Vector2(mapArea.rect.width + 2f, 8f);
                if (child.name == "Minimap Horizontal Road Center") child.sizeDelta = new Vector2(mapArea.rect.width + 2f, 1.5f);
            }
        }

        private RectTransform CreateMarker(string objectName, Sprite sprite, Color tint, Vector2 size, int siblingOrder)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(mapArea, false);
            go.transform.SetSiblingIndex(Mathf.Min(siblingOrder, mapArea.childCount - 1));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
            return rect;
        }

        private Text CreateLabel(string textValue, Color color, int size, int siblingOrder)
        {
            GameObject go = new("Map Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(mapArea, false);
            go.transform.SetSiblingIndex(Mathf.Min(siblingOrder, mapArea.childCount - 1));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(70f, 20f);
            Text text = go.GetComponent<Text>();
            text.text = textValue;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateUiRect(string objectName, Color color, int siblingOrder)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(mapArea, false);
            go.transform.SetSiblingIndex(Mathf.Min(siblingOrder, mapArea.childCount - 1));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private Vector2 WorldToMap(Vector2 worldPosition)
        {
            Vector2 size = mapArea.rect.size;
            float normalizedX = Mathf.InverseLerp(-worldWidth * 0.5f, worldWidth * 0.5f, worldPosition.x);
            float normalizedY = Mathf.InverseLerp(-worldHeight * 0.5f, worldHeight * 0.5f, worldPosition.y);
            return new Vector2((normalizedX - 0.5f) * size.x, (normalizedY - 0.5f) * size.y);
        }

        private Vector2 WorldToNormalized(Vector2 worldPosition)
        {
            float normalizedX = Mathf.InverseLerp(-worldWidth * 0.5f, worldWidth * 0.5f, worldPosition.x);
            float normalizedY = Mathf.InverseLerp(-worldHeight * 0.5f, worldHeight * 0.5f, worldPosition.y);
            return new Vector2(normalizedX, normalizedY);
        }

        private bool IsInsideWorld(Vector2 worldPosition)
        {
            return Mathf.Abs(worldPosition.x) <= worldWidth * 0.5f && Mathf.Abs(worldPosition.y) <= worldHeight * 0.5f;
        }

        private Vector2 ClampToMapEdge(Vector2 position, float margin)
        {
            Vector2 half = mapArea.rect.size * 0.5f - Vector2.one * margin;
            return new Vector2(Mathf.Clamp(position.x, -half.x, half.x), Mathf.Clamp(position.y, -half.y, half.y));
        }
    }
}

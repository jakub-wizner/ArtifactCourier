#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArtifactCourier.Editor
{
    // Atlas rectangles use top-left image coordinates. Persistent Sprite assets
    // let generated scenes reference real assets, never transient editor objects.
    internal static class ModernCourierArt
    {
        private const string Root = "Assets/Art/Modern/";
        private static readonly Dictionary<string, Sprite> Sprites = new();

        public static void Build()
        {
            Sprites.Clear();
            Directory.CreateDirectory(Root + "Slices");
            Add("player_car", "courier_atlas", 56, 24, 206, 355, 1.8f);
            Add("traffic_sedan", "courier_atlas", 365, 22, 206, 362, 1.8f);
            Add("traffic_taxi", "courier_atlas", 675, 41, 211, 333, 1.8f);
            Add("police_car", "courier_atlas", 992, 24, 210, 365, 1.8f);
            Add("pickup_truck", "courier_atlas", 56, 393, 206, 337, 1.8f);
            Add("street_racer", "courier_atlas", 370, 400, 197, 323, 1.8f);
            Add("tires", "courier_atlas", 683, 451, 205, 252, 1.2f);
            Add("cone", "courier_atlas", 1016, 485, 163, 180, 0.65f);
            Add("road_straight", "courier_atlas", 43, 747, 238, 225, 4.5f);
            Add("road_horizontal", "courier_atlas", 350, 747, 241, 225, 4.5f);
            Add("road_corner", "courier_atlas", 664, 747, 239, 225, 4.5f);
            Add("road_cross", "courier_atlas", 975, 747, 239, 225, 4.5f);
            Add("barrier", "courier_atlas", 39, 1052, 258, 137, 0.8f);
            Add("oil_slick", "courier_atlas", 362, 1026, 231, 178, 1.7f);
            Add("bonus_nitro", "courier_atlas", 700, 1018, 163, 192, 1f);
            Add("style_coin", "courier_atlas", 1014, 1030, 166, 174, 0.7f);
            Add("robber_car", "enemy_atlas", 112, 33, 251, 402, 1.8f);
            Add("jammer_van", "enemy_atlas", 552, 19, 244, 423, 1.9f);
            Add("tow_truck", "enemy_atlas", 992, 28, 237, 434, 1.95f);
            Add("oil_dropper", "enemy_atlas", 1438, 16, 225, 435, 1.95f);
            Add("monster_truck", "enemy_atlas", 39, 447, 397, 403, 2.4f);
            Add("cairo_boss", "enemy_atlas", 487, 447, 388, 403, 2.4f);
            Add("traffic_van", "enemy_atlas", 991, 467, 237, 385, 1.95f);
            Add("traffic_hatchback", "enemy_atlas", 1434, 516, 232, 332, 1.75f);
            var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "world_manifest.json");
            if (manifest == null) throw new FileNotFoundException("Missing world_manifest.json");
            foreach (var spec in JsonUtility.FromJson<WorldManifest>(manifest.text).sprites)
                Add(spec.key, spec.atlas, spec.x, spec.top, spec.width, spec.height, spec.units, spec.border);
            Alias("recipient", "recipient_0");
            Alias("barricade", "barrier");
            Alias("pulse_pickup", "emp_icon");
            Alias("hud_panel_frame", "ui_panel");
            Alias("minimap_panel_frame", "ui_panel");
            string[] keys = { "class_bike", "class_pickup", "class_car", "class_slash", "class_slam", "class_pulse", "class_wing", "class_shield", "class_radio" };
            int[] tops = { 0, 480, 885 }; int[] heights = { 475, 400, 355 };
            for (int i=0;i<keys.Length;i++) Add(keys[i], "vehicle_classes", i%3*418, tops[i/3], 418, heights[i/3], i<3?2.5f:3f);
            string[] districtKeys={"new_york","tokyo","beijing","paris_france","buenos_aires","moscow","cairo","garden"};
            int[,] districtRects={{8,95,318,478},{340,101,271,475},{619,158,294,397},{929,106,315,465},{10,706,310,448},{328,628,287,523},{619,627,302,539},{929,719,316,423}};
            for(int i=0;i<8;i++)Add("district_"+districtKeys[i],"district_buildings",districtRects[i,0],districtRects[i,1],districtRects[i,2],districtRects[i,3],4f);
            int[] cosmeticTops={0,470,867};int[] cosmeticHeights={465,395,373};
            for(int tier=0;tier<3;tier++)for(int vehicle=0;vehicle<3;vehicle++)
                Add("skin_"+tier+"_"+vehicle,"cosmetic_vehicles",vehicle*418,cosmeticTops[tier],418,cosmeticHeights[tier],2.5f);
            Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
            const string catalogPath="Assets/Resources/CosmeticCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<ArtifactCourier.Player.CosmeticCatalog>(catalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<ArtifactCourier.Player.CosmeticCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
            catalog.skins=new Sprite[9];for(int tier=0;tier<3;tier++)for(int vehicle=0;vehicle<3;vehicle++)catalog.skins[tier*3+vehicle]=Get("skin_"+tier+"_"+vehicle);
            catalog.originals=new[]{Get("class_bike"),Get("class_pickup"),Get("class_car")};
            const string materialPath="Assets/Art/Modern/CosmeticCutout.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("ArtifactCourier/CosmeticCutout"));AssetDatabase.CreateAsset(material,materialPath);}
            catalog.keyedMaterial=material;EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void Add(string name, string atlas, int x, int top, int width, int height, float worldHeight, int border = 0)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + atlas + ".png");
            if (texture == null) throw new System.IO.FileNotFoundException("Missing modern art atlas: " + atlas);
            Rect rect = new Rect(x, texture.height - top - height, width, height);
            Sprite generated = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), height / worldHeight, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            generated.name = name;
            string path = Root + "Slices/" + name + ".asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing == null) { AssetDatabase.CreateAsset(generated, path); existing = generated; }
            else { EditorUtility.CopySerialized(generated, existing); Object.DestroyImmediate(generated); EditorUtility.SetDirty(existing); }
            Sprites[name] = existing;
        }

        [System.Serializable] private sealed class WorldManifest { public WorldSprite[] sprites; }
        [System.Serializable] private sealed class WorldSprite
        { public string key, atlas; public int x, top, width, height, border; public float units; }
        private static void Alias(string key, string source) => Sprites[key] = Sprites[source];
        public static Sprite[] Frames(string folder)
        {
            string kind = folder.Contains("destroy") || folder.Contains("impact") ? "explosion"
                : folder.Contains("pickup") ? "spark" : folder.Contains("boost") ? "nitro" : "pulse";
            return new[] { Get("fx_" + kind + "_0"), Get("fx_" + kind + "_1"), Get("fx_" + kind + "_2"), Get("fx_" + kind + "_3") };
        }

        public static Sprite Find(string path)
        {
            string key = Path.GetFileNameWithoutExtension(path);
            // Active city, vehicle, gameplay and UI artwork all resolves to the modern collection.
            return Sprites.TryGetValue(key, out Sprite sprite) ? sprite : null;
        }
        public static Sprite Get(string key) => Sprites[key];
    }
}
#endif

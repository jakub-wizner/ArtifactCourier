#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using ArtifactCourier.Audio;
using ArtifactCourier.Core;
using ArtifactCourier.Delivery;
using ArtifactCourier.Enemies;
using ArtifactCourier.Player;
using ArtifactCourier.Traffic;
using ArtifactCourier.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArtifactCourier.Editor
{
    public static partial class ArtifactCourierProjectBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const float PixelsPerUnit = 64f;

        private sealed class TrafficRouteSpec
        {
            public Vector2[] Points;
            public int Vehicles;
            public TrafficRouteSpec(Vector2[] points, int vehicles)
            {
                Points = points;
                Vehicles = vehicles;
            }
        }

        private sealed class LevelSpec
        {
            public string SceneName;
            public string CityName;
            public string CityKey;
            public int Deliveries;
            public bool Boss;
            public float WorldWidth;
            public float WorldHeight;
            public float CameraSize;
            public Vector2 PlayerStart;
            public float[] VerticalRoads;
            public float[] HorizontalRoads;
            public Vector2[] PickupPositions;
            public Vector2[] RecipientPositions;
            public string[] RecipientNames;
            public Color[] DeliveryColors;
            public Vector2[] BoostPositions;
            public Vector2[] GasStationPositions;
            public Vector2[] PropPositions;
            public Vector2[] EnemySpawnPositions;
            public EnemyKind[] Enemies;
            public TrafficRouteSpec[] TrafficRoutes;
        }

        private enum EnemyKind { Police, Robber, Jammer, Racer, Tow, Oil, Boss }

        [MenuItem("Tools/Artifact Courier/Build Complete Game")]
        public static void BuildCompleteGame()
        {
            GenerateCompleteGame(true);
        }

        public static void BuildCompleteGameBatch()
        {
            GenerateCompleteGame(false);
        }

        [MenuItem("Tools/Artifact Courier/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            GenerateCompleteGame(false);
            string buildFolder = Path.Combine("Build", "Windows");
            Directory.CreateDirectory(buildFolder);
            string executable = Path.Combine(buildFolder, "ArtifactCourier.exe");
            string[] scenePaths = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                scenePaths[i] = EditorBuildSettings.scenes[i].path;
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Windows build failed. See the Console for the first error.");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Artifact Courier", $"Windows build finished at {executable}", "OK");
            }
        }

        private static void GenerateCompleteGame(bool showDialog)
        {
            Directory.CreateDirectory(ScenesFolder);
            AssetDatabase.Refresh();
            ConfigureSpriteImporters();
            ModernCourierArt.Build();
            AssetDatabase.Refresh();

            BuildMainMenu();
            LevelSpec[] specs = CreateLevelSpecs();
            for (int i = 0; i < specs.Length; i++)
            {
                BuildLevel(i, specs[i]);
            }
            ConfigureBuildSettings(specs);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Artifact Courier", "Build complete. Open MainMenu and press Play.", "OK");
            }
        }

        [MenuItem("Tools/Artifact Courier/Open Main Menu")]
        public static void OpenMainMenu()
        {
            string path = $"{ScenesFolder}/{GameScenes.MainMenu}.unity";
            if (File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Artifact Courier", "Build the complete game first.", "OK");
            }
        }

        private static void ConfigureSpriteImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }
                // Skip unchanged imports on subsequent builds.
                if (importer.textureType == TextureImporterType.Sprite &&
                    importer.spriteImportMode == SpriteImportMode.Single &&
                    Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                    importer.filterMode == FilterMode.Trilinear &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed &&
                    importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Clamp &&
                    importer.alphaIsTransparency && importer.npotScale == TextureImporterNPOTScale.None &&
                    importer.maxTextureSize == 4096) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
        }

        private static void BuildMainMenu()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = GameScenes.MainMenu;
            CreateCamera(new Vector3(0f, 0f, -10f), 6f, new Color(0.015f, 0.025f, 0.06f));
            CreateEventSystem();
            GameObject music = new GameObject("Main Menu Music");
            music.AddComponent<AudioSource>();
            music.AddComponent<CityMusicPlayer>().Configure(ClipAt("Assets/Audio/Music/menu_instrumental.wav"), 0.18f);
            Canvas canvas = CreateCanvas("Main Menu Canvas");
            CreateImage(canvas.transform, "Illustrated City Journey", ModernCourierArt.Get("menu_background"), Color.white, Vector2.zero, Vector2.one);
            GameObject rail = CreatePanel(canvas.transform, "Dispatch Menu", new Color(0.015f,0.03f,0.065f,0.90f), new Vector2(0.035f,0.09f),new Vector2(0.385f,0.94f),Vector2.zero,Vector2.zero);
            CreatePanel(rail.transform, "Gold Accent", new Color(1f,0.72f,0.22f),new Vector2(0.07f,0.95f),new Vector2(0.30f,0.956f),Vector2.zero,Vector2.zero);
            CreateIcon(rail.transform, "Courier Wings", ModernCourierArt.Get("menu_emblem"),new Vector2(0.07f,0.76f),new Vector2(0.38f,0.93f));
            Text edition = CreateText(rail.transform,"Edition","WORLD TOUR / STREET FLOW",15,TextAnchor.MiddleLeft,new Vector2(0.42f,0.78f),new Vector2(0.94f,0.88f),Vector2.zero,Vector2.zero,FontStyle.Bold);
            edition.color = new Color(0.35f,0.87f,1f);
            CreateText(rail.transform,"Title","ARTIFACT\nCOURIER",60,TextAnchor.UpperLeft,new Vector2(0.07f,0.57f),new Vector2(0.95f,0.77f),Vector2.zero,Vector2.zero,FontStyle.Bold);
            Text subtitle = CreateText(rail.transform,"Tagline","PRECIOUS CARGO. EXTRAORDINARY CITIES.",14,TextAnchor.MiddleLeft,new Vector2(0.07f,0.51f),new Vector2(0.94f,0.57f),Vector2.zero,Vector2.zero);
            subtitle.color = new Color(0.8f,0.87f,0.95f);
            Button start = CreateButton(rail.transform,"New Game Button","START YOUR JOURNEY",new Vector2(0.07f,0.39f),new Vector2(0.93f,0.48f));
            Button load = CreateButton(rail.transform,"Load Game Button","CONTINUE DELIVERY",new Vector2(0.07f,0.28f),new Vector2(0.93f,0.37f));
            Button quit = CreateButton(rail.transform,"Quit Button","EXIT",new Vector2(0.07f,0.17f),new Vector2(0.93f,0.26f));
            Text info = CreateText(rail.transform,"Save Info","No saved journey",15,TextAnchor.MiddleLeft,new Vector2(0.08f,0.105f),new Vector2(0.92f,0.16f),Vector2.zero,Vector2.zero);
            CreateText(rail.transform,"Controls","WASD DRIVE    SHIFT NITRO    CTRL DRIFT\nSPACE ATTACK    M MAP    ESC PAUSE",13,TextAnchor.MiddleLeft,new Vector2(0.08f,0.02f),new Vector2(0.93f,0.10f),Vector2.zero,Vector2.zero);
            GameObject cities = CreatePanel(canvas.transform,"Seven Cities",new Color(0.02f,0.035f,0.07f,0.86f),new Vector2(0.43f,0.025f),new Vector2(0.975f,0.205f),Vector2.zero,Vector2.zero);
            string[] keys = { "new_york", "tokyo", "beijing", "paris_france", "buenos_aires", "moscow", "cairo" };
            string[] names = { "NEW YORK", "TOKYO", "BEIJING", "PARIS", "BUENOS AIRES", "MOSCOW", "CAIRO" };
            for (int i=0;i<keys.Length;i++)
            {
                float x = 0.015f + i * 0.14f;
                CreateIcon(cities.transform,"City " + names[i],ModernCourierArt.Get(keys[i]+"_landmark"),new Vector2(x,0.25f),new Vector2(x+0.13f,0.95f));
                CreateText(cities.transform,"City Name",names[i],11,TextAnchor.MiddleCenter,new Vector2(x,0.03f),new Vector2(x+0.13f,0.22f),Vector2.zero,Vector2.zero,FontStyle.Bold);
            }
            CreateGarageSelection(canvas.transform);
            new GameObject("MainMenuController").AddComponent<MainMenuController>().Configure(start,load,quit,info);
            SaveScene(scene, GameScenes.MainMenu);
        }

        private static void BuildLevel(int index, LevelSpec spec)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = spec.SceneName;

            Transform root = new GameObject($"LEVEL {index + 1} - {spec.CityName}").transform;
            LevelController level = new GameObject("LevelController").AddComponent<LevelController>();
            level.Configure(index, spec.CityName, spec.Deliveries, spec.Boss, spec.Enemies.Length);

            CreateWorld(spec, root);
            GameObject player = CreatePlayer(root, spec.PlayerStart, index);
            CreateCamera(player.transform.position + Vector3.back * 10f, spec.CameraSize * 1.30f, new Color(0.12f, 0.14f, 0.15f), player.transform);
            CreateCityMusic(spec);
            CreateEventSystem();
            CreateDeliveryObjects(index, spec, root);
            CreateBoostPads(spec, root);
            CreateBonuses(index, spec, root);
            CreateGasStations(index, spec, root);
            CreateTraffic(index, spec, root);
            CreateEnemies(index, spec, root);
            CreateLevelUI(index, spec);
            EvolveCity(index, spec, root);
            SaveScene(scene, spec.SceneName);
        }

        private static void CreateWorld(LevelSpec spec, Transform parent)
        {
            Transform world = new GameObject("World").transform;
            world.SetParent(parent);

            Sprite groundSprite = SpriteAt($"Assets/Art/Sprites/Cities/{spec.CityKey}_ground.png");
            Sprite propSprite = SpriteAt($"Assets/Art/Sprites/Props/{spec.CityKey}_street_prop.png");

            CreateTiledSurface("City Paving", groundSprite, Vector2.zero, world, -30, new Vector2(spec.WorldWidth + 80f, spec.WorldHeight + 80f));
            CreateDetailedPerimeter(spec, world);

            // Repeat short road sections rather than stretching lane markings over the whole city.
            for (int i = 0; i < spec.VerticalRoads.Length; i++)
                CreateModernRoad(world, new Vector2(spec.VerticalRoads[i], 0f), spec.WorldHeight, false, CityRoadTint(spec.CityKey));
            for (int i = 0; i < spec.HorizontalRoads.Length; i++)
                CreateModernRoad(world, new Vector2(0f, spec.HorizontalRoads[i]), spec.WorldWidth, true, CityRoadTint(spec.CityKey));
            foreach (float x in spec.VerticalRoads)
                foreach (float y in spec.HorizontalRoads)
                {
                    Sprite cross = ModernCourierArt.Get("road_cross");
                    CreateSpriteObject("Road Junction", cross, new Vector2(x, y), world, -7,
                        new Vector3(4.5f / cross.bounds.size.x, 4.5f / cross.bounds.size.y, 1f)).GetComponent<SpriteRenderer>().color = CityRoadTint(spec.CityKey);
                }

            CreateAvenueDetails(spec, world);
            CreateDistrictLots(spec, world);
            CreateMissionContextLots(spec, world);
            CreateCityLandmarks(spec, world);

            for (int i = 0; i < spec.PropPositions.Length; i++)
            {
                Sprite detail = i % 4 == 0 ? propSprite : ModernCourierArt.Get(i % 4 == 1 ? "cone" : i % 4 == 2 ? "tires" : "barrier");
                CreateSizedSprite($"Street Prop {i + 1:00}", detail, spec.PropPositions[i], world, -2, 1.8f);
            }

            // Keep the entire visible rectangle reachable. Boundaries sit just outside the usable map,
            // while district lots leave a clear perimeter lane so no building seals off an edge.
            float halfW = spec.WorldWidth * 0.5f;
            float halfH = spec.WorldHeight * 0.5f;
            const float boundaryThickness = 1f;
            CreateBoundary(world, "North Boundary", new Vector2(0f, halfH + boundaryThickness * 0.5f), new Vector2(spec.WorldWidth + 2f, boundaryThickness));
            CreateBoundary(world, "South Boundary", new Vector2(0f, -halfH - boundaryThickness * 0.5f), new Vector2(spec.WorldWidth + 2f, boundaryThickness));
            CreateBoundary(world, "West Boundary", new Vector2(-halfW - boundaryThickness * 0.5f, 0f), new Vector2(boundaryThickness, spec.WorldHeight + 2f));
            CreateBoundary(world, "East Boundary", new Vector2(halfW + boundaryThickness * 0.5f, 0f), new Vector2(boundaryThickness, spec.WorldHeight + 2f));
        }


        private static void CreateMissionContextLots(LevelSpec spec, Transform parent)
        {
            for (int i=0; i<Mathf.Min(2,spec.PickupPositions.Length); i++)
            {
                string kind = i==0 ? "park" : "parking";
                CreateSizedSprite("Mission " + kind, ModernCourierArt.Get(spec.CityKey+"_"+kind),spec.PickupPositions[i],parent,-5,5.8f);
            }
        }

        private static void CreateCityLandmarks(LevelSpec spec, Transform parent)
        {
            // The skyline landmark is outside the driving boundary, so its footprint cannot seal a route.
            CreateSizedSprite("City Landmark",ModernCourierArt.Get(spec.CityKey+"_landmark"),
                new Vector2(spec.WorldWidth*0.18f,spec.WorldHeight*0.5f+8f),parent,-2,9f);
        }

        private static void CreateDistrictLots(LevelSpec spec, Transform parent)
        {
            // Fill actual blocks between roads. A fixed grid previously skipped most lots where it hit roads.
            float[] xs = (float[])spec.VerticalRoads.Clone();
            float[] ys = (float[])spec.HorizontalRoads.Clone();
            System.Array.Sort(xs); System.Array.Sort(ys);
            string[] types = { "house", "shop", "park", "cinema", "parking", "church", "depot", "border" };
            int index=0;
            for(int x=0;x<xs.Length-1;x++)
                for(int y=0;y<ys.Length-1;y++)
                {
                    float roomX = xs[x+1]-xs[x]-4.5f, roomY=ys[y+1]-ys[y]-4.5f;
                    float size = Mathf.Clamp(Mathf.Min(roomX,roomY)*0.32f,2.7f,4.5f);
                    if(Mathf.Min(roomX,roomY)<7f) continue;
                    Vector2 center = new Vector2((xs[x]+xs[x+1])*0.5f,(ys[y]+ys[y+1])*0.5f);
                    foreach(float dx in new[]{-0.27f,0.27f})
                        foreach(float dy in new[]{-0.27f,0.27f})
                        {
                            Vector2 position=center+new Vector2(dx*roomX,dy*roomY);
                            if(NearImportantPoint(position,spec,size*0.71f+2.0f)) continue;
                            string kind=types[(index++ + spec.Deliveries)%types.Length];
                            GameObject lot=CreateSizedSprite("District " + kind,ModernCourierArt.Get(spec.CityKey+"_"+kind),position,parent,-4,size);
                            if(kind!="park" && kind!="parking")
                                lot.AddComponent<BoxCollider2D>().size=GetSpriteColliderSize(lot.GetComponent<SpriteRenderer>(),0.78f);
                        }
                }
        }

        private static void CreateAvenueDetails(LevelSpec spec,Transform parent)
        {
            int index=0;
            foreach(float roadX in spec.VerticalRoads)
                for(float y=-spec.WorldHeight*0.5f+5f;y<spec.WorldHeight*0.5f-4f;y+=7.5f)
                    foreach(float side in new[]{-1f,1f})
                    {
                        Vector2 pos=new Vector2(roadX+side*3.85f,y);
                        if(Mathf.Abs(pos.x)>spec.WorldWidth*0.5f-1.7f || NearAnyRoad(y,spec.HorizontalRoads,4f) || NearImportantPoint(pos,spec,3f)) continue;
                        string kind=index++%4==0?"street_prop":"tree";
                        CreateSizedSprite("Avenue " + kind,ModernCourierArt.Get(spec.CityKey+"_"+kind),pos,parent,-2,kind=="tree"?2.6f:1.7f);
                    }
        }

        private static Color CityRoadTint(string city) => city switch
        {
            "tokyo" => new Color(0.79f,0.86f,1f), "beijing" => new Color(1f,0.95f,0.86f),
            "paris_france" => new Color(0.95f,0.95f,1f), "buenos_aires" => new Color(1f,0.91f,0.83f),
            "moscow" => new Color(0.80f,0.91f,1f), "cairo" => new Color(1f,0.88f,0.69f), _ => Color.white
        };

        private static void CreateDetailedPerimeter(LevelSpec spec, Transform parent)
        {
            Transform border=new GameObject("Detailed City Perimeter").transform; border.SetParent(parent,false);
            float hw=spec.WorldWidth*0.5f, hh=spec.WorldHeight*0.5f;
            Sprite paving=ModernCourierArt.Get(spec.CityKey+"_ground");
            // A visible sidewalk separates the playable lane from the surrounding neighborhood.
            CreateTiledSurface("North Promenade",paving,new Vector2(0,hh+1.5f),border,-15,new Vector2(spec.WorldWidth+6f,3f));
            CreateTiledSurface("South Promenade",paving,new Vector2(0,-hh-1.5f),border,-15,new Vector2(spec.WorldWidth+6f,3f));
            CreateTiledSurface("West Promenade",paving,new Vector2(-hw-1.5f,0),border,-15,new Vector2(3f,spec.WorldHeight));
            CreateTiledSurface("East Promenade",paving,new Vector2(hw+1.5f,0),border,-15,new Vector2(3f,spec.WorldHeight));
            // Match visible road closures to the collision boundary rather than ending asphalt in empty paving.
            foreach(float x in spec.VerticalRoads)
                foreach(float sign in new[]{-1f,1f})
                    CreateSizedSprite("Road End Barrier",ModernCourierArt.Get("barrier"),new Vector2(x,sign*(hh+0.25f)),border,1,4.4f);
            foreach(float y in spec.HorizontalRoads)
                foreach(float sign in new[]{-1f,1f})
                    CreateSizedSprite("Road End Barrier",ModernCourierArt.Get("barrier"),new Vector2(sign*(hw+0.25f),y),border,1,4.4f).transform.rotation=Quaternion.Euler(0,0,90);
            string[] kinds={"border","house","shop","tree","depot","park","cinema","street_prop"};
            int serial=0;
            for(int layer=0;layer<3;layer++)
            {
                float distance=6.5f+layer*10f;
                for(int side=0;side<4;side++)
                {
                    bool horizontal=side<2; float half=horizontal?hw:hh;
                    for(float t=-half-20f+layer*3f;t<=half+20f;t+=8f)
                    {
                        int seed=serial++;
                        string kind=kinds[(seed*3+layer)%kinds.Length];
                        Vector2 pos=horizontal?new Vector2(t,(side==0?1:-1)*(hh+distance)):new Vector2((side==2?1:-1)*(hw+distance),t);
                        if(layer==0 && Vector2.Distance(pos,new Vector2(spec.WorldWidth*0.18f,hh+8f))<8f) continue;
                        float size=kind=="tree"?3.5f:kind=="street_prop"?2.5f:6.2f+(seed%3)*0.6f;
                        var go=CreateSizedSprite("Perimeter " + kind,ModernCourierArt.Get(spec.CityKey+"_"+kind),pos,border,-6-layer,size);
                        go.GetComponent<SpriteRenderer>().color=Color.Lerp(Color.white,new Color(0.60f,0.67f,0.76f),layer*0.22f);
                    }
                }
            }
            // Small, readable details at the near edge interrupt long rows of buildings.
            for(float x=-hw+3f;x<hw;x+=9f)
                foreach(float sign in new[]{-1f,1f})
                    CreateSizedSprite("Promenade Detail",ModernCourierArt.Get(spec.CityKey+"_street_prop"),new Vector2(x,sign*(hh+2.2f)),border,-1,1.65f);
        }

        private static GameObject CreateSizedSprite(string name,Sprite sprite,Vector2 position,Transform parent,int order,float longestSide)
        {
            if(sprite==null) throw new System.InvalidOperationException("Missing sprite: "+name);
            float size=Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y);
            return CreateSpriteObject(name,sprite,position,parent,order,Vector3.one*(longestSide/size));
        }

        private static GameObject CreateTiledSurface(string name,Sprite sprite,Vector2 position,Transform parent,int order,Vector2 size)
        {
            GameObject go=CreateSpriteObject(name,sprite,position,parent,order,Vector3.one);
            var renderer=go.GetComponent<SpriteRenderer>(); renderer.drawMode=SpriteDrawMode.Tiled;
            renderer.tileMode=SpriteTileMode.Continuous; renderer.size=size;
            return go;
        }

        private static bool NearImportantPoint(Vector2 position, LevelSpec spec, float radius)
        {
            if (NearAnyPoint(position, spec.PickupPositions, radius)) return true;
            if (NearAnyPoint(position, spec.RecipientPositions, radius)) return true;
            if (NearAnyPoint(position, spec.GasStationPositions, radius)) return true;
            if (NearAnyPoint(position, spec.BoostPositions, radius * 0.75f)) return true;
            if (Vector2.Distance(position, spec.PlayerStart) < radius) return true;
            return false;
        }

        private static bool NearAnyPoint(Vector2 position, Vector2[] points, float radius)
        {
            if (points == null) return false;
            for (int i = 0; i < points.Length; i++)
            {
                if (Vector2.Distance(position, points[i]) < radius) return true;
            }
            return false;
        }

        private static bool NearAnyRoad(float value, float[] roadPositions, float distance)
        {
            for (int i = 0; i < roadPositions.Length; i++)
            {
                if (Mathf.Abs(value - roadPositions[i]) < distance)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CreateModernRoad(Transform parent, Vector2 position, float length, bool horizontal, Color tint)
        {
            Sprite sprite = ModernCourierArt.Get("road_straight");
            GameObject go = CreateSpriteObject("Asphalt Lane", sprite, position, parent, -10, Vector3.one);
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            // Sprite is about 4.5 world units wide, so the road contains one column of tiles.
            go.transform.localScale = new Vector3(4.5f / sprite.bounds.size.x, 1f, 1f);
            renderer.size = new Vector2(sprite.bounds.size.x, length);
            if (horizontal) go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static GameObject CreatePlayer(Transform parent, Vector2 startPosition, int levelIndex)
        {
            GameObject go = CreateSpriteObject("Player", SpriteAt("Assets/Art/Sprites/Vehicles/player_car.png"), startPosition, parent, 10, Vector3.one * 1.45f);
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.mass = 1.3f;
            body.angularDamping = 2.8f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.88f, 1.52f);
            collider.direction = CapsuleDirection2D.Vertical;
            PhysicsMaterial2D playerMaterial = new("Player Low Friction") { friction = 0f, bounciness = 0.04f };
            collider.sharedMaterial = playerMaterial;

            go.AddComponent<CarController>();
            go.AddComponent<ArtifactCourier.Driving.DrivingStyle>();
            go.AddComponent<ArtifactCourier.Driving.DriftTrails>();
            go.AddComponent<PlayerCargo>();
            PlayerHealth playerHealth = go.AddComponent<PlayerHealth>();
            playerHealth.Configure(CombatBalance.PlayerMaxHealth(levelIndex));
            go.AddComponent<PlayerStatusEffects>();
            go.AddComponent<AudioListener>();
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            VehicleAudio vehicleAudio = go.AddComponent<VehicleAudio>();
            vehicleAudio.Configure(ClipAt("Assets/Audio/SFX/engine_player_loop.wav"), 0.28f, 0f);

            PlayerVisualEffects vfx = go.AddComponent<PlayerVisualEffects>();
            vfx.Configure(
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/player_attack"),
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/pickup_burst"),
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/charging"),
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/boost_blue"));

            PlayerPulseAttack pulse = go.AddComponent<PlayerPulseAttack>();
            pulse.Configure(
                ClipAt("Assets/Audio/SFX/pulse.wav"),
                levelIndex,
                SpriteAt("Assets/Art/Sprites/Gameplay/player_projectile.png"),
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/player_emp"),
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/player_ranged_muzzle"),
                LoadSpritesFromFolder("Assets/Art/Sprites/Effects/player_ranged_impact"));
            go.AddComponent<VehicleLoadout>().Configure(new[] { ModernCourierArt.Get("class_bike"), ModernCourierArt.Get("class_pickup"), ModernCourierArt.Get("class_car") },
                new[] { ModernCourierArt.Get("class_slash"), ModernCourierArt.Get("class_slam"), ModernCourierArt.Get("class_pulse") }, levelIndex);
            return go;
        }

        private static void CreateDeliveryObjects(int levelIndex, LevelSpec spec, Transform parent)
        {
            Transform deliveries = new GameObject("Deliveries").transform;
            deliveries.SetParent(parent);
            string[] artifactKinds = { "artifact", "artifact_vase", "artifact_mask", "artifact_jade", "artifact_compass" };
            AudioClip pickup = ClipAt("Assets/Audio/SFX/pickup.wav");
            AudioClip delivery = ClipAt("Assets/Audio/SFX/delivery.wav");

            for (int i = 0; i < spec.Deliveries; i++)
            {
                string id = $"L{levelIndex + 1:00}-ART-{i + 1:00}";
                Color color = spec.DeliveryColors[i % spec.DeliveryColors.Length];
                string recipientName = spec.RecipientNames[i % spec.RecipientNames.Length];

                Sprite artifactSprite=ModernCourierArt.Get(artifactKinds[(i+levelIndex)%artifactKinds.Length]);
                Sprite recipientSprite=ModernCourierArt.Get("recipient_"+((i+levelIndex)%8));
                GameObject pickupGo = CreateSizedSprite($"Artifact {i + 1:00} [{id}]", artifactSprite, spec.PickupPositions[i], deliveries, 8, 2.5f);
                CircleCollider2D pickupCollider = pickupGo.AddComponent<CircleCollider2D>();
                pickupCollider.isTrigger = true;
                pickupCollider.radius = 1.35f / pickupGo.transform.localScale.x;
                pickupGo.AddComponent<ArtifactPickup>().Configure(id, pickup, color);
                AddDeliveryBeacon(pickupGo, color, (i+1).ToString());

                GameObject recipientGo = CreateSizedSprite($"Recipient {i + 1:00} [{recipientName}]", recipientSprite, spec.RecipientPositions[i], deliveries, 7, 2.5f);
                CircleCollider2D recipientCollider = recipientGo.AddComponent<CircleCollider2D>();
                recipientCollider.isTrigger = true;
                recipientCollider.radius = 1.4f / recipientGo.transform.localScale.x;
                recipientGo.AddComponent<DeliveryRecipient>().Configure(id, delivery, color, recipientName);
                AddDeliveryBeacon(recipientGo, color, (i+1).ToString());
            }
        }

        private static void CreateBoostPads(LevelSpec spec, Transform parent)
        {
            Transform boosts = new GameObject("Speed Boosters").transform;
            boosts.SetParent(parent);
            Sprite sprite = SpriteAt("Assets/Art/Sprites/Gameplay/speed_boost.png");
            AudioClip clip = ClipAt("Assets/Audio/SFX/boost.wav");
            for (int i = 0; i < spec.BoostPositions.Length; i++)
            {
                GameObject go = CreateSizedSprite($"Speed Boost {i + 1}", sprite, spec.BoostPositions[i], boosts, 4, 3.0f);
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(2.5f, 2.5f) / go.transform.localScale.x;
                go.AddComponent<SpeedBoostPad>().Configure(clip, 1.7f, 5f);
            }
        }

        private static void CreateBonuses(int levelIndex, LevelSpec spec, Transform parent)
        {
            Transform bonuses = new GameObject("Level Bonuses").transform;
            bonuses.SetParent(parent);
            AudioClip pickupClip = ClipAt("Assets/Audio/SFX/pickup.wav");
            float hw = spec.WorldWidth * 0.5f;
            float hh = spec.WorldHeight * 0.5f;

            // Every level keeps a repair option, while later levels introduce temporary combat and survival choices.
            CreateBonusObject(bonuses, BonusKind.RepairKit, new Vector2(-hw + 8f, hh - 8f), 32f + levelIndex * 6f, 0f, "bonus_repair", pickupClip);
            if (levelIndex >= 1)
            {
                CreateBonusObject(bonuses, BonusKind.Shield, new Vector2(hw - 10f, -hh + 8f), 28f + levelIndex * 9f, 0f, "bonus_shield", pickupClip);
                CreateBonusObject(bonuses, BonusKind.Nitro, new Vector2(-hw * 0.15f, hh - 7f), 1.38f + levelIndex * 0.04f, 8f, "bonus_nitro", pickupClip);
            }
            if (levelIndex >= 2)
            {
                CreateBonusObject(bonuses, BonusKind.CooldownChip, new Vector2(hw * 0.20f, -hh + 7f), 0.62f, 12f, "bonus_cooldown", pickupClip);
                CreateBonusObject(bonuses, BonusKind.AttackOverdrive, new Vector2(hw - 11f, hh * 0.20f), 1.6f, 10f, "bonus_overdrive", pickupClip);
            }
            if (levelIndex >= 3)
            {
                CreateBonusObject(bonuses, BonusKind.CargoExpansion, new Vector2(hw - 9f, hh - 9f), 1f, 0f, "bonus_cargo", pickupClip);
                CreateBonusObject(bonuses, BonusKind.Invulnerability, new Vector2(-hw + 11f, -hh * 0.10f), 0f, 3.2f, "bonus_invulnerability", pickupClip);
            }
            if (levelIndex >= 4)
            {
                CreateBonusObject(bonuses, BonusKind.Shield, new Vector2(-hw + 11f, -hh + 9f), 65f, 0f, "bonus_shield", pickupClip);
                CreateBonusObject(bonuses, BonusKind.Nitro, new Vector2(hw - 12f, 0f), 1.58f, 10f, "bonus_nitro", pickupClip);
                CreateBonusObject(bonuses, BonusKind.AttackOverdrive, new Vector2(0f, hh - 8f), 1.85f, 11f, "bonus_overdrive", pickupClip);
            }
        }

        private static void CreateBonusObject(Transform parent, BonusKind kind, Vector2 position, float amount, float duration, string spriteName, AudioClip clip)
        {
            GameObject go = CreateSizedSprite($"Bonus - {kind}", SpriteAt($"Assets/Art/Sprites/Gameplay/{spriteName}.png"), position, parent, 8, 2.5f);
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.2f / go.transform.localScale.x;
            go.AddComponent<BonusPickup>().Configure(kind, amount, duration, clip);
        }

        private static void CreateGasStations(int levelIndex, LevelSpec spec, Transform parent)
        {
            Transform root = new GameObject("Gas Stations").transform;
            root.SetParent(parent);
            Sprite sprite = SpriteAt("Assets/Art/Sprites/Gameplay/gas_station.png");
            AudioClip clip = ClipAt("Assets/Audio/SFX/heal_station.wav");
            float healRate = 12f + levelIndex * 4f;
            for (int i = 0; i < spec.GasStationPositions.Length; i++)
            {
                GameObject station = CreateSizedSprite($"Gas Station {i + 1:00}", sprite, spec.GasStationPositions[i], root, 6, 4.4f);
                BoxCollider2D collider = station.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(3.0f, 2.6f);
                station.AddComponent<GasStation>().Configure(healRate, 3.0f, clip);
            }
        }

        private static void CreateCityMusic(LevelSpec spec)
        {
            GameObject musicObject = new GameObject("City Music");
            musicObject.AddComponent<AudioSource>();
            CityMusicPlayer player = musicObject.AddComponent<CityMusicPlayer>();
            player.Configure(ClipAt($"Assets/Audio/Music/{spec.CityKey}_instrumental.wav"), 0.20f);
        }

        private static void CreateTraffic(int levelIndex, LevelSpec spec, Transform parent)
        {
            Transform trafficRoot = new GameObject("Civilian Traffic").transform;
            trafficRoot.SetParent(parent);
            string[] sprites = { "traffic_sedan", "traffic_taxi", "traffic_hatchback", "traffic_van" };
            int globalIndex = 0;

            for (int routeIndex = 0; routeIndex < spec.TrafficRoutes.Length; routeIndex++)
            {
                TrafficRouteSpec routeSpec = spec.TrafficRoutes[routeIndex];
                Transform routeRoot = new GameObject($"Traffic Route {routeIndex + 1:00}").transform;
                routeRoot.SetParent(trafficRoot);
                Transform[] route = new Transform[routeSpec.Points.Length];
                for (int i = 0; i < routeSpec.Points.Length; i++)
                {
                    Transform wp = new GameObject($"Waypoint {routeIndex + 1:00}-{i + 1:00}").transform;
                    wp.SetParent(routeRoot);
                    wp.position = routeSpec.Points[i];
                    route[i] = wp;
                }

                for (int vehicleIndex = 0; vehicleIndex < routeSpec.Vehicles; vehicleIndex++)
                {
                    string spriteName = sprites[globalIndex % sprites.Length];
                    Vector2 start = routeSpec.Points[vehicleIndex % routeSpec.Points.Length] + new Vector2((vehicleIndex % 2) * 1.4f, 0f);
                    GameObject go = CreateSpriteObject($"Traffic {globalIndex + 1:00} - {spriteName}", SpriteAt($"Assets/Art/Sprites/Vehicles/{spriteName}.png"), start, trafficRoot, 5, Vector3.one * 1.22f);
                    Rigidbody2D body = go.AddComponent<Rigidbody2D>();
                    body.mass = 2.0f;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    body.gravityScale = 0f;
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(0.88f, 1.48f);
                    go.AddComponent<AudioSource>();
                    TrafficVehicle traffic = go.AddComponent<TrafficVehicle>();
                    traffic.Configure(route, 4.1f + 0.25f * levelIndex + 0.05f * routeIndex, ClipAt("Assets/Audio/SFX/engine_traffic_loop.wav"), ClipAt("Assets/Audio/SFX/horn.wav"), ClipAt("Assets/Audio/SFX/crash.wav"), LoadSpritesFromFolder("Assets/Art/Sprites/Effects/enemy_destroy"), 2 + levelIndex / 3, 4f + levelIndex * 1.1f);
                    globalIndex++;
                }
            }
        }

        private static void CreateEnemies(int levelIndex, LevelSpec spec, Transform parent)
        {
            Transform root = new GameObject("Enemies").transform;
            root.SetParent(parent);
            Sprite[] destroyFrames = LoadSpritesFromFolder("Assets/Art/Sprites/Effects/enemy_destroy");
            Sprite[] muzzleFrames = LoadSpritesFromFolder("Assets/Art/Sprites/Effects/boss_muzzle");
            Sprite[] impactFrames = LoadSpritesFromFolder("Assets/Art/Sprites/Effects/projectile_impact");
            Sprite projectileSprite = SpriteAt("Assets/Art/Sprites/Gameplay/boss_projectile.png");

            for (int i = 0; i < spec.Enemies.Length; i++)
            {
                Vector2 pos = spec.EnemySpawnPositions[i % spec.EnemySpawnPositions.Length];
                EnemyKind kind = spec.Enemies[i];
                string enemyKey = EnemyKey(kind);
                Sprite[] attackFrames = LoadSpritesFromFolder($"Assets/Art/Sprites/Effects/enemy_{enemyKey}_attack");
                CombatBalance.EnemyTuning tuning = CombatBalance.Enemy(enemyKey, levelIndex);

                switch (kind)
                {
                    case EnemyKind.Police:
                        CreateEnemy<PoliceInterceptor>("Police Interceptor", "police_car", pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, false, false, 1.28f, destroyFrames, attackFrames);
                        break;
                    case EnemyKind.Robber:
                        RobberCar robber = CreateEnemy<RobberCar>("Robber Car", "robber_car", pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, false, false, 1.28f, destroyFrames, attackFrames);
                        robber.ConfigureRobber(ClipAt("Assets/Audio/SFX/steal.wav"), SpriteAt("Assets/Art/Sprites/Gameplay/artifact.png"), ClipAt("Assets/Audio/SFX/pickup.wav"));
                        break;
                    case EnemyKind.Jammer:
                        JammerVan jammer = CreateEnemy<JammerVan>("Jammer Van", "jammer_van", pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, false, false, 1.30f, destroyFrames, attackFrames);
                        jammer.ConfigureJammer(ClipAt("Assets/Audio/SFX/slow.wav"));
                        break;
                    case EnemyKind.Racer:
                        CreateEnemy<StreetRacer>("Street Racer", "street_racer", pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, false, false, 1.24f, destroyFrames, attackFrames);
                        break;
                    case EnemyKind.Tow:
                        TowTruckEnemy tow = CreateEnemy<TowTruckEnemy>("Tow Truck", "tow_truck", pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, false, false, 1.32f, destroyFrames, attackFrames);
                        tow.ConfigureTow(ClipAt("Assets/Audio/SFX/slow.wav"));
                        break;
                    case EnemyKind.Oil:
                        OilDropperCar oil = CreateEnemy<OilDropperCar>("Oil Dropper", "oil_dropper", pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, false, false, 1.30f, destroyFrames, attackFrames);
                        oil.ConfigureOil(SpriteAt("Assets/Art/Sprites/Gameplay/oil_slick.png"), ClipAt("Assets/Audio/SFX/slow.wav"));
                        break;
                    case EnemyKind.Boss:
                        string bossSprite = spec.CityKey == "cairo" ? "cairo_boss" : "monster_truck";
                        string bossName = spec.CityKey == "cairo" ? "CITY BOSS - Sand Reaper" : "CITY BOSS - Monster Truck";
                        MonsterTruckBoss boss = CreateEnemy<MonsterTruckBoss>(bossName, bossSprite, pos, root, tuning.Speed, tuning.Health, tuning.ContactDamage, true, true, 2.2f, destroyFrames, attackFrames);
                        boss.ConfigureBoss(ClipAt("Assets/Audio/SFX/boss_hit.wav"), projectileSprite, muzzleFrames, impactFrames);
                        break;
                }
            }
        }

        private static string EnemyKey(EnemyKind kind)
        {
            return kind switch
            {
                EnemyKind.Police => "police",
                EnemyKind.Robber => "robber",
                EnemyKind.Jammer => "jammer",
                EnemyKind.Racer => "racer",
                EnemyKind.Tow => "tow",
                EnemyKind.Oil => "oil",
                EnemyKind.Boss => "boss",
                _ => "generic"
            };
        }

        private static T CreateEnemy<T>(string displayName, string spriteName, Vector2 position, Transform parent, float speed, int health, float damage, bool isBoss, bool monsterAudio, float scaleMultiplier, Sprite[] destroyFrames, Sprite[] attackFrames) where T : EnemyVehicle
        {
            GameObject go = CreateSpriteObject(displayName, SpriteAt($"Assets/Art/Sprites/Vehicles/{spriteName}.png"), position, parent, 9, Vector3.one * scaleMultiplier);
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.mass = isBoss ? 6f : 2f;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = isBoss ? new Vector2(1.55f, 2.15f) : new Vector2(0.88f, 1.50f);
            T enemy = go.AddComponent<T>();
            enemy.ConfigureBase(speed, health, damage, isBoss, ClipAt("Assets/Audio/SFX/crash.wav"), isBoss ? ClipAt("Assets/Audio/SFX/boss_defeat.wav") : ClipAt("Assets/Audio/SFX/crash.wav"), destroyFrames, attackFrames);
            go.AddComponent<AudioSource>();
            VehicleAudio audio = go.AddComponent<VehicleAudio>();
            audio.Configure(ClipAt(monsterAudio ? "Assets/Audio/SFX/engine_monster_loop.wav" : "Assets/Audio/SFX/engine_enemy_loop.wav"), isBoss ? 0.38f : 0.20f, 0.7f);

            if (typeof(T) == typeof(PoliceInterceptor))
            {
                AudioSource siren = go.AddComponent<AudioSource>();
                siren.clip = ClipAt("Assets/Audio/SFX/police_siren_loop.wav");
                siren.loop = true;
                siren.playOnAwake = true;
                siren.volume = 0.14f;
                siren.spatialBlend = 0.7f;
            }
            return enemy;
        }

        private static void CreateLevelUI(int levelIndex, LevelSpec spec)
        {
            Canvas canvas = CreateCanvas("Gameplay Canvas");

            GameObject hud = CreatePanel(canvas.transform, "HUD Panel", new Color(0.008f, 0.013f, 0.015f, 0.975f), new Vector2(0.012f, 0.64f), new Vector2(0.30f, 0.985f), Vector2.zero, Vector2.zero);
            CreatePanel(hud.transform, "Cyan Accent", new Color(0.18f, 0.86f, 1f), new Vector2(0f, 0.99f), Vector2.one, Vector2.zero, Vector2.zero);
            Text city = CreateText(hud.transform, "City", spec.CityName, 25, TextAnchor.UpperLeft, new Vector2(0.055f,0.865f), new Vector2(0.945f,0.965f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            city.color = new Color(1f, 0.76f, 0.20f, 1f);
            Text objective = CreateText(hud.transform, "Objective", "OBJECTIVE", 13, TextAnchor.UpperLeft, new Vector2(0.055f,0.755f), new Vector2(0.945f,0.855f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            objective.color = new Color(0.82f, 0.90f, 0.88f, 1f);

            Text hp = CreateText(hud.transform, "Health", "HP", 16, TextAnchor.MiddleLeft, new Vector2(0.055f,0.655f), new Vector2(0.945f,0.735f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            GameObject healthTrack = CreatePanel(hud.transform, "Health Track", new Color(0.08f,0.10f,0.10f,1f), new Vector2(0.055f,0.605f), new Vector2(0.945f,0.645f), Vector2.zero, Vector2.zero);
            GameObject healthFillObject = CreatePanel(healthTrack.transform, "Health Fill", new Color(0.25f,0.88f,0.38f,1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform healthFill = healthFillObject.GetComponent<RectTransform>();
            GameObject shieldTrack = CreatePanel(hud.transform, "Shield Track", new Color(0.05f,0.08f,0.11f,1f), new Vector2(0.055f,0.570f), new Vector2(0.945f,0.598f), Vector2.zero, Vector2.zero);
            GameObject shieldFillObject = CreatePanel(shieldTrack.transform, "Shield Fill", new Color(0.20f,0.62f,1f,1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform shieldFill = shieldFillObject.GetComponent<RectTransform>();

            Text enemies = CreateText(hud.transform, "Enemies", "HOSTILES", 16, TextAnchor.MiddleLeft, new Vector2(0.055f,0.485f), new Vector2(0.945f,0.555f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            enemies.color = new Color(1f, 0.43f, 0.27f, 1f);
            GameObject enemyTrack = CreatePanel(hud.transform, "Enemy Track", new Color(0.11f,0.055f,0.045f,1f), new Vector2(0.055f,0.445f), new Vector2(0.945f,0.480f), Vector2.zero, Vector2.zero);
            GameObject enemyFillObject = CreatePanel(enemyTrack.transform, "Enemy Fill", new Color(0.95f,0.24f,0.14f,1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform enemyFill = enemyFillObject.GetComponent<RectTransform>();

            Text speed = CreateText(hud.transform, "Speed", "SPEED", 16, TextAnchor.UpperLeft, new Vector2(0.055f,0.345f), new Vector2(0.47f,0.425f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text cargo = CreateText(hud.transform, "Cargo", "CARGO", 16, TextAnchor.UpperLeft, new Vector2(0.50f,0.345f), new Vector2(0.945f,0.425f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text deliveries = CreateText(hud.transform, "Deliveries", "DELIVERIES", 16, TextAnchor.UpperLeft, new Vector2(0.055f,0.265f), new Vector2(0.47f,0.340f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text status = CreateText(hud.transform, "Status", "DRIVE NORMAL", 14, TextAnchor.UpperLeft, new Vector2(0.50f,0.265f), new Vector2(0.945f,0.340f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            GameObject attackStrip = CreatePanel(hud.transform, "Attack Strip", new Color(0.035f,0.055f,0.060f,0.92f), new Vector2(0.045f,0.075f), new Vector2(0.955f,0.245f), Vector2.zero, Vector2.zero);
            Text pulse = CreateText(attackStrip.transform, "Pulse", "SPACE ATTACK", 14, TextAnchor.MiddleLeft, new Vector2(0.03f,0.38f), new Vector2(0.97f,0.92f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            pulse.color = new Color(0.42f, 0.86f, 1f, 1f);
            string unlockText = levelIndex < 2 ? "LOCKED  Q CLASS SPECIAL @ LEVEL 3" : levelIndex < 4 ? "Q CLASS SPECIAL   |   E CLASS FINISHER @ LEVEL 5" : "Q CLASS SPECIAL   |   E CLASS FINISHER";
            Text unlock = CreateText(attackStrip.transform, "Attack Unlocks", unlockText, 11, TextAnchor.MiddleLeft, new Vector2(0.03f,0.05f), new Vector2(0.97f,0.38f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            unlock.color = new Color(0.82f, 0.78f, 0.58f, 1f);
            HUDController hudController = hud.AddComponent<HUDController>();
            hudController.Configure(city, speed, hp, enemies, cargo, deliveries, status, pulse, objective, healthFill, shieldFill, enemyFill);

            GameObject drivePanel = CreatePanel(canvas.transform, "Driving Strip", new Color(0.02f, 0.035f, 0.06f, 0.93f), new Vector2(0.33f,0.018f), new Vector2(0.78f,0.145f), Vector2.zero, Vector2.zero);
            Text style = CreateText(drivePanel.transform, "Style", "STYLE", 19, TextAnchor.MiddleLeft, new Vector2(0.03f,0.70f), new Vector2(0.97f,0.98f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            style.color = new Color(0.28f,0.9f,1f);
            Text contract = CreateText(drivePanel.transform, "Contract", "CONTRACT", 15, TextAnchor.MiddleLeft, new Vector2(0.03f,0.43f), new Vector2(0.97f,0.70f), Vector2.zero, Vector2.zero);
            Text nitro = CreateText(drivePanel.transform, "Nitro", "SHIFT NITRO / CTRL DRIFT", 14, TextAnchor.MiddleLeft, new Vector2(0.03f,0.17f), new Vector2(0.97f,0.43f), Vector2.zero, Vector2.zero);
            GameObject nitroTrack = CreatePanel(drivePanel.transform, "Nitro Track", new Color(0.1f,0.16f,0.2f), new Vector2(0.03f,0.06f), new Vector2(0.97f,0.12f), Vector2.zero, Vector2.zero);
            GameObject nitroFill = CreatePanel(nitroTrack.transform, "Nitro Fill", new Color(0.1f,0.85f,1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text toast = CreateText(canvas.transform, "Driving Feedback", "", 22, TextAnchor.MiddleCenter, new Vector2(0.31f,0.16f), new Vector2(0.94f,0.215f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            toast.color = new Color(1f,0.83f,0.35f);
            drivePanel.AddComponent<DrivingHUD>().Configure(style, contract, toast, nitro, nitroFill.GetComponent<RectTransform>());

            GameObject mapPanel = CreatePanel(canvas.transform, "Minimap Panel", new Color(0.006f, 0.011f, 0.013f, 0.98f), new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.94f), Vector2.zero, Vector2.zero);
            CreateImage(mapPanel.transform, "Map Frame Texture", SpriteAt("Assets/Art/Sprites/UI/minimap_panel_frame.png"), Color.white, Vector2.zero, Vector2.one);
            Text mapTitle = CreateText(mapPanel.transform, "Map Title", $"CITY NAV  /  {spec.CityName.ToUpperInvariant()}", 19, TextAnchor.MiddleLeft, new Vector2(0.055f,0.895f), new Vector2(0.945f,0.972f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            mapTitle.color = new Color(1f, 0.76f, 0.20f, 1f);
            Text mapHint = CreateText(mapPanel.transform, "Map Mode Hint", "[M] OPEN TACTICAL MAP", 11, TextAnchor.MiddleRight, new Vector2(0.46f,0.835f), new Vector2(0.945f,0.895f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            mapHint.color = new Color(0.45f, 0.92f, 1f, 1f);
            Text playerState = CreateText(mapPanel.transform, "Player Map State", "● PLAYER INSIDE MAP", 11, TextAnchor.MiddleLeft, new Vector2(0.055f,0.835f), new Vector2(0.46f,0.895f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            playerState.color = new Color(0.32f, 1f, 0.72f, 1f);

            GameObject mapViewport = CreatePanel(mapPanel.transform, "Map Viewport", new Color(0.010f,0.018f,0.019f,1f), new Vector2(0.050f,0.18f), new Vector2(0.950f,0.83f), Vector2.zero, Vector2.zero);
            GameObject mapAreaObject = CreatePanel(mapViewport.transform, "Map Area", new Color(0.030f,0.046f,0.047f,1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AspectRatioFitter mapAspect = mapAreaObject.AddComponent<AspectRatioFitter>();
            mapAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            mapAspect.aspectRatio = spec.WorldWidth / spec.WorldHeight;

            RectTransform mapArea = mapAreaObject.GetComponent<RectTransform>();
            Text legend = CreateText(mapPanel.transform, "Map Legend", "GOLD/WHITE ▲ YOU    ◆ ARTIFACT    ■ RECIPIENT   |   PRESS M TO TOGGLE", 12, TextAnchor.MiddleCenter, new Vector2(0.04f,0.07f), new Vector2(0.96f,0.16f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            legend.color = new Color(0.84f, 0.90f, 0.88f, 1f);
            MinimapController minimap = mapPanel.AddComponent<MinimapController>();
            minimap.Configure(mapPanel.GetComponent<RectTransform>(), mapArea, mapHint, playerState, spec.WorldWidth, spec.WorldHeight, spec.VerticalRoads, spec.HorizontalRoads,
                SpriteAt("Assets/Art/Sprites/UI/map_player.png"), SpriteAt("Assets/Art/Sprites/UI/map_artifact.png"), SpriteAt("Assets/Art/Sprites/UI/map_receiver.png"), SpriteAt("Assets/Art/Sprites/UI/map_player_halo.png"));

            GameObject pausePanel = CreatePanel(canvas.transform, "Pause Panel", new Color(0f,0f,0f,0.84f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateText(pausePanel.transform, "Pause Title", "PAUSED", 52, TextAnchor.MiddleCenter, new Vector2(.25f,.66f),new Vector2(.75f,.80f),Vector2.zero,Vector2.zero,FontStyle.Bold);
            Button resume = CreateButton(pausePanel.transform, "Resume Button", "RESUME", new Vector2(.38f,.49f),new Vector2(.62f,.57f));
            Button menu = CreateButton(pausePanel.transform, "Main Menu Button", "MAIN MENU", new Vector2(.38f,.38f),new Vector2(.62f,.46f));
            PauseMenuController pauseController = canvas.gameObject.AddComponent<PauseMenuController>();
            pauseController.Configure(pausePanel, resume, menu);

            GameObject completePanel = CreatePanel(canvas.transform, "Complete Panel", new Color(0.018f,0.035f,0.04f,0.96f), new Vector2(.22f,.24f),new Vector2(.78f,.76f),Vector2.zero,Vector2.zero);
            Text completeTitle = CreateText(completePanel.transform,"Complete Title","LEVEL COMPLETE",46,TextAnchor.MiddleCenter,new Vector2(.08f,.72f),new Vector2(.92f,.92f),Vector2.zero,Vector2.zero,FontStyle.Bold);
            Text summary = CreateText(completePanel.transform,"Summary","Summary",24,TextAnchor.MiddleCenter,new Vector2(.10f,.30f),new Vector2(.90f,.70f),Vector2.zero,Vector2.zero);
            Button next = CreateButton(completePanel.transform,"Continue Button","NEXT CITY",new Vector2(.31f,.10f),new Vector2(.69f,.24f));
            LevelCompletePanel completeController = canvas.gameObject.AddComponent<LevelCompletePanel>();
            completeController.Configure(completePanel, completeTitle, summary, next, ClipAt("Assets/Audio/SFX/level_complete.wav"));

            if (levelIndex == 0)
            {
                GameObject tutorial = CreatePanel(canvas.transform, "Tutorial Panel", new Color(0.018f,0.03f,0.035f,0.98f), new Vector2(.15f,.10f),new Vector2(.85f,.90f),Vector2.zero,Vector2.zero);
                CreateText(tutorial.transform,"Tutorial Title","DRIVER BRIEFING",46,TextAnchor.MiddleCenter,new Vector2(.08f,.79f),new Vector2(.92f,.94f),Vector2.zero,Vector2.zero,FontStyle.Bold);
                string body = "WASD / ARROWS drive     SHIFT nitro     CTRL drift\nSPACE class attack     C station charge     M map     G cosmetic shop     ESC pause\n\nChoose Nightjar (fast slash), Bison (heavy slam), or Kestrel (balanced pulse).\nLevel 3: Q class special. Level 5: E class finisher.\nClear a new city to earn a garage point: unlock R dash, combat skills, or cargo attraction.\n\nFollow radio beacons in order within 45 seconds to charge CITY RESONANCE.\nF releases it: Nightjar escapes, Bison gains armor, Kestrel commands a green wave.\nRed danger rings warn you before enemy attacks. Leave the circle to dodge.\nTraffic signals, turn indicators, curved avenues and roundabouts shape each route.";
                CreateText(tutorial.transform,"Tutorial Body",body,20,TextAnchor.MiddleCenter,new Vector2(.08f,.24f),new Vector2(.92f,.76f),Vector2.zero,Vector2.zero);
                Button start = CreateButton(tutorial.transform,"Start Driving Button","START DRIVING",new Vector2(.34f,.07f),new Vector2(.66f,.18f));
                canvas.gameObject.AddComponent<TutorialOverlay>().Configure(tutorial,start);
            }
        }

        private static LevelSpec[] CreateLevelSpecs()
        {
            return new[]
            {
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[0], CityName = "New York", CityKey = "new_york", Deliveries = 4, Boss = false,
                    WorldWidth = 74f, WorldHeight = 46f, CameraSize = 8.4f, PlayerStart = new Vector2(-30f,-17f),
                    VerticalRoads = new[] { -24f, -8f, 10f, 28f }, HorizontalRoads = new[] { -14f, 0f, 16f },
                    PickupPositions = new[] { new Vector2(-31f,-6f), new Vector2(-16f,10f), new Vector2(5f,-3f), new Vector2(19f,9f) },
                    RecipientPositions = new[] { new Vector2(30f,-8f), new Vector2(18f,-18f), new Vector2(-10f,-18f), new Vector2(7f,13f) },
                    RecipientNames = new[] { "Maya", "Theo", "Ivy", "Jonah" },
                    DeliveryColors = DeliveryPalette(4),
                    BoostPositions = new[] { new Vector2(-24f,12f), new Vector2(10f,-5f), new Vector2(28f,1f) },
                    GasStationPositions = new[] { new Vector2(-32f,19f), new Vector2(32f,-19f) },
                    PropPositions = new[] { new Vector2(-33f,-20f), new Vector2(-33f,20f), new Vector2(33f,-20f), new Vector2(33f,20f) },
                    EnemySpawnPositions = new[] { new Vector2(-24f,14f), new Vector2(-8f,-14f), new Vector2(10f,16f), new Vector2(28f,2f), new Vector2(28f,-14f), new Vector2(10f,0f) },
                    Enemies = new[] { EnemyKind.Police, EnemyKind.Racer, EnemyKind.Robber, EnemyKind.Jammer, EnemyKind.Racer, EnemyKind.Police },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-24f,-14f), new Vector2(-24f,16f), new Vector2(28f,16f), new Vector2(28f,-14f) }, 4),
                        new TrafficRouteSpec(new[] { new Vector2(-8f,-14f), new Vector2(-8f,16f), new Vector2(10f,16f), new Vector2(10f,-14f) }, 3),
                    }
                },
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[1], CityName = "Tokyo", CityKey = "tokyo", Deliveries = 5, Boss = false,
                    WorldWidth = 84f, WorldHeight = 50f, CameraSize = 8.8f, PlayerStart = new Vector2(-36f,-20f),
                    VerticalRoads = new[] { -26f, -8f, 10f, 28f }, HorizontalRoads = new[] { -16f, -1f, 14f },
                    PickupPositions = new[] { new Vector2(-32f,-7f), new Vector2(-17f,8f), new Vector2(2f,-18f), new Vector2(18f,18f), new Vector2(32f,8f) },
                    RecipientPositions = new[] { new Vector2(32f,-18f), new Vector2(18f,-6f), new Vector2(2f,18f), new Vector2(-32f,18f), new Vector2(-14f,-19f) },
                    RecipientNames = new[] { "Hana", "Ken", "Aiko", "Ren", "Sora" },
                    DeliveryColors = DeliveryPalette(5),
                    BoostPositions = new[] { new Vector2(-26f,-7f), new Vector2(-8f,7f), new Vector2(28f,7f), new Vector2(10f,-7f) },
                    GasStationPositions = new[] { new Vector2(-38f,22f), new Vector2(38f,22f) },
                    PropPositions = new[] { new Vector2(-38f,-20f), new Vector2(-38f,20f), new Vector2(38f,-20f), new Vector2(38f,20f), new Vector2(0f,22f) },
                    EnemySpawnPositions = new[] { new Vector2(-26f,-16f), new Vector2(-8f,0f), new Vector2(10f,14f), new Vector2(28f,-16f), new Vector2(28f,14f), new Vector2(-8f,14f), new Vector2(10f,-2f) },
                    Enemies = new[] { EnemyKind.Police, EnemyKind.Jammer, EnemyKind.Racer, EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Racer, EnemyKind.Police },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-26f,-16f), new Vector2(-26f,14f), new Vector2(28f,14f), new Vector2(28f,-16f) }, 5),
                        new TrafficRouteSpec(new[] { new Vector2(-8f,-16f), new Vector2(-8f,14f), new Vector2(10f,14f), new Vector2(10f,-16f) }, 4),
                        new TrafficRouteSpec(new[] { new Vector2(-26f,-1f), new Vector2(28f,-1f), new Vector2(28f,14f), new Vector2(-26f,14f) }, 3),
                    }
                },
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[2], CityName = "Beijing", CityKey = "beijing", Deliveries = 6, Boss = false,
                    WorldWidth = 92f, WorldHeight = 54f, CameraSize = 9.2f, PlayerStart = new Vector2(-40f,-22f),
                    VerticalRoads = new[] { -30f, -10f, 10f, 30f }, HorizontalRoads = new[] { -18f, -2f, 14f },
                    PickupPositions = new[] { new Vector2(-38f,-10f), new Vector2(-19f,18f), new Vector2(0f,-10f), new Vector2(20f,6f), new Vector2(38f,-20f), new Vector2(36f,18f) },
                    RecipientPositions = new[] { new Vector2(-38f,18f), new Vector2(-20f,-21f), new Vector2(0f,18f), new Vector2(20f,-20f), new Vector2(38f,6f), new Vector2(-18f,5f) },
                    RecipientNames = new[] { "Lin", "Bo", "Mei", "Jun", "Yue", "Tao" },
                    DeliveryColors = DeliveryPalette(6),
                    BoostPositions = new[] { new Vector2(-30f,6f), new Vector2(-10f,-10f), new Vector2(10f,6f), new Vector2(30f,-10f) },
                    GasStationPositions = new[] { new Vector2(-42f,24f), new Vector2(42f,-24f) },
                    PropPositions = new[] { new Vector2(-42f,-22f), new Vector2(-42f,22f), new Vector2(42f,-22f), new Vector2(42f,22f), new Vector2(0f,24f) },
                    EnemySpawnPositions = new[] { new Vector2(-30f,-18f), new Vector2(-10f,-2f), new Vector2(10f,14f), new Vector2(30f,-2f), new Vector2(-30f,14f), new Vector2(30f,14f), new Vector2(10f,-18f), new Vector2(0f,0f) },
                    Enemies = new[] { EnemyKind.Robber, EnemyKind.Jammer, EnemyKind.Police, EnemyKind.Racer, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Racer, EnemyKind.Police },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-30f,-18f), new Vector2(-30f,14f), new Vector2(30f,14f), new Vector2(30f,-18f) }, 5),
                        new TrafficRouteSpec(new[] { new Vector2(-10f,-18f), new Vector2(-10f,14f), new Vector2(10f,14f), new Vector2(10f,-18f) }, 4),
                        new TrafficRouteSpec(new[] { new Vector2(-30f,-2f), new Vector2(30f,-2f), new Vector2(30f,14f), new Vector2(-30f,14f) }, 4),
                    }
                },
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[3], CityName = "Paris, France", CityKey = "paris_france", Deliveries = 7, Boss = false,
                    WorldWidth = 102f, WorldHeight = 56f, CameraSize = 9.6f, PlayerStart = new Vector2(-44f,-23f),
                    VerticalRoads = new[] { -32f, -12f, 8f, 28f, 40f }, HorizontalRoads = new[] { -18f, -3f, 12f, 24f },
                    PickupPositions = new[] { new Vector2(-42f,-10f), new Vector2(-21f,16f), new Vector2(-2f,-22f), new Vector2(16f,4f), new Vector2(30f,-10f), new Vector2(44f,16f), new Vector2(-18f,3f) },
                    RecipientPositions = new[] { new Vector2(-42f,24f), new Vector2(-20f,-22f), new Vector2(-2f,16f), new Vector2(16f,-10f), new Vector2(30f,16f), new Vector2(44f,-22f), new Vector2(0f,3f) },
                    RecipientNames = new[] { "Claire", "Louis", "Amelie", "Luc", "Noe", "Elise", "Marc" },
                    DeliveryColors = DeliveryPalette(7),
                    BoostPositions = new[] { new Vector2(-32f,12f), new Vector2(-12f,-10f), new Vector2(8f,4f), new Vector2(28f,-10f), new Vector2(40f,4f) },
                    GasStationPositions = new[] { new Vector2(-46f,24f), new Vector2(46f,24f), new Vector2(46f,-24f) },
                    PropPositions = new[] { new Vector2(-46f,-22f), new Vector2(-46f,22f), new Vector2(46f,-22f), new Vector2(46f,22f), new Vector2(0f,24f) },
                    EnemySpawnPositions = new[] { new Vector2(-32f,-18f), new Vector2(-12f,-3f), new Vector2(8f,12f), new Vector2(28f,-3f), new Vector2(40f,12f), new Vector2(-32f,12f), new Vector2(28f,24f), new Vector2(8f,-18f), new Vector2(0f,12f) },
                    Enemies = new[] { EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Police, EnemyKind.Jammer, EnemyKind.Racer, EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Police },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-32f,-18f), new Vector2(-32f,24f), new Vector2(40f,24f), new Vector2(40f,-18f) }, 6),
                        new TrafficRouteSpec(new[] { new Vector2(-12f,-18f), new Vector2(-12f,24f), new Vector2(28f,24f), new Vector2(28f,-18f) }, 5),
                        new TrafficRouteSpec(new[] { new Vector2(8f,-18f), new Vector2(8f,24f), new Vector2(40f,12f), new Vector2(-32f,12f) }, 4),
                    }
                },
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[4], CityName = "Buenos Aires", CityKey = "buenos_aires", Deliveries = 8, Boss = true,
                    WorldWidth = 116f, WorldHeight = 62f, CameraSize = 10.2f, PlayerStart = new Vector2(-48f,-25f),
                    VerticalRoads = new[] { -36f, -16f, 4f, 24f, 42f }, HorizontalRoads = new[] { -20f, -5f, 10f, 24f },
                    PickupPositions = new[] { new Vector2(-46f,-14f), new Vector2(-25f,16f), new Vector2(-6f,-25f), new Vector2(12f,2f), new Vector2(30f,-14f), new Vector2(48f,16f), new Vector2(-25f,27f), new Vector2(30f,27f) },
                    RecipientPositions = new[] { new Vector2(48f,-25f), new Vector2(-46f,27f), new Vector2(-25f,-25f), new Vector2(-6f,16f), new Vector2(12f,-14f), new Vector2(30f,16f), new Vector2(-46f,-25f), new Vector2(48f,2f) },
                    RecipientNames = new[] { "Sofia", "Mateo", "Luna", "Bruno", "Alma", "Nico", "Emma", "Tomas" },
                    DeliveryColors = DeliveryPalette(8),
                    BoostPositions = new[] { new Vector2(-36f,10f), new Vector2(-16f,-14f), new Vector2(4f,2f), new Vector2(24f,-14f), new Vector2(42f,2f) },
                    GasStationPositions = new[] { new Vector2(-52f,27f), new Vector2(52f,27f), new Vector2(52f,-27f) },
                    PropPositions = new[] { new Vector2(-52f,-25f), new Vector2(-52f,25f), new Vector2(52f,-25f), new Vector2(52f,25f), new Vector2(0f,27f), new Vector2(0f,-27f) },
                    EnemySpawnPositions = new[] { new Vector2(-36f,-20f), new Vector2(-16f,-5f), new Vector2(4f,10f), new Vector2(24f,-5f), new Vector2(42f,10f), new Vector2(-36f,10f), new Vector2(24f,24f), new Vector2(4f,-20f), new Vector2(42f,-20f), new Vector2(4f,24f), new Vector2(24f,10f) },
                    Enemies = new[] { EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Police, EnemyKind.Jammer, EnemyKind.Racer, EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Boss, EnemyKind.Police },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-36f,-20f), new Vector2(-36f,24f), new Vector2(42f,24f), new Vector2(42f,-20f) }, 7),
                        new TrafficRouteSpec(new[] { new Vector2(-16f,-20f), new Vector2(-16f,24f), new Vector2(24f,24f), new Vector2(24f,-20f) }, 6),
                        new TrafficRouteSpec(new[] { new Vector2(4f,-20f), new Vector2(4f,24f), new Vector2(42f,10f), new Vector2(-36f,10f) }, 5),
                    }
                },
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[5], CityName = "Moscow", CityKey = "moscow", Deliveries = 9, Boss = false,
                    WorldWidth = 126f, WorldHeight = 68f, CameraSize = 10.8f, PlayerStart = new Vector2(-54f,-28f),
                    VerticalRoads = new[] { -40f, -20f, 0f, 20f, 40f }, HorizontalRoads = new[] { -24f, -8f, 8f, 24f },
                    PickupPositions = new[] { new Vector2(-50f,-16f), new Vector2(-30f,18f), new Vector2(-10f,-28f), new Vector2(10f,4f), new Vector2(30f,-16f), new Vector2(50f,18f), new Vector2(-30f,28f), new Vector2(30f,28f), new Vector2(0f,18f) },
                    RecipientPositions = new[] { new Vector2(50f,-28f), new Vector2(-50f,28f), new Vector2(-30f,-28f), new Vector2(-10f,18f), new Vector2(10f,-16f), new Vector2(30f,18f), new Vector2(-50f,-28f), new Vector2(50f,4f), new Vector2(0f,-8f) },
                    RecipientNames = new[] { "Nadia", "Mikhail", "Irina", "Oleg", "Yelena", "Dmitri", "Pavel", "Anya", "Viktor" },
                    DeliveryColors = DeliveryPalette(9),
                    BoostPositions = new[] { new Vector2(-40f,8f), new Vector2(-20f,-16f), new Vector2(0f,0f), new Vector2(20f,-16f), new Vector2(40f,0f), new Vector2(0f,24f) },
                    GasStationPositions = new[] { new Vector2(-56f,30f), new Vector2(56f,30f), new Vector2(56f,-30f) },
                    PropPositions = new[] { new Vector2(-56f,-28f), new Vector2(-56f,28f), new Vector2(56f,-28f), new Vector2(56f,28f), new Vector2(0f,30f), new Vector2(0f,-30f) },
                    EnemySpawnPositions = new[] { new Vector2(-40f,-24f), new Vector2(-20f,-8f), new Vector2(0f,8f), new Vector2(20f,-8f), new Vector2(40f,8f), new Vector2(-40f,8f), new Vector2(20f,24f), new Vector2(0f,-24f), new Vector2(40f,-24f), new Vector2(0f,24f), new Vector2(-20f,24f), new Vector2(20f,8f) },
                    Enemies = new[] { EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Police, EnemyKind.Jammer, EnemyKind.Racer, EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Police, EnemyKind.Racer, EnemyKind.Jammer },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-40f,-24f), new Vector2(-40f,24f), new Vector2(40f,24f), new Vector2(40f,-24f) }, 7),
                        new TrafficRouteSpec(new[] { new Vector2(-20f,-24f), new Vector2(-20f,24f), new Vector2(20f,24f), new Vector2(20f,-24f) }, 6),
                        new TrafficRouteSpec(new[] { new Vector2(0f,-24f), new Vector2(0f,24f), new Vector2(40f,8f), new Vector2(-40f,8f) }, 6),
                    }
                },
                new LevelSpec
                {
                    SceneName = GameScenes.Levels[6], CityName = "Cairo", CityKey = "cairo", Deliveries = 10, Boss = true,
                    WorldWidth = 136f, WorldHeight = 74f, CameraSize = 11.4f, PlayerStart = new Vector2(-58f,-31f),
                    VerticalRoads = new[] { -44f, -22f, 0f, 22f, 44f }, HorizontalRoads = new[] { -26f, -9f, 8f, 26f },
                    PickupPositions = new[] { new Vector2(-54f,-18f), new Vector2(-32f,19f), new Vector2(-12f,-30f), new Vector2(12f,4f), new Vector2(32f,-18f), new Vector2(54f,19f), new Vector2(-32f,30f), new Vector2(32f,30f), new Vector2(0f,20f), new Vector2(52f,-2f) },
                    RecipientPositions = new[] { new Vector2(54f,-30f), new Vector2(-54f,30f), new Vector2(-32f,-30f), new Vector2(-12f,20f), new Vector2(12f,-18f), new Vector2(32f,20f), new Vector2(-54f,-30f), new Vector2(54f,4f), new Vector2(0f,-10f), new Vector2(20f,30f) },
                    RecipientNames = new[] { "Amina", "Youssef", "Karim", "Nour", "Layla", "Omar", "Samir", "Fatima", "Rania", "Ziad" },
                    DeliveryColors = DeliveryPalette(10),
                    BoostPositions = new[] { new Vector2(-44f,8f), new Vector2(-22f,-18f), new Vector2(0f,0f), new Vector2(22f,-18f), new Vector2(44f,0f), new Vector2(0f,26f), new Vector2(44f,18f) },
                    GasStationPositions = new[] { new Vector2(-60f,32f), new Vector2(60f,32f), new Vector2(60f,-32f), new Vector2(-60f,-32f) },
                    PropPositions = new[] { new Vector2(-60f,-30f), new Vector2(-60f,30f), new Vector2(60f,-30f), new Vector2(60f,30f), new Vector2(0f,32f), new Vector2(0f,-32f) },
                    EnemySpawnPositions = new[] { new Vector2(-44f,-26f), new Vector2(-22f,-9f), new Vector2(0f,8f), new Vector2(22f,-9f), new Vector2(44f,8f), new Vector2(-44f,8f), new Vector2(22f,26f), new Vector2(0f,-26f), new Vector2(44f,-26f), new Vector2(0f,26f), new Vector2(-22f,26f), new Vector2(22f,8f), new Vector2(44f,26f) },
                    Enemies = new[] { EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Police, EnemyKind.Jammer, EnemyKind.Racer, EnemyKind.Robber, EnemyKind.Tow, EnemyKind.Oil, EnemyKind.Police, EnemyKind.Racer, EnemyKind.Jammer, EnemyKind.Boss },
                    TrafficRoutes = new[] {
                        new TrafficRouteSpec(new[] { new Vector2(-44f,-26f), new Vector2(-44f,26f), new Vector2(44f,26f), new Vector2(44f,-26f) }, 8),
                        new TrafficRouteSpec(new[] { new Vector2(-22f,-26f), new Vector2(-22f,26f), new Vector2(22f,26f), new Vector2(22f,-26f) }, 7),
                        new TrafficRouteSpec(new[] { new Vector2(0f,-26f), new Vector2(0f,26f), new Vector2(44f,8f), new Vector2(-44f,8f) }, 6),
                    }
                }
            };
        }

        private static Color[] DeliveryPalette(int count)
        {
            Color[] palette = new[]
            {
                new Color(0.95f, 0.35f, 0.35f),
                new Color(0.35f, 0.7f, 1f),
                new Color(0.45f, 0.9f, 0.45f),
                new Color(0.98f, 0.9f, 0.35f),
                new Color(0.85f, 0.45f, 1f),
                new Color(1f, 0.6f, 0.25f),
                new Color(0.3f, 0.95f, 0.85f),
                new Color(1f, 0.55f, 0.75f),
            };
            Color[] result = new Color[count];
            for (int i = 0; i < count; i++) result[i] = palette[i % palette.Length];
            return result;
        }

        private static void ConfigureBuildSettings(LevelSpec[] specs)
        {
            List<EditorBuildSettingsScene> scenes = new()
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/{GameScenes.MainMenu}.unity", true)
            };
            foreach (LevelSpec spec in specs)
            {
                scenes.Add(new EditorBuildSettingsScene($"{ScenesFolder}/{spec.SceneName}.unity", true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{sceneName}.unity");
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector2 position, Transform parent, int sortingOrder, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private static void CreateBoundary(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static Vector2 GetSpriteColliderSize(SpriteRenderer renderer, float scaleFactor)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return Vector2.one;
            }

            Vector2 size = renderer.sprite.bounds.size;
            return new Vector2(size.x * scaleFactor, size.y * scaleFactor);
        }

        private static Camera CreateCamera(Vector3 position, float orthographicSize, Color background, Transform follow = null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = position;
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.backgroundColor = background;
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (follow != null) go.AddComponent<CameraFollow2D>().Configure(follow);
            return cam;
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateIcon(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max)
        { CreateImage(parent,name,sprite,Color.white,min,max).GetComponent<Image>().preserveAspect=true; }

        private static void AddDeliveryBeacon(GameObject target,Color color,string number)
        {
            GameObject ring=new GameObject("Mission Color Ring"); ring.transform.SetParent(target.transform,false);
            SpriteRenderer renderer=ring.AddComponent<SpriteRenderer>(); renderer.sprite=ModernCourierArt.Get("map_player_halo");
            renderer.color=color; renderer.sortingOrder=6;
            float scale=2.9f/(renderer.sprite.bounds.size.x*target.transform.localScale.x);
            ring.transform.localScale=Vector3.one*scale;
            GameObject label=new GameObject("Mission Number"); label.transform.SetParent(target.transform,false);
            label.transform.localPosition=new Vector3(-1.05f,-1.2f,0f)/target.transform.localScale.x;
            label.transform.localScale=Vector3.one/target.transform.localScale.x;
            TextMesh text=label.AddComponent<TextMesh>(); text.text=number; text.fontSize=48; text.characterSize=0.08f;
            text.anchor=TextAnchor.MiddleCenter; text.color=color; label.GetComponent<MeshRenderer>().sortingOrder=20;
        }

        private static GameObject CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = false;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image panelImage = go.GetComponent<Image>();
            panelImage.color = color;
            if(name=="HUD Panel" || name=="Driving Strip" || name=="Complete Panel" || name=="Tutorial Panel")
            {
                panelImage.sprite=ModernCourierArt.Get("ui_panel"); panelImage.type=Image.Type.Sliced;
                panelImage.color=new Color(1f,1f,1f,color.a);
            }
            return go;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, FontStyle style = FontStyle.Normal)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.fontStyle = style;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.sprite = ModernCourierArt.Get("ui_button");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.65f, 0.94f, 1f);
            colors.pressedColor = new Color(0.65f, 0.72f, 0.83f);
            colors.selectedColor = new Color(1f, 0.90f, 0.62f);
            button.colors = colors;
            CreateText(go.transform, "Label", label, 23, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            return button;
        }

        private static Sprite[] LoadSpritesFromFolder(string folderPath) => ModernCourierArt.Frames(folderPath);

        private static Sprite SpriteAt(string path) => ModernCourierArt.Find(path) ?? throw new System.InvalidOperationException("Unmapped artwork: " + path);
        private static AudioClip ClipAt(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
#endif

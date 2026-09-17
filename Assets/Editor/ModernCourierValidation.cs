#if UNITY_EDITOR
using System;
using ArtifactCourier.Core;
using ArtifactCourier.Driving;
using ArtifactCourier.Player;
using ArtifactCourier.Traffic;
using ArtifactCourier.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtifactCourier.Editor
{
    internal static class ModernCourierValidation
    {
        [MenuItem("Tools/Artifact Courier/Validate Modern Game")]
        public static void Validate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Require(EditorBuildSettings.scenes.Length == 8, "Build Complete Game first: expected menu and seven cities.");
                foreach (var entry in EditorBuildSettings.scenes)
                {
                    var scene = EditorSceneManager.OpenScene(entry.path);
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + child.name);
                    if (UnityEngine.Object.FindFirstObjectByType<LevelController>() == null) continue;
                    var car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                    Require(car != null, "Missing player in " + entry.path);
                    Require(car.GetComponent<VehicleLoadout>() != null, "Missing vehicle class selection.");
                    var roads=UnityEngine.Object.FindFirstObjectByType<CityRoadNetwork>();
                    Require(roads!=null && roads.paths!=null && roads.paths.Length>=7,"Missing curved road network.");
                    foreach(var path in roads.paths)
                    {
                        Require(path.points!=null && path.points.Length>=2,"Empty road path.");
                        foreach(var point in path.points)
                            Require(!float.IsNaN(point.x) && !float.IsNaN(point.y),"Invalid road coordinate.");
                    }
                    Require(UnityEngine.Object.FindObjectsByType<TrafficSignal>(FindObjectsSortMode.None).Length>0,"Missing traffic signals.");
                    foreach(var traffic in UnityEngine.Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None))
                    {
                        var serialized=new SerializedObject(traffic);
                        var points=serialized.FindProperty("waypoints");
                        Require(points.arraySize>=4,"Missing traffic path.");
                        for(int i=0;i<points.arraySize;i++)Require(points.GetArrayElementAtIndex(i).objectReferenceValue!=null,"Broken traffic waypoint reference.");
                    }
                    int districtCount=0;
                    foreach(var renderer in roads.transform.parent.Find("World").GetComponentsInChildren<SpriteRenderer>())if(renderer.name.StartsWith("District "))districtCount++;
                    Require(districtCount>=12,"Too few interior district buildings: "+districtCount);
                    foreach(var bonus in UnityEngine.Object.FindObjectsByType<ArtifactCourier.Delivery.BonusPickup>(FindObjectsSortMode.None))
                    {
                        if(bonus.Kind!=ArtifactCourier.Delivery.BonusKind.StyleCoin)continue;
                        float margin=float.NegativeInfinity;Vector2 point=bonus.transform.position;
                        foreach(var path in roads.paths)for(int i=0;i<path.points.Length-1;i++)
                        {
                            Vector2 a=path.points[i],ab=path.points[i+1]-a;
                            float distance=Vector2.Distance(point,a+ab*Mathf.Clamp01(Vector2.Dot(point-a,ab)/Mathf.Max(.0001f,ab.sqrMagnitude)));
                            margin=Mathf.Max(margin,path.width*.5f-distance);
                        }
                        Require(margin>=.5f,"Coin outside road: "+bonus.name);
                    }
                    var staticOrders=new System.Collections.Generic.HashSet<int>();
                    foreach(var renderer in roads.transform.parent.Find("World").GetComponentsInChildren<SpriteRenderer>())
                        Require(staticOrders.Add(renderer.sortingOrder),"Static sprite sorting tie: "+renderer.name);
                    Require(car.GetComponent<DrivingStyle>() != null, "Missing driving rewards.");
                    Require(car.GetComponent<DriftTrails>() != null, "Missing tyre trails.");
                    Require(UnityEngine.Object.FindFirstObjectByType<DrivingHUD>() != null, "Missing driving HUD.");
                    var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow2D>();
                    Require(follow != null && follow.GetComponent<Camera>().orthographicSize >= 10.9f, "Camera is not zoomed out.");
                    string asset = AssetDatabase.GetAssetPath(car.GetComponent<SpriteRenderer>().sprite);
                    Require(asset.StartsWith("Assets/Art/Modern/"), "Player still uses old artwork.");
                    foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                        if (renderer.GetComponent<CarController>() != null || renderer.GetComponent<TrafficVehicle>() != null || renderer.GetComponent<ArtifactCourier.Enemies.EnemyVehicle>() != null)
                            Require(renderer.sprite != null && AssetDatabase.GetAssetPath(renderer.sprite).StartsWith("Assets/Art/Modern/"), "Old/missing vehicle art: " + renderer.name);
                }
                Debug.Log("Modern Courier validation passed: eight scenes, class loadouts, curved roads, signals, traffic routes, unique static draw orders, camera, HUD, persistent art and no missing scripts.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif

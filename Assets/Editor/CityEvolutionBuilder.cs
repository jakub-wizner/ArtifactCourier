#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ArtifactCourier.Traffic;
using ArtifactCourier.Player;
using ArtifactCourier.UI;
using UnityEditor;
using UnityEngine;
namespace ArtifactCourier.Editor
{
    public static partial class ArtifactCourierProjectBuilder
    {
        static Material roadMaterial;
        [Serializable] sealed class LayoutBook { public Layout[] cities; }
        [Serializable] sealed class Layout { public string name; public Vector2[] nodes; public Edge[] edges; public Loop[] loops; }
        [Serializable] sealed class Edge { public int a,b; public float bend,width; }
        [Serializable] sealed class Loop { public int[] nodes; }
        static void EvolveCity(int index,LevelSpec spec,Transform root)
        {
            var source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Modern/city_layouts.json");
            var layout=JsonUtility.FromJson<LayoutBook>(source.text).cities[index];
            var world=root.Find("World");
            foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>())
                if(sr.name=="Asphalt Lane" || sr.name=="Road Junction" || sr.name.StartsWith("District ") || sr.name.StartsWith("Mission ") || sr.name.StartsWith("Road End Barrier") || sr.name.StartsWith("Avenue"))UnityEngine.Object.DestroyImmediate(sr.gameObject);
            var network=new GameObject("Authored City Roads").AddComponent<CityRoadNetwork>();network.transform.SetParent(root,false);network.identity=layout.name;
            Vector2[] nodes=layout.nodes.Select(p=>new Vector2(p.x*spec.WorldWidth*.5f,p.y*spec.WorldHeight*.5f)).ToArray();
            var paths=new List<CityRoadNetwork.Path>();var sampled=new Dictionary<string,Vector2[]>();int[] degrees=new int[nodes.Length];
            string Key(int a,int b)=>Mathf.Min(a,b)+":"+Mathf.Max(a,b);
            foreach(var edge in layout.edges)
            {
                Vector2 a=nodes[edge.a],b=nodes[edge.b],d=b-a;
                Vector2 control=(a+b)*.5f+new Vector2(-d.y,d.x).normalized*edge.bend;
                int count=Mathf.Max(8,Mathf.CeilToInt(d.magnitude/.65f));var points=new Vector2[count+1];
                for(int i=0;i<=count;i++){float t=i/(float)count;points[i]=(1-t)*(1-t)*a+2*(1-t)*t*control+t*t*b;}
                paths.Add(new CityRoadNetwork.Path{points=points,width=edge.width});sampled[Key(edge.a,edge.b)]=points;degrees[edge.a]++;degrees[edge.b]++;
            }
            if(index==3 || index==5)
            {
                var ringNodes=layout.loops[layout.loops.Length-1].nodes;Vector2 center=Vector2.zero;
                foreach(int id in ringNodes)center+=nodes[id];center/=ringNodes.Length;
                var island=CreateSizedSprite("Circular plaza garden",ModernCourierArt.Get(spec.CityKey+"_park"),center,world,-4,4.2f);
                island.AddComponent<CircleCollider2D>().radius=1.6f/island.transform.localScale.x;
            }
            network.paths=paths.ToArray();network.junctions=nodes.Where((p,i)=>degrees[i]>=3).ToArray();network.radius=0;network.roundabout=Vector2.zero;
            foreach(var path in paths)
            {
                Stroke(network.transform,path.points,path.width+.7f,new Color(.67f,.68f,.64f),-13000,false);
                Stroke(network.transform,path.points,path.width,RoadPalette(index),-12000,false);
                for(int i=3;i<path.points.Length-4;i+=6)Stroke(network.transform,new[]{path.points[i],path.points[Mathf.Min(i+2,path.points.Length-1)]},.09f,new Color(.87f,.8f,.53f),-11000,false);
            }
            foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>())
                if(sr.name.StartsWith("Street Prop") && paths.Any(path=>path.points.Any(p=>Vector2.Distance(p,sr.transform.position)<4f)))UnityEngine.Object.DestroyImmediate(sr.gameObject);
            // Signals guard actual graph junctions; driving priority is granted by junction reservation.
            for(int i=0;i<nodes.Length;i++)if(degrees[i]>=3 && i%3==0)
            {
                var signal=new GameObject("Junction signal").AddComponent<TrafficSignal>();signal.transform.SetParent(root,false);signal.transform.position=nodes[i];signal.offset=i*.9f;
                signal.northLamp=Lamp(signal.transform,new Vector2(-2.8f,-2.8f),Color.red);signal.eastLamp=Lamp(signal.transform,new Vector2(2.8f,2.8f),Color.red);
            }
            // Construct shared lane loops from the SAME sampled edges used for road rendering.
            var routes=new List<Transform[]>();
            foreach(var loop in layout.loops)
            {
                var centerline=new List<Vector2>();
                for(int i=0;i<loop.nodes.Length;i++)
                {
                    int a=loop.nodes[i],b=loop.nodes[(i+1)%loop.nodes.Length];var edge=layout.edges.First(e=>Key(e.a,e.b)==Key(a,b));
                    var segment=sampled[Key(a,b)];
                    if(edge.a!=a)segment=segment.Reverse().ToArray();
                    centerline.AddRange(segment.Take(segment.Length-1));
                }
                var smooth=SmoothCorners(centerline);var lane=new Transform[smooth.Count];
                for(int i=0;i<lane.Length;i++)
                {
                    Vector2 tangent=(smooth[(i+1)%lane.Length]-smooth[(i+lane.Length-1)%lane.Length]).normalized;
                    var t=new GameObject("Shared lane node").transform;t.SetParent(network.transform,false);
                    t.position=smooth[i]+new Vector2(tangent.y,-tangent.x)*.85f;lane[i]=t;
                }
                routes.Add(lane);
            }
            var used=new List<Vector2>();int serial=0;
            foreach(var vehicle in root.GetComponentsInChildren<TrafficVehicle>())
            {
                var route=routes[serial%routes.Count];int start=(serial*31)%route.Length;
                for(int tries=0;tries<route.Length;tries++)
                {if(used.All(p=>Vector2.Distance(p,route[start].position)>4.5f))break;start=(start+1)%route.Length;}
                if(used.Any(p=>Vector2.Distance(p,route[start].position)<=4.5f)){UnityEngine.Object.DestroyImmediate(vehicle.gameObject);continue;}
                used.Add(route[start].position);vehicle.SetRoute(route,start);vehicle.transform.position=route[start].position;vehicle.transform.up=route[(start+1)%route.Length].position-route[start].position;
                var intent=vehicle.gameObject.AddComponent<TrafficIntent>();intent.left=Lamp(vehicle.transform,new Vector2(-.42f,.4f),new Color(1,.6f,0));intent.right=Lamp(vehicle.transform,new Vector2(.42f,.4f),new Color(1,.6f,0));intent.brake=Lamp(vehicle.transform,new Vector2(0,-.65f),Color.red);serial++;
            }
            // Missions connect to the closest street through a short curbside approach.
            var markers=root.GetComponentsInChildren<ArtifactCourier.Delivery.ArtifactPickup>().Select(x=>x.transform)
                .Concat(root.GetComponentsInChildren<ArtifactCourier.Delivery.DeliveryRecipient>().Select(x=>x.transform));
            foreach(var marker in markers)
            {
                Vector2 old=marker.position;var nearest=paths.SelectMany(p=>p.points).OrderBy(p=>(p-old).sqrMagnitude).First();
                Vector2 side=(old-nearest).normalized;if(side.sqrMagnitude<.1f)side=Vector2.up;
                marker.position=nearest+side*3.5f;
                foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>())if(sr.name.StartsWith("District ") && Vector2.Distance(sr.transform.position,marker.position)<5f)UnityEngine.Object.DestroyImmediate(sr.gameObject);
            }
            PopulateDistricts(index,spec,root,world,paths);
            PlaceRoadCoins(index,root,paths);
            var player=root.GetComponentInChildren<CarController>();
            player.transform.position=routes[0][0].position;player.transform.up=routes[0][1].position-routes[0][0].position;
            foreach(var vehicle in root.GetComponentsInChildren<TrafficVehicle>())if(Vector2.Distance(vehicle.transform.position,player.transform.position)<4f)UnityEngine.Object.DestroyImmediate(vehicle.gameObject);
            int order=0;foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>().OrderBy(s=>s.sortingOrder).ThenByDescending(s=>s.transform.position.y))
                sr.sortingOrder=sr.name=="City Paving"?-16000:sr.drawMode==SpriteDrawMode.Tiled?-15000+order++:-9000+order++;
            var challenge=new GameObject("City Resonance Route").AddComponent<CityResonanceRoute>();challenge.transform.SetParent(root,false);
            challenge.Configure(new[]{nodes[1],nodes[4],nodes[6]},ModernCourierArt.Get("class_radio"),layout.name);
        }
        static Color RoadPalette(int city)
        {
            Color[] colors={new Color(.16f,.19f,.22f),new Color(.12f,.15f,.23f),new Color(.27f,.28f,.27f),new Color(.31f,.28f,.24f),new Color(.22f,.21f,.27f),new Color(.19f,.23f,.28f),new Color(.42f,.35f,.24f)};
            return colors[city];
        }
        static void PlaceRoadCoins(int city,Transform root,List<CityRoadNetwork.Path> paths)
        {
            var parent=root.Find("Level Bonuses");int serial=0;
            foreach(var path in paths)
            {
                if(path.points.Length<10)continue;
                // The complete coin sprite fits inside even the narrowest lane.
                for(int i=5;i<path.points.Length-4;i+=13)
                {
                    CreateBonusObject(parent,ArtifactCourier.Delivery.BonusKind.StyleCoin,path.points[i],50f,0f,"style_coin",ClipAt("Assets/Audio/SFX/pickup.wav"));
                    Transform coin=parent.GetChild(parent.childCount-1);coin.name="Road Coin "+(serial++).ToString("000");
                    var sprite=coin.GetComponent<SpriteRenderer>();coin.localScale=Vector3.one*(.85f/Mathf.Max(sprite.sprite.bounds.size.x,sprite.sprite.bounds.size.y));
                    coin.GetComponent<CircleCollider2D>().radius=.6f/coin.localScale.x;
                }
            }
        }
        static void PopulateDistricts(int city,LevelSpec spec,Transform root,Transform world,List<CityRoadNetwork.Path> paths)
        {
            var keepClear=new List<Vector2>();
            foreach(var marker in root.GetComponentsInChildren<ArtifactCourier.Delivery.ArtifactPickup>())keepClear.Add(marker.transform.position);
            foreach(var marker in root.GetComponentsInChildren<ArtifactCourier.Delivery.DeliveryRecipient>())keepClear.Add(marker.transform.position);
            foreach(var station in root.GetComponentsInChildren<ArtifactCourier.Delivery.GasStation>())keepClear.Add(station.transform.position);
            var occupied=new List<Vector3>();int count=0;
            foreach(var existing in world.GetComponentsInChildren<SpriteRenderer>())
                if(existing.GetComponent<Collider2D>()!=null)occupied.Add(new Vector3(existing.transform.position.x,existing.transform.position.y,Mathf.Max(existing.bounds.extents.x,existing.bounds.extents.y)+.4f));
            foreach(var bonus in root.GetComponentsInChildren<ArtifactCourier.Delivery.BonusPickup>())keepClear.Add(bonus.transform.position);
            // Dense candidate sampling with variable footprints fills interior pockets without blocking roads.
            for(float y=-spec.WorldHeight*.44f;y<spec.WorldHeight*.44f;y+=2.1f)
                for(float x=-spec.WorldWidth*.44f;x<spec.WorldWidth*.44f;x+=2.1f)
                {
                    Vector2 p=new Vector2(x+.35f*Mathf.Sin(y+city),y+.3f*Mathf.Sin(x*.9f));
                    float room=100f;
                    foreach(var path in paths)for(int i=0;i<path.points.Length-1;i++)room=Mathf.Min(room,SegmentDistance(p,path.points[i],path.points[i+1])-path.width*.5f);
                    float size=Mathf.Min(city==0?4.2f:city==6?3.1f:3.7f,(room-.35f)*1.4f);
                    if(size<1.7f)continue;
                    float radius=size*.71f;
                    if(keepClear.Any(v=>Vector2.Distance(v,p)<radius+1.5f))continue;
                    if(occupied.Any(v=>Vector2.Distance(new Vector2(v.x,v.y),p)<v.z+radius+.25f))continue;
                    string key=count%7==6?"district_garden":count%4==3?spec.CityKey+"_house":"district_"+spec.CityKey;
                    var go=CreateSizedSprite("District "+spec.CityKey+" "+count,ModernCourierArt.Get(key),p,world,-4,size);
                    if(key!="district_garden")go.AddComponent<BoxCollider2D>().size=GetSpriteColliderSize(go.GetComponent<SpriteRenderer>(),.80f);
                    occupied.Add(new Vector3(p.x,p.y,radius));count++;
                }
            Debug.Log(spec.CityName+": placed "+count+" interior district buildings and gardens.");
        }
        static List<Vector2> SmoothCorners(List<Vector2> raw)
        {
            var result=new List<Vector2>();for(int i=0;i<raw.Count;i++)result.Add((raw[(i+raw.Count-1)%raw.Count]+raw[i]*2+raw[(i+1)%raw.Count])*.25f);return result;
        }
        static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
        {var ab=b-a;return Vector2.Distance(p,a+ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.0001f,ab.sqrMagnitude)));}
        static void Stroke(Transform parent,Vector2[] points,float width,Color color,int order,bool loop)
        {
            if(roadMaterial==null)
            {
                const string path="Assets/Art/Modern/RoadInk.mat";
                roadMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(roadMaterial==null){roadMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(roadMaterial,path);}
            }
            var line=new GameObject("Road ribbon").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);
            line.sharedMaterial=roadMaterial;line.useWorldSpace=true;line.loop=loop;line.widthMultiplier=width;
            line.startColor=line.endColor=color;line.sortingOrder=order;line.numCornerVertices=4;line.numCapVertices=4;
            line.positionCount=points.Length;for(int i=0;i<points.Length;i++)line.SetPosition(i,points[i]);
        }
        static SpriteRenderer Lamp(Transform parent,Vector2 local,Color color)
        {
            var go=new GameObject("Signal lamp");go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=Vector3.one*.18f;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=ModernCourierArt.Get("style_coin");sr.color=color;sr.sortingOrder=25;return sr;
        }
        static void CreateGarageSelection(Transform canvas)
        {
            var panel=CreatePanel(canvas,"Choose Your Courier",new Color(.025f,.045f,.08f,.97f),new Vector2(.42f,.26f),new Vector2(.97f,.69f),Vector2.zero,Vector2.zero);
            CreateText(panel.transform,"Garage heading","CHOOSE YOUR COURIER",24,TextAnchor.MiddleCenter,new Vector2(0,.84f),Vector2.one,Vector2.zero,Vector2.zero,FontStyle.Bold);
            string[] titles={"NIGHTJAR","BISON","KESTREL"};string[] details={"MOTORCYCLE\nFast / light armor\nQuick short-range slash","PICKUP\nSlow / heavy armor\nPowerful 360-degree slam","STANDARD CAR\nBalanced speed / armor\nRadial pulse attack"};string[] art={"class_bike","class_pickup","class_car"};
            var selection=panel.AddComponent<GarageSelection>();var buttons=new UnityEngine.UI.Button[3];
            for(int i=0;i<3;i++)
            {
                float x=.02f+i*.325f;
                CreateIcon(panel.transform,titles[i],ModernCourierArt.Get(art[i]),new Vector2(x,.42f),new Vector2(x+.30f,.82f));
                CreateText(panel.transform,"Stats",details[i],14,TextAnchor.MiddleCenter,new Vector2(x,.16f),new Vector2(x+.30f,.43f),Vector2.zero,Vector2.zero);
                buttons[i]=CreateButton(panel.transform,titles[i],titles[i],new Vector2(x,.03f),new Vector2(x+.30f,.15f));
            }
            selection.Configure(buttons);
        }
    }
}
#endif

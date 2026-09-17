using UnityEngine;
namespace ArtifactCourier.UI
{
    public sealed class MapRoadSegment : MonoBehaviour
    {
        RectTransform map,rect;Vector2 a,b,lastSize;
        public void Configure(RectTransform parent,Vector2 start,Vector2 end){map=parent;rect=GetComponent<RectTransform>();a=start;b=end;Refresh();}
        void LateUpdate(){if(map!=null && map.rect.size!=lastSize)Refresh();}
        void Refresh()
        {
            lastSize=map.rect.size;var start=Vector2.Scale(a-Vector2.one*.5f,lastSize);var end=Vector2.Scale(b-Vector2.one*.5f,lastSize);
            rect.anchoredPosition=(start+end)*.5f;rect.sizeDelta=new Vector2(Vector2.Distance(start,end)+.5f,3f);rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg);
        }
    }
}

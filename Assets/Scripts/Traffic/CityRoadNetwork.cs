using UnityEngine;
namespace ArtifactCourier.Traffic
{
    // Serialized once in the scene; minimap and traffic consume the same geometry.
    public sealed class CityRoadNetwork : MonoBehaviour
    {
        [System.Serializable] public sealed class Path { public Vector2[] points; public bool loop; public float width=4.6f; }
        public Path[] paths;
        public Vector2[] junctions;
        public Vector2 roundabout;
        public float radius=5f;
        public string identity;
    }
}

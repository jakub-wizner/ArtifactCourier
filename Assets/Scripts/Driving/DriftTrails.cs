using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Driving
{
    [RequireComponent(typeof(CarController))]
    public sealed class DriftTrails : MonoBehaviour
    {
        private CarController car;
        private readonly TrailRenderer[] trails = new TrailRenderer[2];
        private Material material;
        private void Start()
        {
            car = GetComponent<CarController>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) { enabled = false; return; }
            material = new Material(shader);
            for (int i = 0; i < trails.Length; i++)
            {
                GameObject wheel = new GameObject("Rear Tyre Trail");
                wheel.transform.SetParent(transform, false);
                wheel.transform.localPosition = new Vector3(i == 0 ? -0.35f : 0.35f, -0.55f, 0f);
                TrailRenderer trail = wheel.AddComponent<TrailRenderer>();
                trail.sharedMaterial = material;
                trail.time = 1.7f;
                trail.minVertexDistance = 0.1f;
                trail.startWidth = 0.11f;
                trail.endWidth = 0.04f;
                trail.startColor = new Color(0.015f,0.02f,0.025f,0.65f);
                trail.endColor = new Color(0.015f,0.02f,0.025f,0f);
                trail.sortingOrder = 0;
                trail.emitting = false;
                trails[i] = trail;
            }
        }
        private void Update()
        {
            foreach (TrailRenderer trail in trails)
                if (trail != null) trail.emitting = Time.timeScale > 0f && car.IsDrifting;
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}

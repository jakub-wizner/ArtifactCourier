using UnityEngine;

namespace ArtifactCourier.Traffic
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.22f;
        [SerializeField] private float speedZoom = 2.4f;
        private Vector3 velocity;
        private Rigidbody2D targetBody;
        private Camera lens;
        private float baseSize;
        private float zoomVelocity;

        public void Configure(Transform followTarget) => target = followTarget;

        private void Start()
        {
            lens = GetComponent<Camera>();
            baseSize = Mathf.Max(11.5f, lens.orthographicSize);
            lens.orthographicSize = baseSize;
            if (target != null) targetBody = target.GetComponent<Rigidbody2D>();
            if (target != null) transform.position = target.position + Vector3.back * 10f;
        }

        private void LateUpdate()
        {
            if (target == null || Time.timeScale == 0f) return;
            Vector2 motion = targetBody == null ? Vector2.zero : targetBody.linearVelocity;
            Vector3 lead = Vector2.ClampMagnitude(motion * 0.23f, 3.2f);
            Vector3 desired = target.position + lead + Vector3.back * 10f;
            desired.z = -10f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            // Keep the same horizontal visibility on portrait displays.
            float aspectCorrection = Mathf.Max(1f, 1.25f / Mathf.Max(0.4f, lens.aspect));
            float size = (baseSize + Mathf.Clamp01(motion.magnitude / 19f) * speedZoom) * aspectCorrection;
            lens.orthographicSize = Mathf.SmoothDamp(lens.orthographicSize, size, ref zoomVelocity, 0.55f);
        }
    }
}

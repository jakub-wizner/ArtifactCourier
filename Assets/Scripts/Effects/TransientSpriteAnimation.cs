using UnityEngine;

namespace ArtifactCourier.Effects
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TransientSpriteAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameDuration = 0.05f;
        [SerializeField] private bool loop;
        [SerializeField] private float totalLifetime;

        private SpriteRenderer spriteRenderer;
        private float elapsed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            int frameIndex = Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, frameDuration));

            if (loop)
            {
                frameIndex %= frames.Length;
                spriteRenderer.sprite = frames[frameIndex];
                if (totalLifetime > 0f && elapsed >= totalLifetime)
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (frameIndex >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }

        public static void Spawn(Sprite[] animationFrames, Vector3 position, float secondsPerFrame, int sortingOrder, Vector3 scale, Color tint, float lifetime = 0f, bool looping = false, float rotationDegrees = 0f)
        {
            if (animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            var go = new GameObject("Transient Sprite Animation");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f,0f,rotationDegrees);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;
            renderer.sprite = animationFrames[0];

            var animation = go.AddComponent<TransientSpriteAnimation>();
            animation.frames = animationFrames;
            animation.frameDuration = secondsPerFrame;
            animation.loop = looping;
            animation.totalLifetime = lifetime;
        }
    }
}

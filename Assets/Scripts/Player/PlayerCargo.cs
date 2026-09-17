using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactCourier.Player
{
    public sealed class PlayerCargo : MonoBehaviour
    {
        [SerializeField] private int capacity = 4;
        private readonly List<string> artifactIds = new();

        public event Action CargoChanged;
        public int Count => artifactIds.Count;
        public int Capacity => capacity;
        public IReadOnlyList<string> Items => artifactIds;

        public bool TryCollect(string artifactId)
        {
            if (string.IsNullOrWhiteSpace(artifactId) || artifactIds.Count >= capacity || artifactIds.Contains(artifactId))
                return false;

            artifactIds.Add(artifactId);
            CargoChanged?.Invoke();
            return true;
        }

        public bool Has(string artifactId) => artifactIds.Contains(artifactId);

        public bool TryDeliver(string artifactId)
        {
            if (!artifactIds.Remove(artifactId)) return false;
            CargoChanged?.Invoke();
            return true;
        }

        public bool TryStealRandom(out string artifactId)
        {
            artifactId = null;
            if (artifactIds.Count == 0) return false;
            int index = UnityEngine.Random.Range(0, artifactIds.Count);
            artifactId = artifactIds[index];
            artifactIds.RemoveAt(index);
            CargoChanged?.Invoke();
            return true;
        }

        public void IncreaseCapacity(int amount)
        {
            capacity = Mathf.Clamp(capacity + Mathf.Max(0, amount), 1, 10);
            CargoChanged?.Invoke();
        }
    }
}

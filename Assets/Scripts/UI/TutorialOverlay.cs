using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class TutorialOverlay : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        [SerializeField] private GameObject panel;
        [SerializeField] private Button startDrivingButton;

        public void Configure(GameObject tutorialPanel, Button startButton)
        {
            panel = tutorialPanel;
            startDrivingButton = startButton;
        }

        private void Start()
        {
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            startDrivingButton?.onClick.AddListener(Close);
            Time.timeScale = 0f;
        }

        private void Close()
        {
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void OnDestroy() => IsOpen = false;
    }
}

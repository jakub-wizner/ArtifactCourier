using ArtifactCourier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        private bool paused;

        public void Configure(GameObject pausePanel, Button resume, Button mainMenu)
        {
            panel = pausePanel;
            resumeButton = resume;
            mainMenuButton = mainMenu;
        }

        private void Start()
        {
            panel?.SetActive(false);
            resumeButton?.onClick.AddListener(Resume);
            mainMenuButton?.onClick.AddListener(ReturnToMenu);
        }

        private void Update()
        {
            if (!RunResultScreen.IsOpen && !CosmeticShop.IsOpen && !GarageScreen.IsOpen && (LevelController.Instance==null || !LevelController.Instance.IsComplete) && !TutorialOverlay.IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                if (paused) Resume(); else Pause();
            }
        }

        private void Pause()
        {
            paused = true;
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Resume()
        {
            paused = false;
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1f;
            GameSession.Instance.ReturnToMainMenu();
        }
    }
}

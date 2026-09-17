using ArtifactCourier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class LevelCompletePanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button continueButton;
        [SerializeField] private AudioClip completeClip;
        private LevelController level;

        public void Configure(GameObject completePanel, Text title, Text summary, Button nextButton, AudioClip clip)
        {
            panel = completePanel;
            titleText = title;
            summaryText = summary;
            continueButton = nextButton;
            completeClip = clip;
        }

        private void Start()
        {
            level = LevelController.Instance;
            if (panel != null) panel.SetActive(false);
            if (level != null) level.LevelCompleted += Show;
            continueButton?.onClick.AddListener(Continue);
        }

        private void OnDestroy()
        {
            if (level != null) level.LevelCompleted -= Show;
        }

        private void Show()
        {
            if (level.LevelIndex == GameScenes.Levels.Length - 1)
            {
                if (panel != null) panel.SetActive(false);
                RunResultScreen.Show(true);
                if (completeClip != null) AudioSource.PlayClipAtPoint(completeClip, Vector3.zero, 0.8f);
                return;
            }
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (titleText != null) titleText.text = level.LevelIndex == GameScenes.Levels.Length - 1 ? "CAMPAIGN COMPLETE" : "LEVEL COMPLETE";
            if (summaryText != null) summaryText.text = $"{level.CityName}\nDeliveries: {level.DeliveredCount}/{level.RequiredDeliveries}\nEnemies defeated: {level.EnemiesDefeated}";
            var style = FindFirstObjectByType<ArtifactCourier.Driving.DrivingStyle>();
            if (summaryText != null && style != null) summaryText.text += $"\nStyle score: {style.Score:N0}   •   Personal best: {style.Best:N0}";
            if (continueButton != null)
            {
                var label = continueButton.GetComponentInChildren<Text>();
                if (label != null) label.text = level.LevelIndex == GameScenes.Levels.Length - 1 ? "MAIN MENU" : "NEXT CITY";
            }
            if (completeClip != null) AudioSource.PlayClipAtPoint(completeClip, Vector3.zero, 0.8f);
        }

        private void Continue()
        {
            if (panel != null) panel.SetActive(false);
            GarageScreen.Open(() => GameSession.Instance.LoadNextLevelOrMenu(level.LevelIndex));
        }
    }
}

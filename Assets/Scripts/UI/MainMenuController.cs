using ArtifactCourier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text saveInfoText;

        public void Configure(Button newButton, Button loadButton, Button exitButton, Text info)
        {
            newGameButton = newButton;
            loadGameButton = loadButton;
            quitButton = exitButton;
            saveInfoText = info;
        }

        private void Start()
        {
            newGameButton?.onClick.AddListener(NewGame);
            loadGameButton?.onClick.AddListener(LoadGame);
            quitButton?.onClick.AddListener(Quit);

            bool hasSave = GameSession.Instance.Saves.HasSave;
            if (loadGameButton != null) loadGameButton.interactable = hasSave;
            if (saveInfoText != null)
            {
                saveInfoText.text = hasSave
                    ? $"Continue from level {GameSession.Instance.Data.currentLevelIndex + 1}"
                    : "No saved game yet";
            }
        }

        private static void NewGame() => GameSession.Instance.StartNewGame();
        private static void LoadGame() => GameSession.Instance.LoadGame();

        private static void Quit()
        {
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }
    }
}

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArtifactCourier.Editor
{
    // Opening a project must not trigger an expensive scene build or reimport loop.
    [InitializeOnLoad]
    public static class ArtifactCourierAutoSetup
    {
        static ArtifactCourierAutoSetup()
        {
            EditorApplication.delayCall += ShowSetupHint;
        }

        private static void ShowSetupHint()
        {
            if (Application.isBatchMode || File.Exists("Assets/Scenes/MainMenu.unity")) return;
            Debug.Log("Artifact Courier: project loaded. When initial importing has finished, run Tools > Artifact Courier > Build Complete Game to generate the menu and seven cities. Scene generation is manual so opening the Editor stays lightweight.");
        }
    }
}
#endif

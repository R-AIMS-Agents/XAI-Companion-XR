using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrIVA.Scenes.Editor
{
    /// <summary>
    /// Editor-only hook that opens a scene's additive scenes alongside it when it is opened, using the list on the scene's <see cref="SceneController"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class AdditiveSceneAutoOpener
    {
        /// <summary>
        /// Subscribes to scene-open events when the Editor loads scripts.
        /// </summary>
        static AdditiveSceneAutoOpener()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        /// <summary>
        /// Opens the additive scenes listed on the opened scene's SceneController, skipping any that are already loaded.
        /// </summary>
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode != OpenSceneMode.Single) return;

            SceneController controller = FindController(scene);
            if (!controller) return;

            foreach (string sceneName in controller.AdditiveSceneNames)
            {
                if (string.IsNullOrEmpty(sceneName) || SceneManager.GetSceneByName(sceneName).isLoaded) continue;

                string path = FindBuildScenePath(sceneName);
                if (path == null)
                {
                    Debug.LogWarning($"AdditiveSceneAutoOpener could not open '{sceneName}': it is not in Build Settings.", controller);
                    continue;
                }

                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
        }

        /// <summary>
        /// Returns the first SceneController under the scene's root GameObjects, or null if there is none.
        /// </summary>
        private static SceneController FindController(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SceneController controller = root.GetComponentInChildren<SceneController>(true);
                if (controller) return controller;
            }
            return null;
        }

        /// <summary>
        /// Returns the asset path of the Build Settings scene with the given name, or null if it is not listed.
        /// </summary>
        private static string FindBuildScenePath(string sceneName)
        {
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (Path.GetFileNameWithoutExtension(buildScene.path) == sceneName)
                    return buildScene.path;
            }
            return null;
        }
    }
}

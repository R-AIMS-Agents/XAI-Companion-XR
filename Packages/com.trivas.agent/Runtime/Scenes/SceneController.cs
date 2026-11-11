using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrIVA.Utils;

namespace TrIVA.Scenes
{
    /// <summary>
    /// Persistent scene orchestrator that keeps the listed scenes loaded additively alongside whatever single scene is loaded, and optionally makes one of them the active scene.
    /// </summary>
    public class SceneController : Singleton<SceneController>
    {
        [SerializeField, Tooltip("Scenes to load additively at startup and after every single-scene load. Must be in Build Settings. Already loaded scenes are skipped.")]
        private string[] AdditiveScenes;
        [SerializeField, Tooltip("Scene to make active once loaded, so its skybox, ambient light and fog apply. Leave empty to keep the default active scene.")]
        private string ActiveScene;

        /// <summary>
        /// Names of the scenes this controller keeps loaded additively.
        /// </summary>
        public IReadOnlyList<string> AdditiveSceneNames => AdditiveScenes ?? Array.Empty<string>();

        /// <summary>
        /// Keeps the surviving instance alive across scene loads.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Loads the additive scenes for the initial scene and starts listening for later scene loads.
        /// </summary>
        private void Start()
        {
            LoadAdditiveScenes();
            ApplyActiveScene();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Stops listening for scene loads and releases the instance.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Reloads the additive scenes after a single-scene load and applies the active scene once it has loaded.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                LoadAdditiveScenes();

            ApplyActiveScene();
        }

        /// <summary>
        /// Starts an additive load for every listed scene that is not already loaded or loading.
        /// </summary>
        private void LoadAdditiveScenes()
        {
            if (AdditiveScenes == null) return;

            foreach (string sceneName in AdditiveScenes)
            {
                if (string.IsNullOrEmpty(sceneName) || SceneManager.GetSceneByName(sceneName).IsValid()) continue;

                if (SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive) == null)
                    Debug.LogError($"SceneController could not load '{sceneName}'. Is it in Build Settings?", this);
            }
        }

        /// <summary>
        /// Makes <see cref="ActiveScene"/> the active scene if it is set and fully loaded.
        /// </summary>
        private void ApplyActiveScene()
        {
            if (string.IsNullOrEmpty(ActiveScene)) return;

            Scene scene = SceneManager.GetSceneByName(ActiveScene);
            if (scene.isLoaded && SceneManager.GetActiveScene() != scene)
                SceneManager.SetActiveScene(scene);
        }
    }
}

using UnityEngine;

namespace XAICompanion.Utils
{
    /// <summary>
    /// Base class for MonoBehaviours that must exist at most once; the first instance to Awake becomes <see cref="Instance"/> and later duplicates destroy their GameObject.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>
        /// The live instance, or null before the first Awake and after it is destroyed.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Registers this component as the instance, or deactivates and destroys its GameObject if another instance already exists.
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning($"{typeof(T).Name}: a second instance exists on '{name}'. Destroying the duplicate.", this);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        /// <summary>
        /// Clears the instance when the registered component is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}

using UnityEngine;
using XAICompanion.Utils;

namespace XAICompanion.Agent
{
    /// <summary>
    /// Makes the agent blink at random intervals by animating the eyelid blendshapes.
    /// </summary>
    public class BlinkController : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer FaceMesh;
        [SerializeField] private FaceRigProfile FaceProfile;
        [SerializeField] private float MinBlinkInterval = 2f;
        [SerializeField] private float MaxBlinkInterval = 6f;
        // Total length of one blink: a quarter closing, half fully closed, a quarter opening.
        [SerializeField] private float BlinkDuration = 0.2f;
        [SerializeField, Range(0, 100), Tooltip("Blink blendshape weight at full closure, keeps the eyelids from over-closing.")]
        private float BlinkWeight = 80f;

        private int _eyeLBlinkIndex = -1;
        private int _eyeRBlinkIndex = -1;
        private float _blinkStartTime = float.NegativeInfinity;
        private float _nextBlinkTime;

        private void Awake()
        {
            if (!FaceMesh || !FaceProfile)
            {
                Debug.LogError("BlinkController needs a face SkinnedMeshRenderer and a FaceRigProfile assigned. Blinking disabled.", this);
                enabled = false;
                return;
            }

            _eyeLBlinkIndex = FindBlendshape(FaceProfile.Blink.Left);
            _eyeRBlinkIndex = FindBlendshape(FaceProfile.Blink.Right);
            _nextBlinkTime = Time.time + Random.Range(MinBlinkInterval, MaxBlinkInterval);
        }

        private void LateUpdate()
        {
            if (Time.time >= _nextBlinkTime)
                Blink();

            float weight = BlinkWeight * GetClosure(Time.time - _blinkStartTime);
            SetBlendshape(_eyeLBlinkIndex, weight);
            SetBlendshape(_eyeRBlinkIndex, weight);
        }

        /// <summary>
        /// Starts a blink now and schedules the next random blink after it.
        /// </summary>
        [Button("Blink", PlayModeOnly = true)]
        public void Blink()
        {
            _blinkStartTime = Time.time;
            _nextBlinkTime = Time.time + BlinkDuration + Random.Range(MinBlinkInterval, MaxBlinkInterval);
        }

        /// <summary>
        /// Eyelid closure from 0 (open) to 1 (closed) at a given time since the blink started.
        /// </summary>
        private float GetClosure(float elapsed)
        {
            if (elapsed <= 0f || elapsed >= BlinkDuration)
                return 0f;

            float ramp = BlinkDuration * 0.25f;
            if (elapsed < ramp)
                return elapsed / ramp;
            if (elapsed > BlinkDuration - ramp)
                return (BlinkDuration - elapsed) / ramp;
            return 1f;
        }

        /// <summary>
        /// Returns the index of a blendshape on the face mesh, or -1 with a warning when it is missing.
        /// </summary>
        private int FindBlendshape(string shapeName)
        {
            int index = FaceMesh.sharedMesh.GetBlendShapeIndex(shapeName);
            if (index < 0)
                Debug.LogWarning($"BlinkController: blendshape '{shapeName}' not found on '{FaceMesh.name}'.", this);
            return index;
        }

        /// <summary>
        /// Writes a blendshape weight, skipping shapes that were not found.
        /// </summary>
        private void SetBlendshape(int index, float weight)
        {
            if (index >= 0)
                FaceMesh.SetBlendShapeWeight(index, weight);
        }
    }
}

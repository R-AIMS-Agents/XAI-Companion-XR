using System;
using UnityEngine;

namespace XAICompanion.Agent
{
    /// <summary>
    /// Names of the face blendshapes that blinking and eye gaze drive, shared by every avatar from the same source (e.g. one profile for all Character Creator characters).
    /// </summary>
    [CreateAssetMenu(fileName = "FaceRigProfile", menuName = "XAI Companion/Face Rig Profile")]
    public class FaceRigProfile : ScriptableObject
    {
        [SerializeField, Tooltip("Blendshapes that close the eyelids.")] private EyeBlendshapes EyeBlink;
        [SerializeField, Tooltip("Blendshapes that turn the eyes to the agent's left.")] private EyeBlendshapes EyeLookLeft;
        [SerializeField, Tooltip("Blendshapes that turn the eyes to the agent's right.")] private EyeBlendshapes EyeLookRight;
        [SerializeField, Tooltip("Blendshapes that turn the eyes up.")] private EyeBlendshapes EyeLookUp;
        [SerializeField, Tooltip("Blendshapes that turn the eyes down.")] private EyeBlendshapes EyeLookDown;

        /// <summary>
        /// Blendshapes that close the eyelids.
        /// </summary>
        public EyeBlendshapes Blink => EyeBlink;

        /// <summary>
        /// Blendshapes that turn the eyes to the agent's left.
        /// </summary>
        public EyeBlendshapes LookLeft => EyeLookLeft;

        /// <summary>
        /// Blendshapes that turn the eyes to the agent's right.
        /// </summary>
        public EyeBlendshapes LookRight => EyeLookRight;

        /// <summary>
        /// Blendshapes that turn the eyes up.
        /// </summary>
        public EyeBlendshapes LookUp => EyeLookUp;

        /// <summary>
        /// Blendshapes that turn the eyes down.
        /// </summary>
        public EyeBlendshapes LookDown => EyeLookDown;
    }

    /// <summary>
    /// Pair of blendshape names, one per eye, for one eye movement.
    /// </summary>
    [Serializable]
    public struct EyeBlendshapes
    {
        [SerializeField] private string LeftEye;
        [SerializeField] private string RightEye;

        /// <summary>
        /// Blendshape name for the left eye.
        /// </summary>
        public string Left => LeftEye;

        /// <summary>
        /// Blendshape name for the right eye.
        /// </summary>
        public string Right => RightEye;
    }
}

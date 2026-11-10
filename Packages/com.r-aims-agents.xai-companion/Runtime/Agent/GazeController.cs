using System;
using UnityEngine;
using XAICompanion.Utils;

namespace XAICompanion.Agent
{
    /// <summary>
    /// Where the agent directs its gaze.
    /// </summary>
    public enum GazeMode { Idle, LookAtUser, LookAtTarget, Thinking }

    /// <summary>
    /// Directs the agent's gaze by writing the eye blendshapes and turning the head through Animator look-at IK.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public class GazeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SkinnedMeshRenderer FaceMesh;
        [SerializeField] private FaceRigProfile FaceProfile;
        [SerializeField] private Transform UserTarget;

        [Header("Behaviours")]
        [SerializeField] private bool EnableEyeGaze = true;
        [SerializeField] private bool EnableHeadGaze = true;
        [SerializeField] private bool DrawGazeDebugRay;

        [Header("Mode")]
        [SerializeField] private GazeMode Mode = GazeMode.Idle;

        [Header("Eye Gaze")]
        [SerializeField, Range(1, 90)] private float MaxEyeAngleH = 45f;
        [SerializeField, Range(1, 90)] private float MaxEyeAngleV = 25f;
        [SerializeField, Range(0, 100)] private float EyeBlendshapeStrength = 100f;
        [SerializeField, Tooltip("Seconds the eyes take to roughly reach a new gaze direction.")] private float EyeGazeSmoothTime = 0.1f;

        [Header("Head Gaze (IK)")]
        [SerializeField, Range(0, 1)] private float HeadLookWeight = 0.5f;
        [SerializeField, Tooltip("Seconds the head takes to roughly reach a new gaze direction.")] private float HeadGazeSmoothTime = 0.35f;
        [SerializeField] private Vector3 HeadLookOffset = Vector3.zero;

        [Header("Thinking")]
        // Look-away direction relative to the agent's forward in degrees: x is yaw (negative turns to the agent's left), y is pitch (positive looks up).
        [SerializeField] private Vector2 ThinkingGazeAngles = new(-20f, 15f);

        [Header("Relaxed Shifts")]
        [SerializeField, Tooltip("Multiplies the eye and head smooth times in Idle and Thinking, so self-directed gaze shifts are slower than looking at someone or something.")] private float RelaxedSmoothTimeMultiplier = 2f;

        private Animator _animator;
        private Transform _headBone;
        private Transform _eyeBoneL;
        private Transform _eyeBoneR;
        private Quaternion _headRestOffset = Quaternion.identity;

        private Vector3 _eyeGazeDirection;
        private Vector3 _headGazeDirection;
        private float _eyeAngularVelocity;
        private float _headAngularVelocity;
        private Vector3 _headLookAtPosition;
        private Transform _lookTarget;
        private Vector3 _lookTargetPoint;

        private int _eyeLLookLIndex = -1, _eyeRLookLIndex = -1, _eyeLLookRIndex = -1, _eyeRLookRIndex = -1;
        private int _eyeLLookUpIndex = -1, _eyeRLookUpIndex = -1, _eyeLLookDownIndex = -1, _eyeRLookDownIndex = -1;

        /// <summary>
        /// The gaze mode currently in effect.
        /// </summary>
        public GazeMode CurrentMode => Mode;

        /// <summary>
        /// Head orientation whose forward matches the agent's forward in the rest pose, independent of the rig's bone axes.
        /// </summary>
        private Quaternion HeadRotation => _headBone ? _headBone.rotation * _headRestOffset : transform.rotation;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator.avatar && _animator.avatar.isHuman)
            {
                _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
                _eyeBoneL = _animator.GetBoneTransform(HumanBodyBones.LeftEye);
                _eyeBoneR = _animator.GetBoneTransform(HumanBodyBones.RightEye);
            }

            if (_headBone)
                _headRestOffset = Quaternion.Inverse(_headBone.rotation) * transform.rotation;
            else if (EnableHeadGaze)
            {
                Debug.LogWarning("GazeController needs a Humanoid Avatar with a head bone. Head gaze disabled.", this);
                EnableHeadGaze = false;
            }

            if (FaceMesh && FaceProfile)
                InitBlendshapes();
            else if (EnableEyeGaze)
            {
                Debug.LogError("GazeController needs a face SkinnedMeshRenderer and a FaceRigProfile assigned. Eye gaze disabled.", this);
                EnableEyeGaze = false;
            }

            _eyeGazeDirection = _headGazeDirection = HeadRotation * Vector3.forward;
            _headLookAtPosition = GetEyeCenterPosition() + _headGazeDirection * 10f;
            _lookTargetPoint = GetEyeCenterPosition() + _headGazeDirection * 2f;
        }

        private void Update()
        {
            Vector3 targetDirection = GetTargetGazeDirection();
            float multiplier = Mode is GazeMode.Idle or GazeMode.Thinking ? RelaxedSmoothTimeMultiplier : 1f;

            _eyeGazeDirection = SmoothDampDirection(_eyeGazeDirection, targetDirection, ref _eyeAngularVelocity, EyeGazeSmoothTime * multiplier);
            _headGazeDirection = SmoothDampDirection(_headGazeDirection, targetDirection, ref _headAngularVelocity, HeadGazeSmoothTime * multiplier);

            if (EnableHeadGaze)
                _headLookAtPosition = _headBone.position + _headGazeDirection * 100f + HeadLookOffset;
        }

        private void LateUpdate()
        {
            if (EnableEyeGaze)
                ApplyGazeToBlendshapes(_eyeGazeDirection);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!EnableHeadGaze)
                return;

            _animator.SetLookAtWeight(HeadLookWeight, 0.3f, 1f, 0f, 0.5f);
            _animator.SetLookAtPosition(_headLookAtPosition);
        }

        private void OnDrawGizmos()
        {
            if (!DrawGazeDebugRay || !Application.isPlaying)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(GetEyeCenterPosition(), _eyeGazeDirection * 0.5f);
        }

        /// <summary>
        /// Switches the gaze mode; LookAtTarget reuses the last target given to LookAt.
        /// </summary>
        public void SetMode(GazeMode mode) => Mode = mode;

        /// <summary>
        /// Makes the agent follow a moving object with its gaze.
        /// </summary>
        public void LookAt(Transform target)
        {
            _lookTarget = target;
            if (target)
                _lookTargetPoint = target.position;
            Mode = GazeMode.LookAtTarget;
        }

        /// <summary>
        /// Makes the agent look at a fixed point in world space.
        /// </summary>
        [Button("Look At Point", PlayModeOnly = true)]
        public void LookAt(Vector3 worldPoint)
        {
            _lookTarget = null;
            _lookTargetPoint = worldPoint;
            Mode = GazeMode.LookAtTarget;
        }

        /// <summary>
        /// Inspector button that switches to Idle.
        /// </summary>
        [Button("Idle", PlayModeOnly = true)]
        private void SetModeIdle() => SetMode(GazeMode.Idle);

        /// <summary>
        /// Inspector button that switches to LookAtUser.
        /// </summary>
        [Button("Look At User", PlayModeOnly = true)]
        private void SetModeLookAtUser() => SetMode(GazeMode.LookAtUser);

        /// <summary>
        /// Inspector button that switches to LookAtTarget with the last target given to LookAt.
        /// </summary>
        [Button("Look At Target", PlayModeOnly = true)]
        private void SetModeLookAtTarget() => SetMode(GazeMode.LookAtTarget);

        /// <summary>
        /// Inspector button that switches to Thinking.
        /// </summary>
        [Button("Thinking", PlayModeOnly = true)]
        private void SetModeThinking() => SetMode(GazeMode.Thinking);
        /// <summary>
        /// Computes the world-space direction the current mode wants the gaze to move toward.
        /// </summary>
        private Vector3 GetTargetGazeDirection()
        {
            switch (Mode)
            {
                case GazeMode.Idle:
                    return GetIdleDirection();

                case GazeMode.LookAtUser:
                    Transform user = ResolveUserTarget();
                    return user ? DirectionFromEyes(user.position) : GetIdleDirection();

                case GazeMode.LookAtTarget:
                    if (_lookTarget)
                        _lookTargetPoint = _lookTarget.position;
                    return DirectionFromEyes(_lookTargetPoint);

                case GazeMode.Thinking:
                    return transform.rotation * Quaternion.Euler(-ThinkingGazeAngles.y, ThinkingGazeAngles.x, 0f) * Vector3.forward;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Rotates a direction toward a target with critically damped smoothing, starting and stopping without a velocity jump.
        /// </summary>
        private static Vector3 SmoothDampDirection(Vector3 current, Vector3 target, ref float angularVelocity, float smoothTime)
        {
            float angle = Vector3.Angle(current, target);
            if (angle < 0.01f)
            {
                angularVelocity = 0f;
                return target;
            }

            float remaining = Mathf.SmoothDamp(angle, 0f, ref angularVelocity, smoothTime);
            float step = Mathf.Max(0f, angle - remaining) * Mathf.Deg2Rad;
            return Vector3.RotateTowards(current, target, step, 0f).normalized;
        }

        /// <summary>
        /// Slow Perlin-noise wander around the agent's forward direction.
        /// </summary>
        private Vector3 GetIdleDirection()
        {
            float noiseX = Mathf.PerlinNoise(Time.time * 0.2f, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, Time.time * 0.2f) * 2f - 1f;
            return transform.TransformDirection(new Vector3(noiseX, noiseY, 1f).normalized);
        }

        /// <summary>
        /// Returns the assigned user target, falling back to the main (XR head) camera.
        /// </summary>
        private Transform ResolveUserTarget()
        {
            if (UserTarget)
                return UserTarget;

            Camera mainCamera = Camera.main;
            if (mainCamera)
                UserTarget = mainCamera.transform;

            return UserTarget;
        }

        /// <summary>
        /// Normalized direction from the agent's eyes to a world-space point.
        /// </summary>
        private Vector3 DirectionFromEyes(Vector3 worldPoint)
        {
            return (worldPoint - GetEyeCenterPosition()).normalized;
        }

        /// <summary>
        /// Midpoint between the eye bones, or an estimate from the head bone or root when the rig lacks them.
        /// </summary>
        private Vector3 GetEyeCenterPosition()
        {
            if (_eyeBoneL && _eyeBoneR)
                return (_eyeBoneL.position + _eyeBoneR.position) * 0.5f;
            if (_headBone)
                return _headBone.position + HeadRotation * Vector3.up * 0.1f;
            return transform.position + transform.up * 1.6f;
        }

        /// <summary>
        /// Converts the gaze direction into head-relative angles and writes them to the eye look blendshapes.
        /// </summary>
        private void ApplyGazeToBlendshapes(Vector3 gazeDirection)
        {
            Vector3 local = Quaternion.Inverse(HeadRotation) * gazeDirection;
            float h = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -MaxEyeAngleH, MaxEyeAngleH);
            float v = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -MaxEyeAngleV, MaxEyeAngleV);

            float left = Mathf.Max(0f, -h) / MaxEyeAngleH * EyeBlendshapeStrength;
            float right = Mathf.Max(0f, h) / MaxEyeAngleH * EyeBlendshapeStrength;
            float up = Mathf.Max(0f, v) / MaxEyeAngleV * EyeBlendshapeStrength;
            float down = Mathf.Max(0f, -v) / MaxEyeAngleV * EyeBlendshapeStrength;

            SetBlendshape(_eyeLLookLIndex, left);
            SetBlendshape(_eyeRLookLIndex, left);
            SetBlendshape(_eyeLLookRIndex, right);
            SetBlendshape(_eyeRLookRIndex, right);
            SetBlendshape(_eyeLLookUpIndex, up);
            SetBlendshape(_eyeRLookUpIndex, up);
            SetBlendshape(_eyeLLookDownIndex, down);
            SetBlendshape(_eyeRLookDownIndex, down);
        }

        /// <summary>
        /// Looks up the eye look blendshape indices on the face mesh.
        /// </summary>
        private void InitBlendshapes()
        {
            _eyeLLookLIndex = FindBlendshape(FaceProfile.LookLeft.Left);
            _eyeRLookLIndex = FindBlendshape(FaceProfile.LookLeft.Right);
            _eyeLLookRIndex = FindBlendshape(FaceProfile.LookRight.Left);
            _eyeRLookRIndex = FindBlendshape(FaceProfile.LookRight.Right);
            _eyeLLookUpIndex = FindBlendshape(FaceProfile.LookUp.Left);
            _eyeRLookUpIndex = FindBlendshape(FaceProfile.LookUp.Right);
            _eyeLLookDownIndex = FindBlendshape(FaceProfile.LookDown.Left);
            _eyeRLookDownIndex = FindBlendshape(FaceProfile.LookDown.Right);
        }

        /// <summary>
        /// Returns the index of a blendshape on the face mesh, or -1 with a warning when it is missing.
        /// </summary>
        private int FindBlendshape(string shapeName)
        {
            int index = FaceMesh.sharedMesh.GetBlendShapeIndex(shapeName);
            if (index < 0)
                Debug.LogWarning($"GazeController: blendshape '{shapeName}' not found on '{FaceMesh.name}'.", this);
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

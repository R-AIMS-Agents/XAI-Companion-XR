#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.TestTools;
using UnityEngine.XR.Management;

namespace XAICompanion.Tests
{
    /// <summary>
    /// Keeps XR from starting when a PlayMode test run enters Play mode, so the tests never wait on the OpenXR runtime, and restores the setting after the run; the change stays in memory and is never saved to the XR settings asset.
    /// </summary>
    public class XrOffDuringTests : IPrebuildSetup, IPostBuildCleanup
    {
        // Session key holding whether XR started on Play mode before the run (1 or 0), absent while no run has it switched off; kept in the session since the run reloads the domain in between.
        private const string PreviousKey = "XAICompanion.Tests.XrInitOnStartBeforeRun";
        private const int NotRemembered = -1;

        /// <summary>
        /// Remembers whether the Standalone XR settings start XR on Play mode and switches that off; a second call in the same run keeps the first remembered value.
        /// </summary>
        public void Setup()
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (!settings)
                return;
            if (SessionState.GetInt(PreviousKey, NotRemembered) == NotRemembered)
                SessionState.SetInt(PreviousKey, settings.InitManagerOnStart ? 1 : 0);
            settings.InitManagerOnStart = false;
        }

        /// <summary>
        /// Gives the Standalone XR settings back the value remembered before the run, if one was remembered.
        /// </summary>
        public void Cleanup()
        {
            int previous = SessionState.GetInt(PreviousKey, NotRemembered);
            if (previous == NotRemembered)
                return;
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings)
                settings.InitManagerOnStart = previous == 1;
            SessionState.EraseInt(PreviousKey);
        }
    }
}
#endif

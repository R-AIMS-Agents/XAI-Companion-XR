# XAI-Companion-XR

Unity XR client for an embodied, explainable AI companion. LiveKit carries audio and data between this client and a separate Python backend; the agent's speech drives lip sync, blinking and gaze on a humanoid avatar.

The agent code lives in the embedded package `Packages/com.r-aims-agents.xai-companion`. This project hosts it with a sample scene.

## Requirements

- Unity 6000.7 (URP), XR Interaction Toolkit 3.7.0, OpenXR.
- Git packages in `Packages/manifest.json` (Unity packages cannot declare these themselves): [LiveKit Unity SDK](https://github.com/livekit/client-sdk-unity) and [uLipSync](https://github.com/hecomi/uLipSync).
- XR Interaction Toolkit samples **Starter Assets** and **World Space UI**, imported through the Package Manager; the debug panel prefab uses them.

## Using the package in another project

Add the two git packages above and the package itself to that project's `Packages/manifest.json`, either as a local path (`"file:<path to>/Packages/com.r-aims-agents.xai-companion"`) or as a git URL with `?path=/Packages/com.r-aims-agents.xai-companion`.

## Licence

MIT, see [LICENSE](LICENSE). Third-party content is listed in the package's `Third Party Notices.md`.

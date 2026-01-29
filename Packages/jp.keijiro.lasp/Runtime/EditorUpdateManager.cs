#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Lasp
{
    /// <summary>
    /// Manages edit-mode updates for LASP components.
    /// Ensures AudioSystem.Update() is only called once per frame and throttles update rate.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorUpdateManager
    {
        private static double _lastUpdateTime;
        private static bool _hasUpdatedThisFrame;
        private static int _activeComponentCount;

        // Target FPS for audio updates in edit mode (lower = better performance)
        public static float TargetFPS { get; set; } = 30f;

        // Whether edit mode updates are enabled globally
        public static bool Enabled { get; set; } = true;

        static EditorUpdateManager()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Reset state when entering/exiting play mode
            _activeComponentCount = 0;
            _hasUpdatedThisFrame = false;
        }

        public static void RegisterComponent()
        {
            _activeComponentCount++;
        }

        public static void UnregisterComponent()
        {
            _activeComponentCount = Mathf.Max(0, _activeComponentCount - 1);
        }

        private static void Update()
        {
            if (Application.isPlaying || !Enabled || _activeComponentCount == 0)
            {
                _hasUpdatedThisFrame = false;
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            double targetDelta = 1.0 / TargetFPS;

            // Throttle updates based on target FPS
            if (currentTime - _lastUpdateTime < targetDelta)
            {
                _hasUpdatedThisFrame = false;
                return;
            }

            // Update AudioSystem once per frame
            // Wrapped in try-catch to handle audio device disconnections gracefully
            try
            {
                AudioSystem.Update();
                _lastUpdateTime = currentTime;
                _hasUpdatedThisFrame = true;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"LASP EditorUpdateManager: Audio system error (device may have disconnected): {e.Message}");

                // Temporarily disable to prevent crash loop
                Enabled = false;
                UnityEngine.Debug.LogWarning("LASP EditorUpdateManager: Temporarily disabled edit mode updates. Re-enable manually after audio device is stable.");

                _hasUpdatedThisFrame = false;
            }
        }

        /// <summary>
        /// Check if AudioSystem was updated this frame
        /// </summary>
        public static bool WasUpdatedThisFrame => _hasUpdatedThisFrame;

        /// <summary>
        /// Get the calculated delta time for this frame
        /// </summary>
        public static float GetDeltaTime()
        {
            return 1.0f / TargetFPS;
        }
    }
}
#endif

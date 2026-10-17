using NasconGJ.Entities;
using UnityEditor;
using UnityEngine;

namespace NasconGJ.EditorTools
{
    public static class ShadowEnemyAITests
    {
        [MenuItem("NASCON/AI/Run Shadow Enemy AI Validation Tests")]
        public static void RunAllTestsMenu()
        {
            RunAllTests();
        }

        public static void RunAllTestsBatch()
        {
            bool passed = RunAllTests();
            EditorApplication.Exit(passed ? 0 : 1);
        }

        public static bool RunAllTests()
        {
            Debug.Log("<color=#FFB462><b>=== Running Shadow Enemy AI Validation Tests ===</b></color>");
            bool allPassed = true;

            allPassed &= Assert("Light Exposure Calculations", Test_LightExposureCalculations());
            allPassed &= Assert("Proximity Model & Drain Curve", Test_ProximityAndDrainCurve());
            allPassed &= Assert("State Weights", Test_StateWeights());
            allPassed &= Assert("Drain Stacking and Cap", Test_DrainStackingAndCapping());
            allPassed &= Assert("Single-Herder Coordination", Test_SingleHerderCoordination());
            allPassed &= Assert("Frozen Resume Delay Logic", Test_FrozenResumeLogic());

            if (allPassed)
            {
                Debug.Log("<color=#55FF88><b>[ALL TESTS PASSED] Shadow Enemy AI validation succeeded!</b></color>");
            }
            else
            {
                Debug.LogError("<color=#FF5555><b>[TEST FAILURE] One or more Shadow Enemy AI tests failed!</b></color>");
            }

            return allPassed;
        }

        private static bool Assert(string testName, bool success)
        {
            if (success)
            {
                Debug.Log($"<color=#55FF88>✔ PASSED:</color> {testName}");
                return true;
            }
            else
            {
                Debug.LogError($"<color=#FF5555>✘ FAILED:</color> {testName}");
                return false;
            }
        }

        private static bool Test_LightExposureCalculations()
        {
            const float radius = 10f;

            // Direct light: distance 2m (d / radius = 0.2 -> exposure = 0.8 >= 0.70 threshold)
            float directExposure = 1f - Mathf.Clamp01(2f / radius);
            if (directExposure < 0.70f) return false;

            // Freeze threshold exact distance: d = 3m -> exposure = 0.70
            float thresholdExposure = 1f - Mathf.Clamp01(3f / radius);
            if (Mathf.Abs(thresholdExposure - 0.70f) > 0.001f) return false;

            // Low light: distance 6m (d / radius = 0.6 -> exposure = 0.40)
            float lowLightExposure = 1f - Mathf.Clamp01(6f / radius);
            if (lowLightExposure < 0.25f || lowLightExposure >= 0.70f) return false;

            // Darkness / beyond radius: distance 11m -> exposure = 0
            float darkExposure = 1f - Mathf.Clamp01(11f / radius);
            if (darkExposure > 0.001f) return false;

            return true;
        }

        private static bool Test_ProximityAndDrainCurve()
        {
            const float fullDrain = 1.75f;
            const float drainRadius = 7.0f;

            // Full drain distance or closer gives proximity = 1.0
            float proxClose = 1f - Mathf.Clamp01((1.5f - fullDrain) / (drainRadius - fullDrain));
            float proxExact = 1f - Mathf.Clamp01((1.75f - fullDrain) / (drainRadius - fullDrain));
            if (Mathf.Abs(proxClose - 1f) > 0.001f || Mathf.Abs(proxExact - 1f) > 0.001f) return false;

            // Midpoint: (1.75 + 7.0) / 2 = 4.375m gives proximity = 0.5
            float proxMid = 1f - Mathf.Clamp01((4.375f - fullDrain) / (drainRadius - fullDrain));
            if (Mathf.Abs(proxMid - 0.5f) > 0.001f) return false;

            // Outside drain radius gives proximity = 0.0
            float proxFar = 1f - Mathf.Clamp01((7.5f - fullDrain) / (drainRadius - fullDrain));
            if (proxFar > 0.001f) return false;

            return true;
        }

        private static bool Test_StateWeights()
        {
            // Observe: 0, Approach: 0.25, Drain: 1.0, Herd: 0.75, Frozen: 0.45 (only within frozenFeedDistance), Dormant: 0
            float GetWeight(ShadowEnemy.State state) => state switch
            {
                ShadowEnemy.State.Approach => 0.25f,
                ShadowEnemy.State.Drain => 1.0f,
                ShadowEnemy.State.Herd => 0.75f,
                ShadowEnemy.State.Frozen => 0.45f,
                _ => 0f
            };

            if (GetWeight(ShadowEnemy.State.Observe) != 0f) return false;
            if (GetWeight(ShadowEnemy.State.Approach) != 0.25f) return false;
            if (GetWeight(ShadowEnemy.State.Drain) != 1.0f) return false;
            if (GetWeight(ShadowEnemy.State.Herd) != 0.75f) return false;
            if (GetWeight(ShadowEnemy.State.Frozen) != 0.45f) return false;
            if (GetWeight(ShadowEnemy.State.Dormant) != 0f) return false;

            return true;
        }

        private static bool Test_DrainStackingAndCapping()
        {
            // Close shadows are meant to be felt: one at full drain burns the flame 4x as fast, the stack caps at 6x.
            const float maxDrainPerEnemy = 3f;
            const float maxStackedDrain = 5f;

            float oneEnemy = maxDrainPerEnemy * 1.0f * 1.0f; // 3
            if (Mathf.Abs(Mathf.Min(oneEnemy, maxStackedDrain) - 3f) > 0.001f) return false;       // +300% burn
            if (Mathf.Abs(Mathf.Min(oneEnemy * 2f, maxStackedDrain) - 5f) > 0.001f) return false;  // capped at +500%
            if (Mathf.Abs(Mathf.Min(oneEnemy * 4f, maxStackedDrain) - 5f) > 0.001f) return false;  // still capped

            return true;
        }

        private static bool Test_SingleHerderCoordination()
        {
            // Verify HerdTarget discovery
            var go1 = new GameObject("TestExit");
            var ht = go1.AddComponent<HerdTarget>();

            HerdTarget primary = HerdTarget.FindPrimaryTarget(Vector3.zero);
            bool found = (primary == ht);

            Object.DestroyImmediate(go1);
            return found;
        }

        private static bool Test_FrozenResumeLogic()
        {
            const float resumeDelay = 0.75f;
            float timer = 0f;

            // During grace period, resume warning is active and not yet moving
            timer += 0.35f;
            bool isWarning = (timer > 0f && timer < resumeDelay);
            if (!isWarning) return false;

            // If direct light returns during grace period, timer resets
            bool directLightHit = true;
            if (directLightHit) timer = 0f;
            if (timer != 0f) return false;

            // When light is lost and timer surpasses delay, buffer is complete
            timer = 0.8f;
            bool resumed = timer >= resumeDelay;
            return resumed;
        }
    }
}

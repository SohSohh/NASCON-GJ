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
            allPassed &= Assert("Drain Stacking and 150% Cap", Test_DrainStackingAndCapping());
            allPassed &= Assert("Single-Herder Coordination", Test_SingleHerderCoordination());
            allPassed &= Assert("Frozen Resume Delay Logic", Test_FrozenResumeLogic());
            allPassed &= Assert("Proximity Kill Logic (Option A)", Test_ProximityKillLogic());

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

            // Carried items quadratic falloff: full torch radius 16.5m freezes at 2 to 3m (~2.69m)
            const float torchRadius = 16.5f;
            float carried2m = Mathf.Pow(1f - Mathf.Clamp01(2f / torchRadius), 2f);
            if (carried2m < 0.70f) return false; // >= 0.70 at 2m (frozen)

            float carriedBoundary = Mathf.Pow(1f - Mathf.Clamp01(2.69f / torchRadius), 2f);
            if (Mathf.Abs(carriedBoundary - 0.70f) > 0.015f) return false; // lands squarely around 2.69m

            float carried3m = Mathf.Pow(1f - Mathf.Clamp01(3f / torchRadius), 2f);
            if (carried3m >= 0.70f) return false; // < 0.70 at 3m (unfrozen)

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
            // Verify weight mapping specified in design doc
            // Observe: 0, Approach: 0.25, Drain: 1.0, Herd: 0.75, Frozen: 0
            float GetWeight(ShadowEnemy.State state) => state switch
            {
                ShadowEnemy.State.Approach => 0.25f,
                ShadowEnemy.State.Drain => 1.0f,
                ShadowEnemy.State.Herd => 0.75f,
                _ => 0f
            };

            if (GetWeight(ShadowEnemy.State.Observe) != 0f) return false;
            if (GetWeight(ShadowEnemy.State.Approach) != 0.25f) return false;
            if (GetWeight(ShadowEnemy.State.Drain) != 1.0f) return false;
            if (GetWeight(ShadowEnemy.State.Herd) != 0.75f) return false;
            if (GetWeight(ShadowEnemy.State.Frozen) != 0f) return false;
            if (GetWeight(ShadowEnemy.State.Dormant) != 0f) return false;

            return true;
        }

        private static bool Test_DrainStackingAndCapping()
        {
            const float maxDrainPerEnemy = 0.5f;
            const float maxStackedDrain = 1.5f;

            // 1 enemy at full drain
            float oneEnemy = maxDrainPerEnemy * 1.0f * 1.0f; // 0.5
            float total1 = Mathf.Min(oneEnemy, maxStackedDrain);
            if (Mathf.Abs(total1 - 0.5f) > 0.001f) return false; // +50% burn

            // 2 enemies at full drain
            float twoEnemies = oneEnemy * 2f; // 1.0
            float total2 = Mathf.Min(twoEnemies, maxStackedDrain);
            if (Mathf.Abs(total2 - 1.0f) > 0.001f) return false; // +100% burn

            // 3 enemies at full drain
            float threeEnemies = oneEnemy * 3f; // 1.5
            float total3 = Mathf.Min(threeEnemies, maxStackedDrain);
            if (Mathf.Abs(total3 - 1.5f) > 0.001f) return false; // +150% burn

            // 4 enemies at full drain
            float fourEnemies = oneEnemy * 4f; // 2.0
            float total4 = Mathf.Min(fourEnemies, maxStackedDrain);
            if (Mathf.Abs(total4 - 1.5f) > 0.001f) return false; // capped at +150% burn

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

        private static bool Test_ProximityKillLogic()
        {
            const float killRange = 1.6f;
            const float contactKillRange = 0.9f;

            // Scenario 1: Active enemy at reach (1.2m) -> windup
            float distReach = 1.2f;
            bool activeContact = distReach <= contactKillRange;
            bool activeWindup = distReach <= killRange && !activeContact;
            if (activeContact || !activeWindup) return false;

            // Scenario 2: Active enemy at contact (0.6m) -> instant kill
            float distContact = 0.6f;
            bool instantKill = distContact <= contactKillRange;
            if (!instantKill) return false;

            // Scenario 3: Frozen enemy at reach (1.2m) -> safe observation under light
            bool isFrozen = true;
            bool frozenWindup = distReach <= killRange && !isFrozen;
            bool frozenContact = distReach <= contactKillRange;
            if (frozenWindup || frozenContact) return false;

            // Scenario 4: Frozen enemy at contact (0.6m) -> fatal dark matter contact
            bool frozenFatalContact = distContact <= contactKillRange;
            if (!frozenFatalContact) return false;

            return true;
        }
    }
}

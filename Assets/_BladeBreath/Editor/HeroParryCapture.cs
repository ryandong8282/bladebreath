using System;
using System.IO;
using BladeBreath;
using UnityEditor;
using UnityEngine;

namespace BladeBreathEditor
{
    // Stages one honest runtime parry with the real combatants, character prefabs,
    // camera, HUD and feedback, then captures the Game view at the impact peak.
    public static class HeroParryCapture
    {
        internal const string OutputPath = "Logs/grey-kiln-hero-parry.png";

        private static int _readyFrames;
        private static double _contactTime;
        private static bool _contactTriggered;

        [MenuItem("BladeBreath/Art/Capture Hero Parry")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before capturing the hero parry.");

            _readyFrames = 0;
            _contactTriggered = false;
            Time.timeScale = 1f;
            PrototypeBootstrap.Instance.ResetPrototype();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                FinishWithError("Play Mode ended before the hero parry could be captured.");
                return;
            }

            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            EnemyController enemy = UnityEngine.Object.FindFirstObjectByType<EnemyController>();
            if (player == null || enemy == null || Camera.main == null)
            {
                _readyFrames = 0;
                return;
            }

            if (!_contactTriggered)
            {
                if (++_readyFrames < 8) return;
                StageContact(player, enemy);
                _contactTime = EditorApplication.timeSinceStartup;
                _contactTriggered = true;
                return;
            }

            if (EditorApplication.timeSinceStartup - _contactTime < 0.065d) return;
            Directory.CreateDirectory("Logs");
            string absolutePath = Path.GetFullPath(OutputPath);
            ScreenCapture.CaptureScreenshot(absolutePath, 1);
            EditorApplication.update -= Tick;
            Debug.Log("Queued Grey Kiln hero-parry screenshot: " + absolutePath);
        }

        private static void StageContact(PlayerController player, EnemyController enemy)
        {
            player.enabled = false;
            enemy.enabled = false;
            player.transform.SetPositionAndRotation(
                new Vector3(-1.55f, 0f, -0.72f),
                Quaternion.LookRotation(new Vector3(1.48f, 0f, 0.96f), Vector3.up));
            enemy.transform.SetPositionAndRotation(
                new Vector3(-0.02f, 0f, 0.28f),
                Quaternion.LookRotation(new Vector3(-1.48f, 0f, -0.96f), Vector3.up));
            Physics.SyncTransforms();

            TopDownCamera camera = UnityEngine.Object.FindFirstObjectByType<TopDownCamera>();
            if (camera != null) camera.SnapZoomForCapture(7.8f);

            FighterPresentation playerPresentation = player.GetComponentInChildren<FighterPresentation>();
            FighterPresentation enemyPresentation = enemy.GetComponentInChildren<FighterPresentation>();
            playerPresentation.PreviewGuardPose(true);
            enemyPresentation.PreviewAttackPose(1, 0.5f);

            player.Combatant.BeginGuard(1f);
            enemy.Combatant.BeginAttackWindow();
            CombatOutcome outcome = player.Combatant.ReceiveAttack(
                enemy.Combatant,
                new AttackData(13f, 26f, false, "造像署劈斩"));
            if (outcome != CombatOutcome.Parried)
                throw new InvalidOperationException("Hero capture expected a real parry but received " + outcome + ".");

            // ReceiveAttack changes combat state; resample the desired impact silhouettes
            // and freeze only their presentation components for the capture frame.
            playerPresentation.PreviewGuardPose(true);
            enemyPresentation.PreviewAttackPose(1, 0.5f);
            playerPresentation.enabled = false;
            enemyPresentation.enabled = false;
            enemyPresentation.ReportWeaponContact(player.Combatant, outcome);
        }

        private static void FinishWithError(string message)
        {
            EditorApplication.update -= Tick;
            Debug.LogError(message);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BladeBreath;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace BladeBreathEditor
{
    // Opt-in integration checks use a temporary keyboard and the real controllers in Play Mode.
    // They do not replace the manual visual check or an iPhone performance/input test.
    [InitializeOnLoad]
    public static class HumanoidPlayModeValidation
    {
        private const string PendingKey = "BladeBreath.PlayModeValidation";
        private const string ExitAfterKey = "BladeBreath.PlayModeValidation.ExitAfter";
        private const string RequestPath = "Logs/run-humanoid-playmode-validation.request";
        private const string ReportPath = "Logs/humanoid-playmode-validation.txt";
        private static readonly Stack<IEnumerator> _steps = new Stack<IEnumerator>();
        private static readonly List<string> _checks = new List<string>();
        private static readonly List<string> _warnings = new List<string>();
        private static int _lastFrame;
        private static PlayerController _player;
        private static EnemyController _enemy;

        static HumanoidPlayModeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            QueueRequestedRun();
        }

        private static void QueueRequestedRun()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            EditorApplication.delayCall += Run;
        }

        [MenuItem("BladeBreath/Art/Run Humanoid Play Mode Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before starting the checks.");
            HumanoidSetupValidation.ValidateAssets();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(HumanoidSandboxBuilder.ScenePath);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, "RUNNING\n");
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        public static void RunAndExit()
        {
            SessionState.SetBool(ExitAfterKey, true);
            Run();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                _checks.Clear();
                _warnings.Clear();
                _lastFrame = -1;
                Application.logMessageReceived += OnLog;
                _steps.Push(CheckRuntime());
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(PendingKey, false))
                Finish("FAIL: Play Mode was stopped before validation finished.");
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ExitAfterKey, false))
            {
                SessionState.SetBool(ExitAfterKey, false);
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                QueueRequestedRun();
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _warnings.Add(type + ": " + message);
        }

        private static void Tick()
        {
            if (Time.frameCount == _lastFrame) return;
            _lastFrame = Time.frameCount;
            try
            {
                while (_steps.Count > 0)
                {
                    IEnumerator step = _steps.Peek();
                    if (!step.MoveNext()) { _steps.Pop(); continue; }
                    if (step.Current is IEnumerator nested) { _steps.Push(nested); continue; }
                    return;
                }
                Require(_warnings.Count == 0, "No new Play Mode warnings/errors. " + string.Join("; ", _warnings));
                Finish("PASS");
            }
            catch (Exception error) { Finish("FAIL: " + error.Message); }
        }

        private static IEnumerator CheckRuntime()
        {
#if !ENABLE_INPUT_SYSTEM
            throw new InvalidOperationException("Enable the Input System backend in Player Settings before running input checks.");
#else
            yield return ResetFixture();
            PrototypeScreenshot.CaptureCurrentGameView();
            PrototypeScreenshot.CaptureFighterTextureCloseup();
            yield return null;
            yield return null;
            // The editor's optional Search index can finish its own startup work after Play Mode
            // begins in a fresh validation project. Only runtime warnings after the fixture is
            // ready belong to this combat regression check.
            _warnings.Clear();
            FighterPresentation visual = _player.GetComponentInChildren<FighterPresentation>();
            Animator animator = visual.Animator;
            float feet = Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,
                animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            float head = animator.GetBoneTransform(HumanBodyBones.Head).position.y;
            Require(head - feet > 1.1f && head - feet < 2f && feet > -0.2f && feet < 0.35f, "Humanoid upright and feet near floor.");

            TopDownCamera followCamera = UnityEngine.Object.FindFirstObjectByType<TopDownCamera>();
            Require(followCamera != null, "Combat scene owns one interactive top-down camera.");
            float initialZoom = followCamera.CurrentFramingDistance;
            followCamera.SetZoomForValidation(5.8f);
            yield return Wait(0.18f);
            Require(followCamera.CurrentFramingDistance < initialZoom - 0.7f,
                "Camera smoothly reaches the close mouse-wheel zoom range.");
            followCamera.SetZoomForValidation(8.8f);
            yield return Wait(0.18f);

            Vector3 start = _player.transform.position;
            Keys(Key.W);
            yield return Wait(0.3f);
            Keys();
            Require(Vector3.Distance(start, _player.transform.position) > 0.8f, "Keyboard movement reaches CharacterController.");
            Require(animator.GetFloat("Speed") > 1f, "Movement drives the locomotion animation.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            float health = _enemy.Combatant.Health;
            FighterPresentation attackVisual = _player.GetComponentInChildren<FighterPresentation>();
            CombatFeedback enemyFeedback = _enemy.GetComponent<CombatFeedback>();
            int contactCount = enemyFeedback.WeaponContactCount;
            Keys(Key.J);
            yield return null;
            Require(_enemy.Combatant.Health == health, "Attack startup does not deal early damage.");
            yield return Wait(0.2f);
            Keys();
            Require(_enemy.Combatant.Health == health - 12f, "First combo strike hits once through the real physics query.");
            yield return Wait(0.06f);
            Require(attackVisual.IsShowingWeaponTrail && attackVisual.WeaponTrailSampleCount >= 2,
                "The visible attack ribbon records the moving blade base and tip after contact.");
            Require(enemyFeedback.WeaponContactCount == contactCount + 1 &&
                    Vector3.Distance(enemyFeedback.LastWeaponContactPosition,
                        _enemy.transform.position + Vector3.up * 1.02f) < 1.2f,
                "The resolved hit emits directional sparks beside the defender instead of at an unrelated point.");
            yield return Wait(0.25f);

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            _enemy.transform.rotation = Quaternion.LookRotation(
                _player.transform.position - _enemy.transform.position,
                Vector3.up);
            Physics.SyncTransforms();
            health = _enemy.Combatant.Health;
            Keys(Key.J);
            yield return null;
            Keys();
            yield return Wait(0.07f);
            Keys(Key.J);
            yield return null;
            Keys();
            yield return Wait(0.37f);
            Require(_enemy.Combatant.Health == health - 26f, "Buffered attack input reaches the mirrored second combo strike.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "Attack2"),
                "Second combo strike uses its distinct Animator state.");
            Keys(Key.J);
            yield return null;
            Keys();
            yield return Wait(0.4f);
            Require(_enemy.Combatant.Health == health - 47f, "A second buffered input reaches the heavy third combo strike.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "Attack3"),
                "Heavy combo finisher uses its distinct Animator state.");
            yield return Wait(0.35f);

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            _enemy.transform.rotation = Quaternion.LookRotation(
                _player.transform.position - _enemy.transform.position,
                Vector3.up);
            Physics.SyncTransforms();
            health = _enemy.Combatant.Health;
            Keys(Key.L);
            yield return null;
            Keys();
            yield return Wait(0.4f);
            Require(_enemy.Combatant.Health == health - 30f,
                "Dedicated heavy input resolves the overhead strike once.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "HeavyAttack"),
                "Dedicated heavy strike uses its overhead Animator state.");
            yield return Wait(0.5f);

            yield return ResetFixture();
            health = _player.Combatant.Health;
            float enemyHealth = _enemy.Combatant.Health;
            _player.Combatant.BeginAttackWindow();
            _enemy.Combatant.BeginAttackWindow();
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(13f, 24f)) == CombatOutcome.Clashed,
                "Overlapping light attack windows resolve as a weapon clash.");
            Require(_player.Combatant.Health == health && _enemy.Combatant.Health == enemyHealth &&
                    _player.Combatant.IsStaggered && _enemy.Combatant.IsStaggered,
                "A weapon clash deals posture recoil without health damage.");
            Require(_player.Combatant.Edge == 1 && _player.Combatant.HasClashMomentum,
                "A player weapon clash grants one Edge and the temporary clash-momentum buff.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            _player.Combatant.GainEdge("Validation", true);
            health = _enemy.Combatant.Health;
            Keys(Key.H);
            yield return null;
            Keys();
            yield return Wait(0.34f);
            Require(Mathf.Abs(_enemy.Combatant.Health - (health - 21.24f)) < 0.05f && _player.Combatant.Edge == 0,
                "Ying Tui consumes one Edge and applies its clash-buffed skill damage once.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "SkillAttack"),
                "Ying Tui uses its distinct sampled attack silhouette.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            _enemy.transform.rotation = Quaternion.LookRotation(
                _player.transform.position - _enemy.transform.position,
                Vector3.up);
            Physics.SyncTransforms();
            _player.Combatant.GainEdge("Validation", false);
            health = _enemy.Combatant.Health;
            Keys(Key.U);
            yield return null;
            Keys();
            Require(_player.Combatant.Edge == 0 && _player.Combatant.IsParrying &&
                    _player.GetSkillCooldownRemaining(1) > 2f,
                "Hui Feng spends one Edge, starts cooldown and opens its counter window.");
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(13f, 24f)) == CombatOutcome.Parried,
                "Hui Feng's short guard window parries a committed incoming strike.");
            yield return Wait(0.38f);
            Require(Mathf.Abs(_enemy.Combatant.Health - (health - 16f)) < 0.05f,
                "Hui Feng returns a distinct counter cut after its defensive window.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "SkillAttack"),
                "Hui Feng uses the counter variant of the sampled skill silhouette.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            _player.Combatant.GainEdge("Validation", false);
            _player.Combatant.GainEdge("Validation", false);
            health = _enemy.Combatant.Health;
            Keys(Key.I);
            yield return null;
            Keys();
            Require(_player.Combatant.Edge == 0 && _player.GetSkillCooldownRemaining(2) > 4f,
                "Zhen Lie spends two Edge and starts its longer finisher cooldown.");
            yield return Wait(0.5f);
            Require(Mathf.Abs(_enemy.Combatant.Health - (health - 34f)) < 0.05f,
                "Zhen Lie resolves its broad high-cost strike once.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "SkillAttack"),
                "Zhen Lie uses the overhead circular skill silhouette.");

            yield return ResetFixture();
            Keys(Key.K);
            yield return null;
            yield return null;
            Require(_player.Combatant.IsParrying, "Guard input opens the startup parry window.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "GuardEnter"),
                "Startup guard uses its entry animation slot.");
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(13f, 24f)) == CombatOutcome.Parried,
                "Startup guard parries and staggers the attacker.");
            Keys();
            yield return Wait(0.2f);
            Keys(Key.K);
            yield return Wait(0.22f);
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(13f, 24f)) == CombatOutcome.Guarded,
                "Held guard blocks after the parry window closes.");
            yield return null;
            Require(_player.GetComponentInChildren<FighterPresentation>().IsShowingGuardImpact,
                "A blocked hit activates its guard-impact presentation window.");
            Keys();

            yield return ResetFixture();
            start = _player.transform.position;
            Keys(Key.Space);
            yield return null;
            yield return null;
            Require(_player.Combatant.IsInvulnerable, "Dodge input opens invulnerability.");
            Require(AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "Dodge"),
                "Forward dodge uses the full-body roll animation slot.");
            Require(_player.GetComponentInChildren<FighterPresentation>().IsShowingDodgeTrail,
                "Dodge emits the authored afterimage trail.");
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(13f, 24f)) == CombatOutcome.Dodged,
                "Incoming hit misses during the dodge window.");
            Keys();
            yield return Wait(0.3f);
            Require(Vector3.Distance(start, _player.transform.position) > 1.8f && !_player.Combatant.IsInvulnerable,
                "Longer dodge moves the collision root clearly and invulnerability expires.");
            FighterPresentation dodgePresentation = _player.GetComponentInChildren<FighterPresentation>();
            // The controller clears a finished action every Update. Pause it while sampling the
            // presentation directly so LateUpdate can evaluate the authored side-dodge states.
            _player.enabled = false;
            dodgePresentation.SampleDodge(0.4f, -1f);
            yield return null;
            Require(AnimatorInState(dodgePresentation.Animator, "DodgeLeft"),
                "Left strafe dodge has its own retargeted animation.");
            dodgePresentation.SampleDodge(0.4f, 1f);
            yield return null;
            Require(AnimatorInState(dodgePresentation.Animator, "DodgeRight"),
                "Right strafe dodge has its own retargeted animation.");
            dodgePresentation.EndAction();
            _player.enabled = true;

            yield return ResetFixture();
            health = _player.Combatant.Health;
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(13f, 24f)) == CombatOutcome.Hit,
                "An unguarded enemy strike resolves as a body hit.");
            yield return null;
            Require(_player.Combatant.Health < health && _player.Combatant.IsHitReacting &&
                    AnimatorInState(_player.GetComponentInChildren<FighterPresentation>().Animator, "Hit"),
                "Body damage opens a readable hit reaction without creating an execution window.");
            Require(!_player.Combatant.CanBeExecutedBy(_enemy.Combatant, 3f),
                "Ordinary hit recoil is distinct from posture-break execution state.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.55f;
            _enemy.transform.rotation = Quaternion.LookRotation(
                _player.transform.position - _enemy.transform.position,
                Vector3.up);
            Physics.SyncTransforms();
            health = _player.Combatant.Health;
            _enemy.enabled = true;
            yield return Wait(0.88f);
            Require(_enemy.Combatant.LastEvent.StartsWith("明斩"),
                "Enemy normal strike commits to a readable telegraph before contact.");
            Vector3 committedDirection = _enemy.transform.forward;
            _player.transform.position = _enemy.transform.position - committedDirection * 1.25f;
            Physics.SyncTransforms();
            yield return Wait(0.55f);
            Require(_player.Combatant.Health == health && _enemy.Combatant.LastEvent == "攻击落空",
                "Circling behind the committed enemy strike leaves its forward attack arc and avoids the hit.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.55f;
            _enemy.transform.rotation = Quaternion.LookRotation(
                _player.transform.position - _enemy.transform.position,
                Vector3.up);
            Physics.SyncTransforms();
            _enemy.enabled = true;
            yield return Wait(0.88f);
            Require(_enemy.Combatant.LastEvent.StartsWith("明斩"),
                "Back execution opportunity opens only after the enemy commits to an attack.");
            committedDirection = _enemy.transform.forward;
            _player.transform.SetPositionAndRotation(
                _enemy.transform.position - committedDirection * 1.2f,
                Quaternion.LookRotation(committedDirection, Vector3.up));
            Physics.SyncTransforms();
            Keys(Key.L);
            yield return null;
            Keys();
            Require(_player.Combatant.IsInvulnerable && _player.Combatant.LastEvent.StartsWith("背袭处决"),
                "Heavy input inside the committed attack's rear cone starts an invulnerable back execution instead of a normal heavy.");
            yield return Wait(0.75f);
            Require(_enemy.Combatant.IsDead && _player.Combatant.IsInvulnerable,
                "The two-cut back execution kills the nearby target while invulnerability remains active.");
            Require(_player.Combatant.ReceiveAttack(_enemy.Combatant, new AttackData(30f, 48f, true, "处决干扰")) == CombatOutcome.Dodged,
                "Incoming damage cannot interrupt the player during the execution invulnerability window.");
            yield return Wait(0.9f);
            Require(!_player.Combatant.IsInvulnerable,
                "Execution invulnerability expires after the authored recovery window.");

            yield return ResetFixture();
            _enemy.transform.position = _player.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            _enemy.Combatant.AddPosture(100f);
            yield return null;
            Require(_enemy.Combatant.IsStaggered, "Posture break leaves a punish window.");
            Keys(Key.J);
            yield return Wait(0.25f);
            Keys();
            Require(_enemy.Combatant.IsDead, "Attack input executes a staggered nearby enemy.");
            Require(_player.Combatant.IsInvulnerable,
                "Every execution grants attacker invulnerability, including posture-break executions.");
            yield return Wait(0.2f);
            Require(_enemy.GetComponentInChildren<FighterPresentation>().Animator.GetCurrentAnimatorStateInfo(0).IsName("Death"),
                "Execution drives the death animation.");

            for (int i = 0; i < 10; i++)
            {
                Keys(Key.R);
                yield return Wait(0.08f);
                Keys();
                yield return Wait(0.08f);
                Require(Count<Camera>() == 1 && Count<Light>() == 5 && Count<PrototypeHud>() == 1 &&
                    Count<GreyKilnCourtyard>() == 1 && Count<CombatFeedback>() == 2 &&
                    Count<CombatAudio>() == 2 &&
                    Count<DepthReconstructedBackdrop>() == 1 &&
                    Count<PlayerController>() == 1 && Count<EnemyController>() == 1 && Count<Combatant>() == 2,
                    "Reset " + (i + 1) + ": one camera/courtyard/depth backdrop/HUD, five authored lights and two fighters with audio.");
                Require(UnityEngine.Object.FindFirstObjectByType<PrototypeHud>().HasSkillIconAtlas,
                    "Reset " + (i + 1) + ": three-cell longblade skill icon atlas remains bound to the HUD.");
                GameObject landmarks = GameObject.Find("Blender Grey Kiln Landmarks");
                GameObject floor = GameObject.Find("Courtyard floor");
                GameObject garden = GameObject.Find("West memorial garden");
                GameObject prison = GameObject.Find("Kiln prison entrance");
                GameObject skyline = GameObject.Find("Northern court skyline");
                Require(landmarks != null && landmarks.GetComponentsInChildren<Renderer>(true).Length >= 12,
                    "Reset " + (i + 1) + ": Blender landmark kit remains instantiated.");
                Require(floor != null && floor.transform.lossyScale.x >= 3f && floor.transform.lossyScale.z >= 1.8f,
                    "Reset " + (i + 1) + ": expanded rectangular courtyard remains active.");
                Require(garden != null && garden.GetComponentsInChildren<Renderer>(true).Length <= 28,
                    "Reset " + (i + 1) + ": memorial garden remains batched within its renderer budget.");
                Require(prison != null && prison.GetComponentsInChildren<Renderer>(true).Length <= 22,
                    "Reset " + (i + 1) + ": kiln-prison entrance remains present without adding a second level.");
                Require(skyline != null && skyline.GetComponentsInChildren<Renderer>(true).Length <= 24,
                    "Reset " + (i + 1) + ": Northern court skyline remains within its distant-detail budget.");
            }
#endif
            yield return null;
        }

#if ENABLE_INPUT_SYSTEM
        private static IEnumerator ResetFixture()
        {
            Keys();
            PrototypeBootstrap.Instance.ResetPrototype();
            yield return Wait(0.08f);
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            _enemy = UnityEngine.Object.FindFirstObjectByType<EnemyController>();
            _enemy.enabled = false; // Isolate player input; combat outcomes still use the real Combatant.
        }

        private static void Keys(params Key[] keys)
        {
            bool Has(Key key) => keys.Contains(key);
            Vector2 move = new Vector2(
                (Has(Key.D) || Has(Key.RightArrow) ? 1f : 0f) -
                (Has(Key.A) || Has(Key.LeftArrow) ? 1f : 0f),
                (Has(Key.W) || Has(Key.UpArrow) ? 1f : 0f) -
                (Has(Key.S) || Has(Key.DownArrow) ? 1f : 0f));
            bool guard = Has(Key.K);
            PrototypeInput.SetEditorValidationInput(move,
                attackPressed: Has(Key.J), heavyPressed: Has(Key.L),
                guardPressed: guard, guardHeld: guard,
                dodgePressed: Has(Key.Space), resetPressed: Has(Key.R),
                skillPressed: Has(Key.H), skill2Pressed: Has(Key.U), skill3Pressed: Has(Key.I));
        }
#endif
        private static int Count<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;
        private static bool AnimatorInState(Animator animator, string name) =>
            animator.GetCurrentAnimatorStateInfo(0).IsName(name) ||
            (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(name));

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            do { yield return null; } while (Time.time < until);
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            _checks.Add(label);
        }

        private static void Finish(string result)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            _steps.Clear();
            PrototypeInput.ClearEditorValidationInput();
            File.WriteAllText(ReportPath, result + "\n" + string.Join("\n", _checks.Concat(_warnings)) + "\n");
            if (result == "PASS") Debug.Log("Humanoid Play Mode checks passed. See " + ReportPath + ". Visual and iPhone checks remain separate.");
            else Debug.LogError(result + " See " + ReportPath);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

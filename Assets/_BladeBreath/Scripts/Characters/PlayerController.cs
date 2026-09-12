using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BladeBreath
{
    [RequireComponent(typeof(CharacterController), typeof(Combatant))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5.2f;
        [SerializeField, Min(0f)] private float turnSpeed = 18f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackStartup = 0.11f;
        [SerializeField, Min(0f)] private float attackRecovery = 0.24f;
        [SerializeField, Min(0.1f)] private float attackReach = 1.35f;
        [SerializeField, Min(0.1f)] private float attackRadius = 0.75f;
        [SerializeField, Min(0f)] private float executionDistance = 1.65f;
        [SerializeField, Min(0f)] private float heavyStartup = 0.36f;
        [SerializeField, Min(0f)] private float heavyRecovery = 0.44f;

        [Header("Back Execution")]
        [SerializeField, Range(-1f, -0.1f)] private float rearExecutionDot = -0.45f;
        [SerializeField, Min(0.05f)] private float executionFirstCutStartup = 0.18f;
        [SerializeField, Min(0.05f)] private float executionSecondCutStartup = 0.22f;
        [SerializeField, Min(0f)] private float executionRecovery = 0.14f;
        [SerializeField, Min(0f)] private float executionInvulnerability = 1.05f;

        [Header("Skill - Ying Tui")]
        [SerializeField, Min(0f)] private float skillStartup = 0.26f;
        [SerializeField, Min(0f)] private float skillRecovery = 0.42f;
        [SerializeField, Min(0.1f)] private float skillReach = 1.7f;
        [SerializeField, Min(0.1f)] private float skillRadius = 0.95f;
        [SerializeField, Range(1, 3)] private int skillEdgeCost = 1;
        [SerializeField, Min(0f)] private float skillCooldown = 1.35f;

        [Header("Skill - Hui Feng")]
        [SerializeField, Min(0.05f)] private float huiFengCounterWindow = 0.22f;
        [SerializeField, Min(0.05f)] private float huiFengStrikeStartup = 0.14f;
        [SerializeField, Min(0f)] private float huiFengRecovery = 0.3f;
        [SerializeField, Min(0.1f)] private float huiFengReach = 1.55f;
        [SerializeField, Min(0.1f)] private float huiFengRadius = 0.9f;
        [SerializeField, Range(1, 3)] private int huiFengEdgeCost = 1;
        [SerializeField, Min(0f)] private float huiFengCooldown = 2.6f;

        [Header("Skill - Zhen Lie")]
        [SerializeField, Min(0.05f)] private float zhenLieStartup = 0.46f;
        [SerializeField, Min(0f)] private float zhenLieRecovery = 0.52f;
        [SerializeField, Min(0.1f)] private float zhenLieRadius = 2.15f;
        [SerializeField, Range(1, 3)] private int zhenLieEdgeCost = 2;
        [SerializeField, Min(0f)] private float zhenLieCooldown = 4.8f;

        [Header("Dodge")]
        [SerializeField, Min(0f)] private float dodgeSpeed = 14.5f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.23f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 0.2f;

        private CharacterController _controller;
        private Combatant _combatant;
        private Camera _camera;
        private Transform _swordVisual;
        private Quaternion _swordRestRotation;
        private Vector3 _facing = Vector3.forward;
        private Vector3 _dodgeDirection;
        private float _dodgePresentationDirection;
        private float _dodgeTimer;
        private float _dodgeCooldownTimer;
        private bool _attacking;
        private bool _attackQueued;
        private bool _executing;
        private FighterPresentation _presentation;
        private float _yingTuiCooldownRemaining;
        private float _huiFengCooldownRemaining;
        private float _zhenLieCooldownRemaining;

        private static readonly float[] ComboStartup = { 0.11f, 0.14f, 0.21f };
        private static readonly float[] ComboRecovery = { 0.18f, 0.19f, 0.30f };
        private static readonly float[] ComboDamage = { 12f, 14f, 21f };
        private static readonly float[] ComboPosture = { 14f, 17f, 27f };
        private static readonly float[] ComboLunge = { 0.22f, 0.36f, 0.50f };
        private static readonly float[] ComboSwordAngles = { -68f, 82f, -74f };
        private static readonly string[] ComboLabels =
            { "镇军长刀·入洞提撩", "镇军长刀·腰砍", "镇军长刀·拗步追砍" };
        private static readonly string[] ComboActionNames = { "提撩", "腰砍", "追砍" };
        private static readonly string[] ComboStartupEvents = { "入洞·提撩起势", "腰砍·承势", "拗步·追砍蓄势" };

        public Combatant Combatant => _combatant;
        public bool IsActionBusy => _attacking || _executing || _dodgeTimer > 0f;
        private bool IsActionInterrupted => _combatant.IsDead || _combatant.IsStaggered || _combatant.IsHitReacting;

        public int GetSkillCost(int slot) => slot switch
        {
            0 => skillEdgeCost,
            1 => huiFengEdgeCost,
            2 => zhenLieEdgeCost,
            _ => 0
        };

        public float GetSkillCooldownRemaining(int slot) => slot switch
        {
            0 => _yingTuiCooldownRemaining,
            1 => _huiFengCooldownRemaining,
            2 => _zhenLieCooldownRemaining,
            _ => 0f
        };

        public float GetSkillCooldownRatio(int slot)
        {
            float duration = slot switch
            {
                0 => skillCooldown,
                1 => huiFengCooldown,
                2 => zhenLieCooldown,
                _ => 0f
            };
            return duration <= 0f ? 0f : Mathf.Clamp01(GetSkillCooldownRemaining(slot) / duration);
        }

        public bool CanUseSkill(int slot) =>
            _combatant != null && !_combatant.IsDead && !_combatant.IsStaggered && !_combatant.IsHitReacting &&
            !IsActionBusy && GetSkillCooldownRemaining(slot) <= 0f && _combatant.Edge >= GetSkillCost(slot);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _combatant = GetComponent<Combatant>();
            _camera = Camera.main;
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _dodgeCooldownTimer = Mathf.Max(0f, _dodgeCooldownTimer - Time.deltaTime);
            _yingTuiCooldownRemaining = Mathf.Max(0f, _yingTuiCooldownRemaining - Time.deltaTime);
            _huiFengCooldownRemaining = Mathf.Max(0f, _huiFengCooldownRemaining - Time.deltaTime);
            _zhenLieCooldownRemaining = Mathf.Max(0f, _zhenLieCooldownRemaining - Time.deltaTime);

            if (_combatant.IsDead)
            {
                _combatant.EndGuard();
                _dodgeTimer = 0f;
                _presentation?.EndAction();
                return;
            }

            if (_combatant.IsStaggered)
            {
                _combatant.EndGuard();
                _dodgeTimer = 0f;
                _presentation?.EndAction();
                return;
            }

            if (_combatant.IsHitReacting)
            {
                _combatant.EndGuard();
                _dodgeTimer = 0f;
                _presentation?.EndAction();
                return;
            }

            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= Time.deltaTime;
                _controller.Move(_dodgeDirection * dodgeSpeed * Time.deltaTime);
                _presentation?.SampleDodge(1f - _dodgeTimer / dodgeDuration, _dodgePresentationDirection);
                return;
            }

            if (_executing)
            {
                return;
            }

            if (!_attacking) _presentation?.EndAction();

            HandleGuard();

            if (PrototypeInput.DodgePressed && !_attacking && _dodgeCooldownTimer <= 0f)
            {
                BeginDodge();
                return;
            }

            if (PrototypeInput.Skill1Pressed && !_combatant.IsGuarding && !_attacking)
            {
                TryStartSkill(0);
                return;
            }

            if (PrototypeInput.Skill2Pressed && !_attacking)
            {
                TryStartSkill(1);
                return;
            }

            if (PrototypeInput.Skill3Pressed && !_combatant.IsGuarding && !_attacking)
            {
                TryStartSkill(2);
                return;
            }

            if (PrototypeInput.AttackPressed && !_combatant.IsGuarding)
            {
                if (_attacking) _attackQueued = true;
                else StartCoroutine(AttackRoutine());
            }

            if (PrototypeInput.HeavyAttackPressed && !_combatant.IsGuarding && !_attacking)
            {
                if (TryGetBackExecutionTarget(out Combatant executionTarget))
                    StartCoroutine(BackExecutionRoutine(executionTarget));
                else
                    StartCoroutine(HeavyAttackRoutine());
            }

            Move(PrototypeInput.Move);
        }

        public void ConfigureSword(Transform swordVisual)
        {
            _swordVisual = swordVisual;
            if (_swordVisual != null)
            {
                _swordRestRotation = _swordVisual.localRotation;
            }
        }

        public void ConfigurePresentation(FighterPresentation presentation)
        {
            _presentation = presentation;
        }

        private void HandleGuard()
        {
            if (PrototypeInput.GuardPressed && !_attacking)
            {
                _combatant.BeginGuard();
            }

            if (!PrototypeInput.GuardHeld)
            {
                _combatant.EndGuard();
            }
        }

        private void Move(Vector2 input)
        {
            Vector3 direction = CameraRelativeDirection(input);
            float movementScale = _attacking ? 0.3f : (_combatant.IsGuarding ? 0.48f : 1f);
            _controller.Move(direction * (moveSpeed * movementScale * Time.deltaTime));

            if (direction.sqrMagnitude > 0.001f)
            {
                _facing = direction.normalized;
                Quaternion targetRotation = Quaternion.LookRotation(_facing, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }
        }

        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.001f)
            {
                return Vector3.zero;
            }

            if (_camera == null)
            {
                return new Vector3(input.x, 0f, input.y).normalized;
            }

            Vector3 forward = _camera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = _camera.transform.right;
            right.y = 0f;
            right.Normalize();

            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private void BeginDodge()
        {
            _combatant.EndGuard();

            Vector3 priorFacing = _facing.sqrMagnitude > 0.001f ? _facing.normalized : transform.forward;
            Vector3 requestedDirection = CameraRelativeDirection(PrototypeInput.Move);
            _dodgeDirection = requestedDirection.sqrMagnitude > 0.001f
                ? requestedDirection.normalized
                : priorFacing;

            if (_dodgeDirection.sqrMagnitude <= 0.001f)
            {
                _dodgeDirection = transform.forward;
            }

            Vector3 right = Vector3.Cross(Vector3.up, priorFacing).normalized;
            float lateral = Vector3.Dot(_dodgeDirection, right);
            float forward = Vector3.Dot(_dodgeDirection, priorFacing);
            _dodgePresentationDirection = Mathf.Abs(lateral) > Mathf.Abs(forward) * 0.75f
                ? Mathf.Sign(lateral)
                : 0f;
            // Side dodges keep the fighter facing the threat so the lateral step reads clearly.
            // Forward dodges can redirect the fighter; backward dodges preserve the guard line.
            if (Mathf.Approximately(_dodgePresentationDirection, 0f) && forward >= -0.25f)
            {
                _facing = _dodgeDirection;
                transform.rotation = Quaternion.LookRotation(_facing, Vector3.up);
            }
            _dodgeTimer = dodgeDuration;
            _dodgeCooldownTimer = dodgeDuration + dodgeCooldown;
            _combatant.SetInvulnerable(dodgeDuration * 0.82f);
            _combatant.SetEvent("闪身");
            _presentation?.SampleDodge(0f, _dodgePresentationDirection);
        }

        private IEnumerator AttackRoutine()
        {
            _attacking = true;
            _combatant.EndGuard();
            int comboStep = 0;
            while (comboStep < ComboDamage.Length)
            {
                _attackQueued = false;
                yield return AttackStep(comboStep);
                if (IsActionInterrupted)
                    break;
                if (!_attackQueued || comboStep >= ComboDamage.Length - 1)
                    break;
                comboStep++;
            }
            FinishAttack();
        }

        private IEnumerator AttackStep(int comboStep)
        {
            float startup = comboStep == 0 ? attackStartup : ComboStartup[comboStep];
            float recovery = comboStep == 0 ? Mathf.Min(attackRecovery, ComboRecovery[0]) : ComboRecovery[comboStep];
            _combatant.SetEvent(ComboStartupEvents[comboStep]);

            float elapsed = 0f;
            bool attackWindowOpen = false;
            float appliedLunge = 0f;
            float lungeDistance = ComboLunge[comboStep];
            while (elapsed < startup)
            {
                if (IsActionInterrupted) yield break;
                float progress = elapsed / Mathf.Max(startup, 0.001f);
                _presentation?.SampleAttack(progress, false, comboStep + 1);
                if (!attackWindowOpen && progress >= 0.58f)
                {
                    _combatant.BeginAttackWindow();
                    attackWindowOpen = true;
                }
                ApplyLunge(progress, 0.42f, lungeDistance, ref appliedLunge);
                AnimateSword(progress, ComboSwordAngles[comboStep]);
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!IsActionInterrupted)
            {
                _presentation?.SampleAttack(0f, true, comboStep + 1);
                ResolveAttack(comboStep);
            }

            elapsed = 0f;
            while (elapsed < recovery)
            {
                if (IsActionInterrupted) yield break;
                float progress = elapsed / Mathf.Max(recovery, 0.001f);
                _presentation?.SampleAttack(progress, true, comboStep + 1);
                if (progress >= 0.18f) _combatant.EndAttackWindow();
                AnimateSword(1f - progress, -ComboSwordAngles[comboStep]);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _combatant.EndAttackWindow();
        }

        private IEnumerator HeavyAttackRoutine()
        {
            _attacking = true;
            _attackQueued = false;
            _combatant.EndGuard();
            _combatant.SetEvent("埋头·镇落蓄势");

            float elapsed = 0f;
            bool attackWindowOpen = false;
            float appliedLunge = 0f;
            while (elapsed < heavyStartup)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }

                float progress = elapsed / Mathf.Max(heavyStartup, 0.001f);
                _presentation?.SampleHeavyAttack(progress, false);
                if (!attackWindowOpen && progress >= 0.66f)
                {
                    _combatant.BeginAttackWindow();
                    attackWindowOpen = true;
                }
                ApplyLunge(progress, 0.5f, 0.52f, ref appliedLunge);
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!IsActionInterrupted)
            {
                _presentation?.SampleHeavyAttack(0f, true);
                ResolveHeavyAttack();
            }

            elapsed = 0f;
            while (elapsed < heavyRecovery)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }

                float progress = elapsed / Mathf.Max(heavyRecovery, 0.001f);
                _presentation?.SampleHeavyAttack(progress, true);
                if (progress >= 0.22f) _combatant.EndAttackWindow();
                yield return null;
                elapsed += Time.deltaTime;
            }
            FinishAttack();
        }

        private IEnumerator BackExecutionRoutine(Combatant target)
        {
            _attacking = true;
            _executing = true;
            _attackQueued = false;
            _combatant.EndGuard();
            _combatant.SetInvulnerable(executionInvulnerability);
            FaceAndCloseForExecution(target);
            target.ForceStagger(
                executionFirstCutStartup + executionSecondCutStartup + executionRecovery + 0.35f,
                "背门已失");

            _combatant.SetEvent("背袭处决·一断起势");
            float elapsed = 0f;
            while (elapsed < executionFirstCutStartup)
            {
                if (_combatant.IsDead || target == null || target.IsDead)
                {
                    FinishAttack();
                    yield break;
                }
                _presentation?.SampleAttack(elapsed / executionFirstCutStartup, false, 1);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _presentation?.SampleAttack(0f, true, 1);
            target.SetEvent("背袭·一断");

            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                _presentation?.SampleAttack(elapsed / 0.1f, true, 1);
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (target == null || target.IsDead)
            {
                FinishAttack();
                yield break;
            }

            _combatant.SetEvent("背袭处决·二断起势");
            elapsed = 0f;
            while (elapsed < executionSecondCutStartup)
            {
                if (_combatant.IsDead || target == null || target.IsDead)
                {
                    FinishAttack();
                    yield break;
                }
                _presentation?.SampleAttack(elapsed / executionSecondCutStartup, false, 2);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _presentation?.SampleAttack(0f, true, 2);
            target.SetEvent("背袭·二断");

            elapsed = 0f;
            while (elapsed < executionRecovery)
            {
                _presentation?.SampleAttack(elapsed / Mathf.Max(executionRecovery, 0.001f), true, 2);
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (target != null && !target.IsDead)
            {
                target.Execute(_combatant);
                _combatant.SetEvent("背袭处决完成");
            }
            FinishAttack();
        }

        private bool TryGetBackExecutionTarget(out Combatant executionTarget)
        {
            executionTarget = null;
            float nearestDistance = float.MaxValue;
            Collider[] hits = Physics.OverlapSphere(
                transform.position + Vector3.up * 0.9f,
                executionDistance,
                ~0,
                QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                Combatant candidate = hit.GetComponentInParent<Combatant>();
                if (candidate == null || candidate == _combatant || candidate.IsDead) continue;
                EnemyController enemy = candidate.GetComponent<EnemyController>();
                if (enemy == null || !enemy.IsAttackCommitted) continue;

                Vector3 fromTarget = transform.position - candidate.transform.position;
                fromTarget.y = 0f;
                float distance = fromTarget.magnitude;
                if (distance <= 0.001f || distance > executionDistance) continue;
                if (Vector3.Dot(candidate.transform.forward, fromTarget / distance) > rearExecutionDot) continue;
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                executionTarget = candidate;
            }

            return executionTarget != null;
        }

        private void FaceAndCloseForExecution(Combatant target)
        {
            Vector3 towardTarget = target.transform.position - transform.position;
            towardTarget.y = 0f;
            if (towardTarget.sqrMagnitude > 0.001f)
            {
                _facing = towardTarget.normalized;
                transform.rotation = Quaternion.LookRotation(_facing, Vector3.up);
            }

            Vector3 desiredPosition = target.transform.position - target.transform.forward * 0.92f;
            desiredPosition.y = transform.position.y;
            Vector3 correction = desiredPosition - transform.position;
            correction.y = 0f;
            _controller.Move(Vector3.ClampMagnitude(correction, 0.55f));
        }

        private IEnumerator SkillAttackRoutine()
        {
            _attacking = true;
            _attackQueued = false;
            _combatant.EndGuard();
            _combatant.SetEvent("迎推·借线");

            float elapsed = 0f;
            float appliedLunge = 0f;
            bool attackWindowOpen = false;
            while (elapsed < skillStartup)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }

                float progress = elapsed / Mathf.Max(skillStartup, 0.001f);
                _presentation?.SampleSkillAttack(progress, false);
                if (!attackWindowOpen && progress >= 0.58f)
                {
                    _combatant.BeginAttackWindow();
                    attackWindowOpen = true;
                }
                ApplyLunge(progress, 0.38f, 0.72f, ref appliedLunge);
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!IsActionInterrupted)
            {
                _presentation?.SampleSkillAttack(0f, true);
                ResolveSkillAttack();
            }

            elapsed = 0f;
            while (elapsed < skillRecovery)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }

                float progress = elapsed / Mathf.Max(skillRecovery, 0.001f);
                _presentation?.SampleSkillAttack(progress, true);
                if (progress >= 0.26f) _combatant.EndAttackWindow();
                yield return null;
                elapsed += Time.deltaTime;
            }
            FinishAttack();
        }

        private IEnumerator HuiFengRoutine()
        {
            _attacking = true;
            _attackQueued = false;
            _combatant.EndGuard();
            _combatant.BeginGuard(huiFengCounterWindow);
            _combatant.SetEvent("回锋·纳刃候隙");

            float elapsed = 0f;
            while (elapsed < huiFengCounterWindow)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }
                _presentation?.SampleSkillAttack(
                    (elapsed / Mathf.Max(huiFengCounterWindow, 0.001f)) * 0.58f, false, 2);
                yield return null;
                elapsed += Time.deltaTime;
            }

            _combatant.EndGuard();
            _combatant.SetEvent("回锋·返刃起势");
            elapsed = 0f;
            bool attackWindowOpen = false;
            while (elapsed < huiFengStrikeStartup)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }
                float progress = elapsed / Mathf.Max(huiFengStrikeStartup, 0.001f);
                _presentation?.SampleSkillAttack(Mathf.Lerp(0.58f, 1f, progress), false, 2);
                if (!attackWindowOpen && progress >= 0.42f)
                {
                    _combatant.BeginAttackWindow();
                    attackWindowOpen = true;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!IsActionInterrupted)
            {
                _presentation?.SampleSkillAttack(0f, true, 2);
                ResolveHuiFeng();
            }

            elapsed = 0f;
            while (elapsed < huiFengRecovery)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }
                float progress = elapsed / Mathf.Max(huiFengRecovery, 0.001f);
                _presentation?.SampleSkillAttack(progress, true, 2);
                if (progress >= 0.24f) _combatant.EndAttackWindow();
                yield return null;
                elapsed += Time.deltaTime;
            }
            FinishAttack();
        }

        private IEnumerator ZhenLieRoutine()
        {
            _attacking = true;
            _attackQueued = false;
            _combatant.EndGuard();
            _combatant.SetEvent("震烈·回环蓄势");

            float elapsed = 0f;
            bool attackWindowOpen = false;
            while (elapsed < zhenLieStartup)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }
                float progress = elapsed / Mathf.Max(zhenLieStartup, 0.001f);
                _presentation?.SampleSkillAttack(progress, false, 3);
                if (!attackWindowOpen && progress >= 0.68f)
                {
                    _combatant.BeginAttackWindow();
                    attackWindowOpen = true;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!IsActionInterrupted)
            {
                _presentation?.SampleSkillAttack(0f, true, 3);
                ResolveZhenLie();
            }

            elapsed = 0f;
            while (elapsed < zhenLieRecovery)
            {
                if (IsActionInterrupted)
                {
                    FinishAttack();
                    yield break;
                }
                float progress = elapsed / Mathf.Max(zhenLieRecovery, 0.001f);
                _presentation?.SampleSkillAttack(progress, true, 3);
                if (progress >= 0.34f) _combatant.EndAttackWindow();
                yield return null;
                elapsed += Time.deltaTime;
            }
            FinishAttack();
        }

        private void TryStartSkill(int slot)
        {
            int cost = GetSkillCost(slot);
            float cooldown = GetSkillCooldownRemaining(slot);
            string name = slot == 0 ? "迎推" : slot == 1 ? "回锋" : "震烈";
            if (cooldown > 0f)
            {
                _combatant.SetEvent($"{name}尚需 {cooldown:0.0}s");
                return;
            }
            if (!_combatant.TrySpendEdge(cost))
            {
                _combatant.SetEvent($"{name}需要 {cost} 锋意");
                return;
            }

            switch (slot)
            {
                case 0:
                    _yingTuiCooldownRemaining = skillCooldown;
                    StartCoroutine(SkillAttackRoutine());
                    break;
                case 1:
                    _huiFengCooldownRemaining = huiFengCooldown;
                    StartCoroutine(HuiFengRoutine());
                    break;
                case 2:
                    _zhenLieCooldownRemaining = zhenLieCooldown;
                    StartCoroutine(ZhenLieRoutine());
                    break;
            }
        }

        private void FinishAttack()
        {
            _combatant?.EndAttackWindow();
            _presentation?.EndAction();
            RestoreSword();
            _attacking = false;
            _attackQueued = false;
            _executing = false;
        }

        private void ResolveAttack(int comboStep)
        {
            Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * attackReach;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Combatant> resolved = new HashSet<Combatant>();

            foreach (Collider hit in hits)
            {
                Combatant target = hit.GetComponentInParent<Combatant>();
                if (target == null || target == _combatant || target.IsDead || !resolved.Add(target))
                {
                    continue;
                }

                if (target.CanBeExecutedBy(_combatant, executionDistance))
                {
                    target.Execute(_combatant);
                    return;
                }

                CombatOutcome outcome = target.ReceiveAttack(
                    _combatant,
                    new AttackData(ComboDamage[comboStep], ComboPosture[comboStep], false, ComboLabels[comboStep]));
                _presentation?.ReportWeaponContact(target, outcome);

                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "攻击被弹开"
                    : outcome == CombatOutcome.Clashed ? "拼刀"
                    : $"{ComboActionNames[comboStep]}命中");
                return;
            }

            _combatant.SetEvent($"{ComboActionNames[comboStep]}挥空");
        }

        private void ResolveHeavyAttack()
        {
            Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * attackReach;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Combatant> resolved = new HashSet<Combatant>();
            foreach (Collider hit in hits)
            {
                Combatant target = hit.GetComponentInParent<Combatant>();
                if (target == null || target == _combatant || target.IsDead || !resolved.Add(target)) continue;
                if (target.CanBeExecutedBy(_combatant, executionDistance))
                {
                    target.Execute(_combatant);
                    return;
                }

                CombatOutcome outcome = target.ReceiveAttack(_combatant,
                    new AttackData(30f, 44f, false, "埋头·镇落"));
                _presentation?.ReportWeaponContact(target, outcome);
                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "重斩被弹开"
                    : outcome == CombatOutcome.Clashed ? "重刀拼刃"
                    : outcome == CombatOutcome.Guarded ? "重刀压架"
                    : "埋头·镇落命中");
                return;
            }
            _combatant.SetEvent("埋头·镇落挥空");
        }

        private void ResolveSkillAttack()
        {
            Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * skillReach;
            Collider[] hits = Physics.OverlapSphere(center, skillRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Combatant> resolved = new HashSet<Combatant>();
            foreach (Collider hit in hits)
            {
                Combatant target = hit.GetComponentInParent<Combatant>();
                if (target == null || target == _combatant || target.IsDead || !resolved.Add(target)) continue;

                CombatOutcome outcome = target.ReceiveAttack(_combatant,
                    new AttackData(18f, 52f, false, "迎推·断势"));
                _presentation?.ReportWeaponContact(target, outcome);
                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "迎推被截"
                    : outcome == CombatOutcome.Clashed ? "迎推抗衡"
                    : outcome == CombatOutcome.Guarded ? "迎推压架"
                    : "迎推·断势命中");
                return;
            }
            _combatant.SetEvent("迎推落空");
        }

        private void ResolveHuiFeng()
        {
            Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * huiFengReach;
            Collider[] hits = Physics.OverlapSphere(center, huiFengRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Combatant> resolved = new HashSet<Combatant>();
            foreach (Collider hit in hits)
            {
                Combatant target = hit.GetComponentInParent<Combatant>();
                if (target == null || target == _combatant || target.IsDead || !resolved.Add(target)) continue;

                CombatOutcome outcome = target.ReceiveAttack(_combatant,
                    new AttackData(16f, 38f, false, "回锋·守隙"));
                _presentation?.ReportWeaponContact(target, outcome);
                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "回锋被截"
                    : outcome == CombatOutcome.Clashed ? "回锋抗衡"
                    : outcome == CombatOutcome.Guarded ? "回锋叩架"
                    : "回锋·守隙命中");
                return;
            }
            _combatant.SetEvent("回锋落空");
        }

        private void ResolveZhenLie()
        {
            Vector3 center = transform.position + Vector3.up * 0.9f;
            Collider[] hits = Physics.OverlapSphere(center, zhenLieRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Combatant> resolved = new HashSet<Combatant>();
            foreach (Collider hit in hits)
            {
                Combatant target = hit.GetComponentInParent<Combatant>();
                if (target == null || target == _combatant || target.IsDead || !resolved.Add(target)) continue;

                CombatOutcome outcome = target.ReceiveAttack(_combatant,
                    new AttackData(34f, 58f, false, "震烈·回环"));
                _presentation?.ReportWeaponContact(target, outcome);
                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "震烈被截"
                    : outcome == CombatOutcome.Clashed ? "震烈抗衡"
                    : outcome == CombatOutcome.Guarded ? "震烈压架"
                    : "震烈·回环命中");
                return;
            }
            _combatant.SetEvent("震烈落空");
        }

        private void ApplyLunge(float progress, float startAt, float distance, ref float appliedDistance)
        {
            float phase = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startAt, 1f, progress));
            float desiredDistance = distance * phase;
            float delta = Mathf.Max(0f, desiredDistance - appliedDistance);
            if (delta > 0f)
            {
                _controller.Move(transform.forward * delta);
                appliedDistance = desiredDistance;
            }
        }

        private void AnimateSword(float normalized, float angle)
        {
            if (_swordVisual == null)
            {
                return;
            }

            _swordVisual.localRotation = _swordRestRotation *
                                         Quaternion.Euler(0f, Mathf.Lerp(0f, angle, normalized), 0f);
        }

        private void RestoreSword()
        {
            if (_swordVisual != null)
            {
                _swordVisual.localRotation = _swordRestRotation;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            FinishAttack();
            _dodgeTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * attackReach;
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}

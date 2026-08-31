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

        [Header("Dodge")]
        [SerializeField, Min(0f)] private float dodgeSpeed = 13f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.18f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 0.18f;

        private CharacterController _controller;
        private Combatant _combatant;
        private Camera _camera;
        private Transform _swordVisual;
        private Quaternion _swordRestRotation;
        private Vector3 _facing = Vector3.forward;
        private Vector3 _dodgeDirection;
        private float _dodgeTimer;
        private float _dodgeCooldownTimer;
        private bool _attacking;

        public Combatant Combatant => _combatant;

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

            if (_combatant.IsDead)
            {
                _combatant.EndGuard();
                return;
            }

            if (_combatant.IsStaggered)
            {
                _combatant.EndGuard();
                return;
            }

            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= Time.deltaTime;
                _controller.Move(_dodgeDirection * dodgeSpeed * Time.deltaTime);
                return;
            }

            HandleGuard();

            if (PrototypeInput.DodgePressed && !_attacking && _dodgeCooldownTimer <= 0f)
            {
                BeginDodge();
                return;
            }

            if (PrototypeInput.AttackPressed && !_attacking && !_combatant.IsGuarding)
            {
                StartCoroutine(AttackRoutine());
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

            Vector3 requestedDirection = CameraRelativeDirection(PrototypeInput.Move);
            _dodgeDirection = requestedDirection.sqrMagnitude > 0.001f
                ? requestedDirection.normalized
                : _facing.normalized;

            if (_dodgeDirection.sqrMagnitude <= 0.001f)
            {
                _dodgeDirection = transform.forward;
            }

            _facing = _dodgeDirection;
            transform.rotation = Quaternion.LookRotation(_facing, Vector3.up);
            _dodgeTimer = dodgeDuration;
            _dodgeCooldownTimer = dodgeDuration + dodgeCooldown;
            _combatant.SetInvulnerable(dodgeDuration * 0.82f);
            _combatant.SetEvent("闪身");
        }

        private IEnumerator AttackRoutine()
        {
            _attacking = true;
            _combatant.EndGuard();
            _combatant.SetEvent("长刀起势");

            float elapsed = 0f;
            while (elapsed < attackStartup)
            {
                elapsed += Time.deltaTime;
                AnimateSword(elapsed / Mathf.Max(attackStartup, 0.001f), -55f);
                yield return null;
            }

            if (!_combatant.IsDead && !_combatant.IsStaggered)
            {
                ResolveAttack();
            }

            elapsed = 0f;
            while (elapsed < attackRecovery)
            {
                elapsed += Time.deltaTime;
                AnimateSword(1f - elapsed / Mathf.Max(attackRecovery, 0.001f), 88f);
                yield return null;
            }

            RestoreSword();
            _attacking = false;
        }

        private void ResolveAttack()
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
                    new AttackData(18f, 28f, false, "镇军长刀"));

                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "攻击被弹开" : "长刀命中");
                return;
            }

            _combatant.SetEvent("挥空");
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
            RestoreSword();
            _attacking = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * attackReach;
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}

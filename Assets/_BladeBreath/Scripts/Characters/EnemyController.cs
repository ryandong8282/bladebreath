using System.Collections;
using UnityEngine;

namespace BladeBreath
{
    [RequireComponent(typeof(Combatant), typeof(CapsuleCollider))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2.25f;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.8f;
        [SerializeField, Min(0f)] private float normalTelegraph = 0.48f;
        [SerializeField, Min(0f)] private float delayedTelegraph = 0.82f;
        [SerializeField, Min(0f)] private float recovery = 0.48f;

        private Combatant _combatant;
        private Combatant _target;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propertyBlock;
        private Color _baseColor = new Color(0.48f, 0.18f, 0.14f);
        private float _nextAttackTime;
        private int _attackIndex;
        private bool _attacking;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public Combatant Combatant => _combatant;

        private void Awake()
        {
            _combatant = GetComponent<Combatant>();
            _renderers = GetComponentsInChildren<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_target == null || _target.IsDead || _combatant.IsDead || _combatant.IsStaggered || _attacking)
            {
                return;
            }

            Vector3 delta = _target.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (delta.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-12f * Time.deltaTime));
            }

            if (distance > attackDistance)
            {
                transform.position += delta.normalized * (moveSpeed * Time.deltaTime);
                _combatant.SetEvent("逼近");
                return;
            }

            if (Time.time >= _nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        public void Configure(Combatant target, Color baseColor)
        {
            _target = target;
            _baseColor = baseColor;
            SetColor(_baseColor);
            _nextAttackTime = Time.time + 0.9f;
        }

        private IEnumerator AttackRoutine()
        {
            _attacking = true;
            _attackIndex++;

            bool delayedHeavy = _attackIndex % 4 == 0;
            float telegraph = delayedHeavy ? delayedTelegraph : normalTelegraph;
            Color warning = delayedHeavy
                ? new Color(1f, 0.18f, 0.08f)
                : new Color(1f, 0.62f, 0.15f);

            _combatant.SetEvent(delayedHeavy ? "裂地：不可格挡" : "明斩：可格挡");
            SetColor(warning);

            float elapsed = 0f;
            while (elapsed < telegraph)
            {
                if (_combatant.IsDead || _combatant.IsStaggered || _target == null || _target.IsDead)
                {
                    AbortAttack();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 delta = _target.transform.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= attackDistance + 0.45f)
            {
                AttackData attack = delayedHeavy
                    ? new AttackData(30f, 48f, true, "裂地")
                    : new AttackData(13f, 24f, false, "明斩");

                CombatOutcome outcome = _target.ReceiveAttack(_combatant, attack);
                _combatant.SetEvent(outcome == CombatOutcome.Parried ? "被弹反" : "出刀");
            }
            else
            {
                _combatant.SetEvent("攻击落空");
            }

            SetColor(Color.white);
            yield return new WaitForSeconds(0.08f);
            SetColor(_baseColor);
            yield return new WaitForSeconds(recovery);

            _nextAttackTime = Time.time + 0.32f;
            _attacking = false;
        }

        private void AbortAttack()
        {
            SetColor(_baseColor);
            _nextAttackTime = Time.time + 0.4f;
            _attacking = false;
        }

        private void SetColor(Color color)
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (Renderer rendererComponent in _renderers)
            {
                if (rendererComponent == null)
                {
                    continue;
                }

                rendererComponent.GetPropertyBlock(_propertyBlock);
                Material shared = rendererComponent.sharedMaterial;
                if (shared != null && shared.HasProperty(BaseColorId))
                {
                    _propertyBlock.SetColor(BaseColorId, color);
                }

                if (shared != null && shared.HasProperty(ColorId))
                {
                    _propertyBlock.SetColor(ColorId, color);
                }

                rendererComponent.SetPropertyBlock(_propertyBlock);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _attacking = false;
        }
    }
}

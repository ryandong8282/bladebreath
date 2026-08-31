using System;
using UnityEngine;

namespace BladeBreath
{
    public enum CombatOutcome
    {
        None,
        Hit,
        Guarded,
        Parried,
        Dodged,
        PostureBroken,
        Executed,
        Killed
    }

    public readonly struct AttackData
    {
        public AttackData(float damage, float postureDamage, bool unblockable = false, string label = "斩击")
        {
            Damage = Mathf.Max(0f, damage);
            PostureDamage = Mathf.Max(0f, postureDamage);
            Unblockable = unblockable;
            Label = string.IsNullOrWhiteSpace(label) ? "斩击" : label;
        }

        public float Damage { get; }
        public float PostureDamage { get; }
        public bool Unblockable { get; }
        public string Label { get; }
    }

    [DisallowMultipleComponent]
    public sealed class Combatant : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(1f)] private float maxPosture = 100f;
        [SerializeField, Min(0f)] private float postureRecoveryPerSecond = 13f;
        [SerializeField, Min(0f)] private float postureRecoveryDelay = 1.25f;

        private float _parryTimer;
        private float _invulnerabilityTimer;
        private float _staggerTimer;
        private float _postureRecoveryDelayTimer;
        private Vector3 _initialScale;
        private Quaternion _initialRotation;

        public event Action<Combatant> Changed;

        public bool IsPlayer { get; private set; }
        public float Health { get; private set; }
        public float Posture { get; private set; }
        public float MaxHealth => maxHealth;
        public float MaxPosture => maxPosture;
        public float HealthRatio => maxHealth <= 0f ? 0f : Health / maxHealth;
        public float PostureRatio => maxPosture <= 0f ? 0f : Posture / maxPosture;
        public bool IsGuarding { get; private set; }
        public bool IsParrying => _parryTimer > 0f;
        public bool IsInvulnerable => _invulnerabilityTimer > 0f;
        public bool IsStaggered => _staggerTimer > 0f;
        public bool IsDead { get; private set; }
        public string LastEvent { get; private set; } = "准备交锋";

        private void Awake()
        {
            _initialScale = transform.localScale;
            _initialRotation = transform.localRotation;
            ResetState();
        }

        private void Update()
        {
            if (_parryTimer > 0f)
            {
                _parryTimer -= Time.deltaTime;
            }

            if (_invulnerabilityTimer > 0f)
            {
                _invulnerabilityTimer -= Time.deltaTime;
            }

            if (_staggerTimer > 0f)
            {
                _staggerTimer -= Time.deltaTime;
                if (_staggerTimer <= 0f && !IsDead)
                {
                    transform.localRotation = _initialRotation;
                    SetEvent("重新站稳");
                }
            }

            if (IsDead || IsGuarding || IsStaggered || Posture <= 0f)
            {
                return;
            }

            if (_postureRecoveryDelayTimer > 0f)
            {
                _postureRecoveryDelayTimer -= Time.deltaTime;
                return;
            }

            float previous = Posture;
            Posture = Mathf.Max(0f, Posture - postureRecoveryPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(previous, Posture))
            {
                NotifyChanged();
            }
        }

        public void Configure(bool isPlayer, float health = 100f, float posture = 100f)
        {
            IsPlayer = isPlayer;
            maxHealth = Mathf.Max(1f, health);
            maxPosture = Mathf.Max(1f, posture);
            ResetState();
        }

        public void ResetState()
        {
            Health = maxHealth;
            Posture = 0f;
            IsGuarding = false;
            IsDead = false;
            _parryTimer = 0f;
            _invulnerabilityTimer = 0f;
            _staggerTimer = 0f;
            _postureRecoveryDelayTimer = 0f;
            LastEvent = "准备交锋";
            transform.localScale = _initialScale == Vector3.zero ? Vector3.one : _initialScale;
            transform.localRotation = _initialRotation;
            NotifyChanged();
        }

        public void BeginGuard(float parryWindowSeconds = 0.13f)
        {
            if (IsDead || IsStaggered)
            {
                return;
            }

            IsGuarding = true;
            _parryTimer = Mathf.Max(_parryTimer, parryWindowSeconds);
            SetEvent("架刀");
        }

        public void EndGuard()
        {
            if (!IsGuarding)
            {
                return;
            }

            IsGuarding = false;
            _parryTimer = 0f;
            NotifyChanged();
        }

        public void SetInvulnerable(float seconds)
        {
            if (IsDead)
            {
                return;
            }

            _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer, seconds);
        }

        public CombatOutcome ReceiveAttack(Combatant attacker, AttackData attack)
        {
            if (IsDead)
            {
                return CombatOutcome.None;
            }

            if (IsInvulnerable)
            {
                SetEvent("闪过");
                attacker?.SetEvent("攻击落空");
                return CombatOutcome.Dodged;
            }

            _postureRecoveryDelayTimer = postureRecoveryDelay;

            if (!attack.Unblockable && IsParrying)
            {
                IsGuarding = false;
                _parryTimer = 0f;
                SetEvent($"弹开「{attack.Label}」");
                if (attacker != null)
                {
                    attacker.AddPosture(attack.PostureDamage * 1.45f, "被弹反");
                    attacker.ForceStagger(0.42f, "刀势被截");
                }

                NotifyChanged();
                return CombatOutcome.Parried;
            }

            if (!attack.Unblockable && IsGuarding)
            {
                SetEvent($"挡住「{attack.Label}」");
                bool broken = AddPosture(attack.PostureDamage, "承受架势压力");
                return broken ? CombatOutcome.PostureBroken : CombatOutcome.Guarded;
            }

            Health = Mathf.Max(0f, Health - attack.Damage);
            SetEvent(attack.Unblockable ? $"被「{attack.Label}」击中" : $"中刀：{attack.Label}");

            if (Health <= 0f)
            {
                Die();
                return CombatOutcome.Killed;
            }

            bool postureBroken = AddPosture(attack.PostureDamage * 0.45f);
            NotifyChanged();
            return postureBroken ? CombatOutcome.PostureBroken : CombatOutcome.Hit;
        }

        public bool AddPosture(float amount, string eventText = "")
        {
            if (IsDead || amount <= 0f)
            {
                return false;
            }

            Posture = Mathf.Clamp(Posture + amount, 0f, maxPosture);
            _postureRecoveryDelayTimer = postureRecoveryDelay;
            if (!string.IsNullOrWhiteSpace(eventText))
            {
                LastEvent = eventText;
            }

            if (Posture >= maxPosture)
            {
                ForceStagger(1.35f, "架势崩溃");
                return true;
            }

            NotifyChanged();
            return false;
        }

        public void ForceStagger(float seconds = 1f, string reason = "失衡")
        {
            if (IsDead)
            {
                return;
            }

            IsGuarding = false;
            _parryTimer = 0f;
            _staggerTimer = Mathf.Max(_staggerTimer, seconds);
            LastEvent = reason;
            transform.localRotation = _initialRotation * Quaternion.Euler(0f, 0f, IsPlayer ? 8f : -11f);
            NotifyChanged();
        }

        public bool CanBeExecutedBy(Combatant attacker, float maximumDistance)
        {
            if (attacker == null || IsDead || !IsStaggered)
            {
                return false;
            }

            return Vector3.Distance(transform.position, attacker.transform.position) <= maximumDistance;
        }

        public void Execute(Combatant attacker)
        {
            if (IsDead)
            {
                return;
            }

            Health = 0f;
            LastEvent = "被处决";
            attacker?.SetEvent("处决");
            Die();
        }

        public void SetEvent(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                LastEvent = message;
            }

            NotifyChanged();
        }

        private void Die()
        {
            IsDead = true;
            IsGuarding = false;
            _parryTimer = 0f;
            _invulnerabilityTimer = 0f;
            _staggerTimer = 0f;
            LastEvent = IsPlayer ? "无铭者倒下" : "执刃者倒下";
            transform.localScale = new Vector3(_initialScale.x, _initialScale.y * 0.28f, _initialScale.z);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}

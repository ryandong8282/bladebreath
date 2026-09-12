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
        Clashed,
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
        [SerializeField, Range(1, 5)] private int maxEdge = 3;
        [SerializeField, Min(0f)] private float clashMomentumDuration = 5f;
        [SerializeField, Min(1f)] private float clashDamageMultiplier = 1.18f;
        [SerializeField, Min(1f)] private float clashPostureMultiplier = 1.28f;

        private float _parryTimer;
        private float _invulnerabilityTimer;
        private float _staggerTimer;
        private float _hitReactionTimer;
        private float _postureRecoveryDelayTimer;
        private float _clashMomentumTimer;

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
        public bool IsHitReacting => _hitReactionTimer > 0f;
        public bool IsAttackActive { get; private set; }
        public bool CanClash { get; private set; }
        public bool IsDead { get; private set; }
        public int Edge { get; private set; }
        public int MaxEdge => maxEdge;
        public float ClashMomentumRemaining => Mathf.Max(0f, _clashMomentumTimer);
        public bool HasClashMomentum => _clashMomentumTimer > 0f;
        public string LastEvent { get; private set; } = "准备交锋";

        private void Awake()
        {
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
                    SetEvent("重新站稳");
                }
            }

            if (_hitReactionTimer > 0f)
            {
                _hitReactionTimer -= Time.deltaTime;
            }

            if (_clashMomentumTimer > 0f)
            {
                _clashMomentumTimer -= Time.deltaTime;
                if (_clashMomentumTimer <= 0f && !IsDead)
                {
                    _clashMomentumTimer = 0f;
                    SetEvent("抗衡势散");
                }
            }

            if (IsDead || IsGuarding || IsStaggered || IsHitReacting || Posture <= 0f)
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
            IsAttackActive = false;
            CanClash = false;
            IsDead = false;
            _parryTimer = 0f;
            _invulnerabilityTimer = 0f;
            _staggerTimer = 0f;
            _hitReactionTimer = 0f;
            _postureRecoveryDelayTimer = 0f;
            _clashMomentumTimer = 0f;
            Edge = 0;
            LastEvent = "准备交锋";
            NotifyChanged();
        }

        public void BeginGuard(float parryWindowSeconds = 0.13f)
        {
            if (IsDead || IsStaggered || IsHitReacting)
            {
                return;
            }

            EndAttackWindow();
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

            EndAttackWindow();
            _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer, seconds);
        }

        public void BeginAttackWindow(bool canClash = true)
        {
            if (IsDead || IsStaggered || IsHitReacting || IsGuarding) return;
            IsAttackActive = true;
            CanClash = canClash;
            NotifyChanged();
        }

        public void EndAttackWindow()
        {
            if (!IsAttackActive && !CanClash) return;
            IsAttackActive = false;
            CanClash = false;
            NotifyChanged();
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

            float outgoingDamage = attack.Damage * (attacker != null ? attacker.OutgoingDamageMultiplier : 1f);
            float outgoingPosture = attack.PostureDamage * (attacker != null ? attacker.OutgoingPostureMultiplier : 1f);

            if (!attack.Unblockable && attacker != null && attacker.CanClash && CanClash)
            {
                EndAttackWindow();
                attacker.EndAttackWindow();
                AddPosture(attack.PostureDamage * 0.52f);
                attacker.AddPosture(attack.PostureDamage * 0.52f);
                ForceStagger(0.18f, "拼刀");
                attacker.ForceStagger(0.18f, "拼刀");
                if (IsPlayer) GainEdge("抗衡得势", true);
                if (attacker.IsPlayer) attacker.GainEdge("抗衡得势", true);
                return CombatOutcome.Clashed;
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

                if (IsPlayer) GainEdge("弹反得势", false);

                NotifyChanged();
                return CombatOutcome.Parried;
            }

            if (!attack.Unblockable && IsGuarding)
            {
                SetEvent($"挡住「{attack.Label}」");
                bool broken = AddPosture(outgoingPosture, "承受架势压力");
                return broken ? CombatOutcome.PostureBroken : CombatOutcome.Guarded;
            }

            Health = Mathf.Max(0f, Health - outgoingDamage);
            SetEvent(attack.Unblockable ? $"被「{attack.Label}」击中" : $"中刀：{attack.Label}");

            if (Health <= 0f)
            {
                Die();
                return CombatOutcome.Killed;
            }

            bool postureBroken = AddPosture(outgoingPosture * 0.45f);
            if (!postureBroken)
            {
                TriggerHitReaction(0.22f);
            }
            NotifyChanged();
            return postureBroken ? CombatOutcome.PostureBroken : CombatOutcome.Hit;
        }

        public bool TrySpendEdge(int amount)
        {
            if (IsDead || amount <= 0 || Edge < amount)
            {
                return false;
            }

            Edge -= amount;
            NotifyChanged();
            return true;
        }

        public void GainEdge(string reason, bool grantClashMomentum)
        {
            if (IsDead)
            {
                return;
            }

            Edge = Mathf.Min(maxEdge, Edge + 1);
            if (grantClashMomentum)
            {
                _clashMomentumTimer = Mathf.Max(_clashMomentumTimer, clashMomentumDuration);
            }

            LastEvent = $"{reason} · 锋意 {Edge}/{maxEdge}";
            NotifyChanged();
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
            EndAttackWindow();
            _parryTimer = 0f;
            _staggerTimer = Mathf.Max(_staggerTimer, seconds);
            _hitReactionTimer = 0f;
            LastEvent = reason;
            NotifyChanged();
        }

        private void TriggerHitReaction(float seconds)
        {
            if (IsDead)
            {
                return;
            }

            IsGuarding = false;
            EndAttackWindow();
            _parryTimer = 0f;
            _hitReactionTimer = Mathf.Max(_hitReactionTimer, seconds);
            LastEvent = "受创";
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

        public void Execute(Combatant attacker, float attackerInvulnerabilitySeconds = 0.85f)
        {
            if (IsDead)
            {
                return;
            }

            Health = 0f;
            LastEvent = "被处决";
            attacker?.SetInvulnerable(attackerInvulnerabilitySeconds);
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

        private float OutgoingDamageMultiplier => HasClashMomentum ? clashDamageMultiplier : 1f;
        private float OutgoingPostureMultiplier => HasClashMomentum ? clashPostureMultiplier : 1f;

        private void Die()
        {
            IsDead = true;
            IsGuarding = false;
            IsAttackActive = false;
            CanClash = false;
            _parryTimer = 0f;
            _invulnerabilityTimer = 0f;
            _staggerTimer = 0f;
            _hitReactionTimer = 0f;
            _clashMomentumTimer = 0f;
            LastEvent = IsPlayer ? "无铭者倒下" : "执刃者倒下";
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}

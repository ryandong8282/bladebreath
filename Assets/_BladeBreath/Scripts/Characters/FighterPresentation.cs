using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    // Presentation observes combat. It never applies damage, moves a fighter, or grants invulnerability.
    // This component lives on ModelRoot, below the root that owns collision and gameplay.
    public sealed class FighterPresentation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform weaponSocket;
        // Normalized contact samples for the four distinct UAL2 sword takes. Combat still owns the
        // actual hit window; these values align the visible blade crossing with that clock.
        [SerializeField, Range(0.05f, 0.95f)] private float attackContactTime = 0.5f;
        [SerializeField, Range(0.05f, 0.95f)] private float attack2ContactTime = 0.5f;
        [SerializeField, Range(0.05f, 0.95f)] private float attack3ContactTime = 0.2f;
        [SerializeField, Range(0.05f, 0.95f)] private float heavyContactTime = 0.42f;

        private Combatant _combatant;
        private Vector3 _previousPosition;
        private float _previousHealth;
        private float _previousPosture;
        private float _hitStartedAt;
        private float _hitUntil;
        private float _guardImpactStartedAt;
        private float _guardImpactUntil;
        private bool _wasDead;
        private bool _wasStaggered;
        private float _reactionSide = 1f;
        private int _currentState;
        private int _actionState;
        private float _actionProgress;
        private int _skillVariant = 1;
        private float _sampledProgress = -1f;
        private Vector3 _restScale;
        private Quaternion _restRotation;
        private Transform _visualRoot;
        private Quaternion _visualRestRotation;
        private Vector3 _visualRestPosition;
        private Quaternion _weaponRestRotation;
        private BladeArcTrail _weaponTrail;
        private TrailRenderer[] _dodgeTrails;
        private Material _dodgeTrailMaterial;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int LocomotionRateId = Animator.StringToHash("LocomotionRate");
        private static readonly int LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
        private static readonly int GuardId = Animator.StringToHash("Base Layer.Guard");
        private static readonly int GuardEnterId = Animator.StringToHash("Base Layer.GuardEnter");
        private static readonly int GuardImpactId = Animator.StringToHash("Base Layer.GuardImpact");
        private static readonly int AttackId = Animator.StringToHash("Base Layer.Attack");
        private static readonly int Attack2Id = Animator.StringToHash("Base Layer.Attack2");
        private static readonly int Attack3Id = Animator.StringToHash("Base Layer.Attack3");
        private static readonly int HeavyAttackId = Animator.StringToHash("Base Layer.HeavyAttack");
        private static readonly int SkillAttackId = Animator.StringToHash("Base Layer.SkillAttack");
        private static readonly int HitId = Animator.StringToHash("Base Layer.Hit");
        private static readonly int DodgeId = Animator.StringToHash("Base Layer.Dodge");
        private static readonly int DodgeLeftId = Animator.StringToHash("Base Layer.DodgeLeft");
        private static readonly int DodgeRightId = Animator.StringToHash("Base Layer.DodgeRight");
        private static readonly int DeathId = Animator.StringToHash("Base Layer.Death");
        private static readonly int StaggerId = Animator.StringToHash("Base Layer.Stagger");

        public Animator Animator => animator;
        public Transform WeaponSocket => weaponSocket;
        public bool IsAnimated => animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman;
        public bool IsShowingGuardImpact => Time.time < _guardImpactUntil;
        public bool IsShowingDodgeTrail => _dodgeTrails != null && _dodgeTrails.Length > 0 && _dodgeTrails[0].emitting;
        public bool IsShowingWeaponTrail => _weaponTrail != null &&
                                            (_weaponTrail.IsEmitting || _weaponTrail.SampleCount >= 2);
        public int WeaponTrailSampleCount => _weaponTrail != null ? _weaponTrail.SampleCount : 0;

        public void Configure(Animator modelAnimator, Transform socket)
        {
            animator = modelAnimator;
            weaponSocket = socket;
            CachePoseReferences();
        }

        public void Bind(Combatant combatant)
        {
            if (_combatant != null) _combatant.Changed -= OnCombatChanged;
            _combatant = combatant;
            _restScale = transform.localScale;
            _restRotation = transform.localRotation;
            _previousPosition = combatant.transform.position;
            _previousHealth = combatant.Health;
            _previousPosture = combatant.Posture;
            combatant.Changed += OnCombatChanged;
            CachePoseReferences();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (!IsAnimated || animator.runtimeAnimatorController == null)
                    Debug.LogError("Humanoid visual needs a valid Humanoid Avatar and controller. Run BladeBreath > Art > Validate Humanoid Setup.", this);
            }
            CreateWeaponTrail();
            CreateDodgeTrails();
        }

        // The controllers supply progress from their existing combat clocks, including the contact frame.
        public void SampleAttack(float progress, bool recoveryPhase, int comboStep = 1)
        {
            _actionState = comboStep == 2 ? Attack2Id : comboStep >= 3 ? Attack3Id : AttackId;
            float contactTime = comboStep == 2 ? attack2ContactTime
                : comboStep >= 3 ? attack3ContactTime : attackContactTime;
            _actionProgress = recoveryPhase
                ? Mathf.Lerp(contactTime, 1f, Mathf.Clamp01(progress))
                : Mathf.Lerp(0f, contactTime, Mathf.Clamp01(progress));
            // Begin shortly before contact so the ribbon describes the incoming blade path,
            // then retain part of the follow-through. Starting only after damage resolution
            // produced a disconnected flash with no readable swing direction.
            _weaponTrail?.SetEmitting((!recoveryPhase && progress >= 0.42f) ||
                                      (recoveryPhase && progress < 0.72f), 0.2f);
        }

        public void SampleHeavyAttack(float progress, bool recoveryPhase)
        {
            _actionState = HeavyAttackId;
            _actionProgress = recoveryPhase
                ? Mathf.Lerp(heavyContactTime, 1f, Mathf.Clamp01(progress))
                : Mathf.Lerp(0f, heavyContactTime, Mathf.Clamp01(progress));
            _weaponTrail?.SetEmitting((!recoveryPhase && progress >= 0.46f) ||
                                      (recoveryPhase && progress < 0.78f), 0.23f);
        }

        public void SampleSkillAttack(float progress, bool recoveryPhase, int variant = 1)
        {
            _actionState = SkillAttackId;
            _skillVariant = Mathf.Clamp(variant, 1, 3);
            const float skillContactTime = 0.36f;
            _actionProgress = recoveryPhase
                ? Mathf.Lerp(skillContactTime, 1f, Mathf.Clamp01(progress))
                : Mathf.Lerp(0f, skillContactTime, Mathf.Clamp01(progress));
            if (_weaponTrail != null)
            {
                _weaponTrail.SetEmitting((!recoveryPhase && progress >= 0.4f) ||
                                         (recoveryPhase && progress < 0.84f), 0.26f);
            }
        }

        public void SampleDodge(float progress, float lateralDirection = 0f)
        {
            _actionState = lateralDirection < -0.25f ? DodgeLeftId
                : lateralDirection > 0.25f ? DodgeRightId : DodgeId;
            _actionProgress = Mathf.Clamp01(progress);
            SetDodgeTrail(true);
        }

        public void EndAction()
        {
            _actionState = 0;
            _skillVariant = 1;
            if (_weaponTrail != null)
            {
                _weaponTrail.SetEmitting(false);
            }
            SetDodgeTrail(false);
        }

        // Editor proof tooling uses the same deterministic pose overlay as Play Mode.
        public void PreviewAttackPose(int comboStep, float progress, bool heavy = false)
        {
            if (animator == null) return;
            CachePoseReferences();
            int state = heavy ? HeavyAttackId : comboStep == 2 ? Attack2Id : comboStep >= 3 ? Attack3Id : AttackId;
            animator.Play(state, 0, Mathf.Clamp01(progress));
            animator.Update(0f);
            ApplyCombatPose(state, Mathf.Clamp01(progress));
        }

        public void PreviewGuardPose(bool parry)
        {
            if (animator == null) return;
            CachePoseReferences();
            int state = parry ? GuardEnterId : GuardId;
            animator.Play(state, 0, parry ? 0.45f : 0.2f);
            animator.Update(0f);
            ApplyCombatPose(state, parry ? 0.45f : 0.2f);
        }

        public void PreviewDodgePose(float lateralDirection, float progress)
        {
            if (animator == null) return;
            CachePoseReferences();
            int state = lateralDirection < 0f ? DodgeLeftId : DodgeRightId;
            float clampedProgress = Mathf.Clamp01(progress);
            animator.Play(state, 0, clampedProgress);
            animator.Update(0f);
            ApplyCombatPose(state, clampedProgress);
        }

        private void CreateWeaponTrail()
        {
            if (weaponSocket == null || _weaponTrail != null)
            {
                return;
            }

            Transform bladeBase = weaponSocket.Find("Longblade/TrailBase");
            Transform bladeTip = weaponSocket.Find("Longblade/TrailTip");
            if (bladeBase == null || bladeTip == null)
            {
                Debug.LogError("Longblade needs TrailBase and TrailTip anchors for the blade arc.", this);
                return;
            }

            Color edge = _combatant != null && _combatant.IsPlayer
                ? new Color(0.73f, 0.88f, 1f, 0.8f)
                : new Color(1f, 0.10f, 0.025f, 0.68f);
            var trailObject = new GameObject("Blade arc trail");
            trailObject.transform.SetParent(transform, false);
            _weaponTrail = trailObject.AddComponent<BladeArcTrail>();
            _weaponTrail.Configure(bladeBase, bladeTip, edge);
        }

        private void CreateDodgeTrails()
        {
            if (_dodgeTrails != null)
            {
                return;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _dodgeTrailMaterial = new Material(shader) { name = "Runtime dodge afterimage" };
            Color color = _combatant != null && _combatant.IsPlayer
                ? new Color(0.055f, 0.14f, 0.19f, 0.2f)
                : new Color(0.2f, 0.055f, 0.025f, 0.18f);
            if (_dodgeTrailMaterial.HasProperty("_BaseColor")) _dodgeTrailMaterial.SetColor("_BaseColor", color);
            if (_dodgeTrailMaterial.HasProperty("_Color")) _dodgeTrailMaterial.SetColor("_Color", color);
            if (_dodgeTrailMaterial.HasProperty("_Surface")) _dodgeTrailMaterial.SetFloat("_Surface", 1f);
            if (_dodgeTrailMaterial.HasProperty("_SrcBlend")) _dodgeTrailMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (_dodgeTrailMaterial.HasProperty("_DstBlend")) _dodgeTrailMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (_dodgeTrailMaterial.HasProperty("_ZWrite")) _dodgeTrailMaterial.SetFloat("_ZWrite", 0f);
            _dodgeTrailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _dodgeTrailMaterial.SetOverrideTag("RenderType", "Transparent");
            _dodgeTrailMaterial.renderQueue = (int)RenderQueue.Transparent;

            _dodgeTrails = new TrailRenderer[1];
            for (int i = 0; i < _dodgeTrails.Length; i++)
            {
                GameObject anchor = new GameObject("Dodge afterimage " + (i + 1));
                anchor.transform.SetParent(transform, false);
                anchor.transform.localPosition = new Vector3(0f, 0.94f, -0.06f);
                TrailRenderer trail = anchor.AddComponent<TrailRenderer>();
                trail.time = 0.16f;
                trail.minVertexDistance = 0.08f;
                trail.widthCurve = new AnimationCurve(
                    new Keyframe(0f, 0.1f),
                    new Keyframe(0.42f, 0.045f),
                    new Keyframe(1f, 0f));
                trail.startColor = color;
                trail.endColor = new Color(color.r, color.g, color.b, 0f);
                trail.numCornerVertices = 2;
                trail.numCapVertices = 2;
                trail.alignment = LineAlignment.View;
                trail.textureMode = LineTextureMode.Stretch;
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.sharedMaterial = _dodgeTrailMaterial;
                trail.emitting = false;
                _dodgeTrails[i] = trail;
            }
        }

        private void SetDodgeTrail(bool emitting)
        {
            if (_dodgeTrails == null) return;
            foreach (TrailRenderer trail in _dodgeTrails)
            {
                if (trail != null) trail.emitting = emitting;
            }
        }

        private void OnCombatChanged(Combatant combatant)
        {
            if (combatant.Health < _previousHealth)
            {
                _hitStartedAt = Time.time;
                _hitUntil = Time.time + 0.28f;
                _reactionSide *= -1f;
            }
            if (combatant.IsGuarding && combatant.Posture > _previousPosture)
            {
                _guardImpactStartedAt = Time.time;
                _guardImpactUntil = Time.time + 0.16f;
            }
            if (combatant.IsDead || combatant.IsStaggered || combatant.IsHitReacting) EndAction();
            if (combatant.IsStaggered && !_wasStaggered && combatant.LastEvent != "受创")
            {
                _reactionSide *= -1f;
            }
            if (!combatant.IsDead && _wasDead)
            {
                _currentState = 0;
                _hitUntil = 0f;
                _guardImpactStartedAt = 0f;
                _guardImpactUntil = 0f;
                EndAction();
                if (animator != null) animator.Rebind();
            }
            _previousHealth = combatant.Health;
            _previousPosture = combatant.Posture;
            _wasDead = combatant.IsDead;
            _wasStaggered = combatant.IsStaggered;
        }

        private void LateUpdate()
        {
            if (_combatant == null) return;
            Vector3 displacement = _combatant.transform.position - _previousPosition;
            displacement.y = 0f;
            _previousPosition = _combatant.transform.position;

            if (animator == null)
            {
                transform.localScale = _combatant.IsDead
                    ? Vector3.Scale(_restScale, new Vector3(1f, 0.28f, 1f)) : _restScale;
                transform.localRotation = _restRotation * (_combatant.IsStaggered
                    ? Quaternion.Euler(0f, 0f, -11f) : Quaternion.identity);
                return;
            }

            if (!IsAnimated || animator.runtimeAnimatorController == null) return;
            float speed = Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;
            animator.SetFloat(SpeedId, speed, 0.08f, Time.deltaTime);
            animator.SetFloat(LocomotionRateId, Mathf.Clamp(speed / 2.25f, 1f, 2.4f));

            int desired = _combatant.IsDead ? DeathId
                : _combatant.IsStaggered ? StaggerId
                : _combatant.IsHitReacting ? HitId
                : _actionState != 0 ? _actionState
                : Time.time < _guardImpactUntil ? GuardImpactId
                : _combatant.IsParrying ? GuardEnterId
                : _combatant.IsGuarding ? GuardId
                : Time.time < _hitUntil ? HitId : LocomotionId;

            // A stagger holds the recoil pose until the actual combat stagger ends.
            float presentationProgress = _actionProgress;
            if (desired == AttackId || desired == Attack2Id || desired == Attack3Id || desired == HeavyAttackId || desired == SkillAttackId ||
                desired == DodgeId || desired == DodgeLeftId || desired == DodgeRightId ||
                desired == GuardImpactId || desired == HitId || desired == StaggerId)
            {
                float progress = desired == StaggerId ? 0.4f
                    : desired == GuardImpactId ? Mathf.Clamp01((Time.time - _guardImpactStartedAt) / 0.16f)
                    : desired == HitId ? Mathf.Clamp01((Time.time - _hitStartedAt) / 0.28f)
                    : Mathf.Min(_actionProgress, 0.999f);
                presentationProgress = progress;
                if (_currentState != desired || !Mathf.Approximately(progress, _sampledProgress))
                {
                    animator.Play(desired, 0, progress);
                    animator.Update(0f);
                    _sampledProgress = progress;
                }
            }
            else if (_currentState != desired)
            {
                animator.CrossFadeInFixedTime(desired, desired == DeathId ? 0.04f : 0.06f, 0, 0f);
                _sampledProgress = -1f;
            }

            ApplyCombatPose(desired, presentationProgress);
            _currentState = desired;
        }

        private void CachePoseReferences()
        {
            if (animator == null || weaponSocket == null) return;
            _visualRoot = animator.transform;
            _visualRestRotation = _visualRoot.localRotation;
            _visualRestPosition = _visualRoot.localPosition;
            _weaponRestRotation = weaponSocket.localRotation;
        }

        private void ApplyCombatPose(int state, float progress)
        {
            if (_visualRoot == null || weaponSocket == null) CachePoseReferences();
            if (_visualRoot == null || weaponSocket == null) return;

            Vector3 weaponEuler = Vector3.zero;
            Vector3 bodyEuler = Vector3.zero;
            Vector3 visualOffset = Vector3.zero;
            bool authoredAttack = state == AttackId || state == Attack2Id || state == Attack3Id ||
                                  state == HeavyAttackId;
            if (authoredAttack)
            {
                // UAL2 already supplies complete full-body motion. The previous 80-150 degree
                // socket rotations fought the authored hand path and made the sword arc kink.
                // Keep only a tiny forward weight transfer; the controller still owns movement.
                visualOffset = Vector3.forward * (0.035f * Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI));
            }
            else if (state == SkillAttackId)
            {
                // Skills reuse the clean authored body action. Small socket differences preserve
                // their silhouettes without pulling the blade away from the hands.
                float phase = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
                weaponEuler = _skillVariant == 2 ? new Vector3(5f, -12f, -8f) * phase
                    : _skillVariant == 3 ? new Vector3(14f, 5f, -4f) * phase
                    : new Vector3(-4f, -15f, -5f) * phase;
                bodyEuler = _skillVariant == 3 ? new Vector3(3f, 5f, 0f) * phase
                    : new Vector3(0f, -4f, 0f) * phase;
                visualOffset = Vector3.forward * ((_skillVariant == 1 ? 0.1f : 0.045f) * phase);
            }
            else if (state == GuardEnterId)
            {
                // The imported Block take raises the hands and blade. Only a small alignment
                // correction remains so the weapon stays between the fighter and the threat.
                weaponEuler = new Vector3(3f, -7f, -5f);
                bodyEuler = new Vector3(-2f, -3f, 1f);
            }
            else if (state == GuardId)
            {
                weaponEuler = new Vector3(2f, -5f, -4f);
                bodyEuler = new Vector3(-1f, -2f, 1f);
            }
            else if (state == GuardImpactId)
            {
                float phase = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
                weaponEuler = new Vector3(5f, -8f, -7f) * phase;
                bodyEuler = new Vector3(-5f * phase, -7f * phase, 5f * _reactionSide * phase);
                visualOffset = Vector3.back * (0.08f * phase);
            }
            else if (state == HitId)
            {
                float phase = Mathf.Sin(Mathf.Clamp01((Time.time - _hitStartedAt) / 0.28f) * Mathf.PI);
                bodyEuler = new Vector3(-12f * phase, 11f * _reactionSide * phase, 15f * _reactionSide * phase);
                weaponEuler = new Vector3(14f * phase, -24f * _reactionSide * phase, 28f * _reactionSide * phase);
                visualOffset = new Vector3(0.07f * _reactionSide * phase, -0.03f * phase, -0.18f * phase);
            }
            else if (state == StaggerId)
            {
                bodyEuler = new Vector3(-13f, 8f * _reactionSide, 14f * _reactionSide);
                weaponEuler = new Vector3(22f, -34f * _reactionSide, 36f * _reactionSide);
                visualOffset = Vector3.back * 0.13f;
            }
            else if (state == DodgeLeftId || state == DodgeRightId || state == DodgeId)
            {
                float phase = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
                float side = state == DodgeLeftId ? -1f : state == DodgeRightId ? 1f : _reactionSide;
                bodyEuler = new Vector3(12f * phase, 0f, -7f * side * phase);
                weaponEuler = new Vector3(-10f * phase, -36f * side * phase, 18f * side * phase);
                visualOffset = Vector3.down * (0.08f * phase);
            }

            _visualRoot.localPosition = _visualRestPosition + visualOffset;
            _visualRoot.localRotation = _visualRestRotation * Quaternion.Euler(bodyEuler);
            weaponSocket.localRotation = _weaponRestRotation * Quaternion.Euler(weaponEuler);
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        public void ReportWeaponContact(Combatant target, CombatOutcome outcome)
        {
            if (target == null || _weaponTrail == null || _weaponTrail.BladeBase == null || _weaponTrail.BladeTip == null)
                return;
            CombatFeedback feedback = target.GetComponent<CombatFeedback>();
            if (feedback == null) feedback = target.GetComponentInChildren<CombatFeedback>();
            if (feedback == null) return;

            Vector3 basePoint = _weaponTrail.BladeBase.position;
            Vector3 tipPoint = _weaponTrail.BladeTip.position;
            Vector3 direction = (tipPoint - basePoint).normalized;
            Vector3 contact;
            FighterPresentation defender = target.GetComponentInChildren<FighterPresentation>();
            if ((outcome == CombatOutcome.Clashed || outcome == CombatOutcome.Parried || outcome == CombatOutcome.Guarded) &&
                defender != null && defender._weaponTrail != null &&
                defender._weaponTrail.BladeBase != null && defender._weaponTrail.BladeTip != null)
            {
                ClosestPointsOnSegments(basePoint, tipPoint,
                    defender._weaponTrail.BladeBase.position, defender._weaponTrail.BladeTip.position,
                    out Vector3 attackerBlade, out Vector3 defenderBlade);
                contact = Vector3.Lerp(attackerBlade, defenderBlade, 0.5f);
                direction = (direction + (defender._weaponTrail.BladeTip.position - defender._weaponTrail.BladeBase.position).normalized).normalized;
            }
            else
            {
                Vector3 chest = target.transform.position + Vector3.up * 1.02f;
                Vector3 bladePoint = ClosestPointOnSegment(basePoint, tipPoint, chest);
                contact = Vector3.Lerp(chest, bladePoint, 0.28f);
            }
            feedback.PlayWeaponContact(contact, direction, outcome);
        }

        private static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.000001f) return start;
            return start + segment * Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        }

        private static void ClosestPointsOnSegments(
            Vector3 firstStart, Vector3 firstEnd, Vector3 secondStart, Vector3 secondEnd,
            out Vector3 firstPoint, out Vector3 secondPoint)
        {
            Vector3 first = firstEnd - firstStart;
            Vector3 second = secondEnd - secondStart;
            Vector3 offset = firstStart - secondStart;
            float a = Vector3.Dot(first, first);
            float e = Vector3.Dot(second, second);
            float b = Vector3.Dot(first, second);
            float c = Vector3.Dot(first, offset);
            float f = Vector3.Dot(second, offset);
            float denominator = a * e - b * b;
            float firstT = denominator > 0.000001f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
            float secondT = e > 0.000001f ? Mathf.Clamp01((b * firstT + f) / e) : 0f;
            if (a > 0.000001f) firstT = Mathf.Clamp01((b * secondT - c) / a);
            firstPoint = firstStart + first * firstT;
            secondPoint = secondStart + second * secondT;
        }

        private void OnDestroy()
        {
            if (_combatant != null) _combatant.Changed -= OnCombatChanged;
            if (_dodgeTrailMaterial != null) Destroy(_dodgeTrailMaterial);
        }
    }
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BladeBreath
{
    // A fixed-axis combat camera. The historical class name is retained so existing
    // scenes and prefabs keep their serialized component reference.
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offsetDirection = new Vector3(0f, 0.56f, -1f);
        [SerializeField, Min(0f)] private float followSharpness = 8f;
        [SerializeField, Min(1f)] private float framingDistance = 6.8f;
        [SerializeField, Min(1f)] private float minimumFramingDistance = 5.8f;
        [SerializeField, Min(1f)] private float maximumFramingDistance = 11f;
        [SerializeField, Min(0f)] private float wheelZoomSpeed = 0.006f;
        [SerializeField, Min(0f)] private float zoomSharpness = 10f;
        [SerializeField, Range(0f, 0.5f)] private float opponentFraming = 0.42f;
        [SerializeField, Range(25f, 65f)] private float fieldOfView = 37f;

        private Transform _target;
        private Transform _opponent;
        private Camera _camera;
        private Combatant _playerCombatant;
        private Combatant _enemyCombatant;
        private float _playerHealth;
        private float _enemyHealth;
        private float _playerPosture;
        private float _enemyPosture;
        private string _playerEvent;
        private string _enemyEvent;
        private float _shake;
        private float _hitStopUntil;
        private float _timeScaleBeforeHitStop = 1f;
        private bool _ownsHitStop;
        private float _targetFramingDistance;
        private float _currentFramingDistance;

        public float CurrentFramingDistance => _currentFramingDistance;
        // Kept for the existing validation API while the prototype transitions from
        // orthographic framing to a perspective hero camera.
        public float CurrentOrthographicSize => CurrentFramingDistance;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ConfigureProjection();
            _targetFramingDistance = framingDistance;
            _currentFramingDistance = framingDistance;
        }

        private void LateUpdate()
        {
            UpdateHitStop();
            UpdateZoom();
            if (_target == null) return;

            Vector3 focus = GetFocusPoint();
            Vector3 direction = offsetDirection.sqrMagnitude > 0.001f
                ? offsetDirection.normalized
                : new Vector3(0f, 0.56f, -1f).normalized;
            Vector3 desired = focus + direction * _currentFramingDistance;
            if (_shake > 0.001f)
            {
                float phase = Time.unscaledTime * 83f;
                Vector3 noise = new Vector3(
                    Mathf.Sin(phase * 1.17f),
                    Mathf.Cos(phase * 0.91f),
                    Mathf.Sin(phase * 0.73f));
                desired += noise * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 1.8f);
            }

            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
            transform.LookAt(focus + Vector3.up * 0.82f, Vector3.up);
        }

        private void ConfigureProjection()
        {
            if (_camera == null) return;
            _camera.orthographic = false;
            _camera.fieldOfView = fieldOfView;
        }

        private void UpdateZoom()
        {
            if (_camera == null) return;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float wheel = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    _targetFramingDistance = Mathf.Clamp(
                        _targetFramingDistance - wheel * wheelZoomSpeed,
                        minimumFramingDistance,
                        maximumFramingDistance);
                }
            }
#endif
            _currentFramingDistance = Mathf.Lerp(
                _currentFramingDistance,
                _targetFramingDistance,
                1f - Mathf.Exp(-zoomSharpness * Time.unscaledDeltaTime));
        }

        public void SetZoomForValidation(float distance)
        {
            _targetFramingDistance = Mathf.Clamp(distance, minimumFramingDistance, maximumFramingDistance);
        }

        public void SnapZoomForCapture(float distance)
        {
            float clamped = Mathf.Clamp(distance, minimumFramingDistance, maximumFramingDistance);
            _targetFramingDistance = clamped;
            _currentFramingDistance = clamped;
        }

        public void BindFeedback(Combatant player, Combatant enemy)
        {
            UnbindFeedback();
            _playerCombatant = player;
            _enemyCombatant = enemy;
            if (player != null)
            {
                _playerHealth = player.Health;
                _playerPosture = player.Posture;
                _playerEvent = player.LastEvent;
                player.Changed += OnCombatChanged;
            }
            if (enemy != null)
            {
                _enemyHealth = enemy.Health;
                _enemyPosture = enemy.Posture;
                _enemyEvent = enemy.LastEvent;
                enemy.Changed += OnCombatChanged;
            }
        }

        private void OnCombatChanged(Combatant combatant)
        {
            bool player = combatant == _playerCombatant;
            float oldHealth = player ? _playerHealth : _enemyHealth;
            float oldPosture = player ? _playerPosture : _enemyPosture;
            string oldEvent = player ? _playerEvent : _enemyEvent;

            if (combatant.LastEvent == "拼刀" && oldEvent != "拼刀")
            {
                _shake = Mathf.Max(_shake, 0.12f);
                RequestHitStop(0.1f);
            }
            else if (combatant.Health < oldHealth - 0.01f)
            {
                _shake = Mathf.Max(_shake, 0.065f);
                RequestHitStop(0.065f);
            }
            else if (combatant.LastEvent != oldEvent && combatant.LastEvent.StartsWith("背袭·"))
            {
                _shake = Mathf.Max(_shake, 0.1f);
                RequestHitStop(0.055f);
            }
            else if (combatant.Posture > oldPosture + 20f || combatant.LastEvent == "架势崩溃")
            {
                _shake = Mathf.Max(_shake, 0.09f);
                RequestHitStop(0.08f);
            }
            else if (combatant.LastEvent != oldEvent &&
                     (combatant.LastEvent.Contains("弹开") || combatant.LastEvent.Contains("被弹反") ||
                      combatant.LastEvent.Contains("刀势被截")))
            {
                _shake = Mathf.Max(_shake, 0.11f);
                RequestHitStop(0.085f);
            }

            if (player)
            {
                _playerHealth = combatant.Health;
                _playerPosture = combatant.Posture;
                _playerEvent = combatant.LastEvent;
            }
            else
            {
                _enemyHealth = combatant.Health;
                _enemyPosture = combatant.Posture;
                _enemyEvent = combatant.LastEvent;
            }
        }

        private void RequestHitStop(float seconds)
        {
            if (!_ownsHitStop)
            {
                _timeScaleBeforeHitStop = Time.timeScale;
                _ownsHitStop = true;
            }

            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + seconds);
            Time.timeScale = Mathf.Min(Time.timeScale, 0.08f);
        }

        private void UpdateHitStop()
        {
            if (!_ownsHitStop || Time.unscaledTime < _hitStopUntil) return;
            Time.timeScale = _timeScaleBeforeHitStop;
            _ownsHitStop = false;
        }

        private void UnbindFeedback()
        {
            if (_playerCombatant != null) _playerCombatant.Changed -= OnCombatChanged;
            if (_enemyCombatant != null) _enemyCombatant.Changed -= OnCombatChanged;
            _playerCombatant = null;
            _enemyCombatant = null;
        }

        public void Configure(
            Transform target,
            Transform opponent,
            Vector3 framingOffsetDirection,
            float distance)
        {
            _target = target;
            _opponent = opponent;
            offsetDirection = framingOffsetDirection.sqrMagnitude > 0.001f
                ? framingOffsetDirection.normalized
                : new Vector3(0f, 0.56f, -1f).normalized;
            framingDistance = Mathf.Clamp(distance, minimumFramingDistance, maximumFramingDistance);
            _targetFramingDistance = framingDistance;
            _currentFramingDistance = framingDistance;
            if (_camera == null) _camera = GetComponent<Camera>();
            ConfigureProjection();

            Vector3 focus = GetFocusPoint();
            transform.position = focus + offsetDirection * _currentFramingDistance;
            transform.LookAt(focus + Vector3.up * 0.82f, Vector3.up);
        }

        private Vector3 GetFocusPoint()
        {
            if (_opponent == null) return _target.position;
            return Vector3.Lerp(_target.position, _opponent.position, opponentFraming);
        }

        private void OnDestroy()
        {
            UnbindFeedback();
            if (_ownsHitStop)
            {
                Time.timeScale = _timeScaleBeforeHitStop;
                _ownsHitStop = false;
            }
        }
    }
}

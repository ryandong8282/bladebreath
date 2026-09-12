using UnityEngine;

namespace BladeBreath
{
    // Positional combat and movement audio. It observes the existing combat state and
    // never resolves hits or changes timing.
    [DisallowMultipleComponent]
    public sealed class CombatAudio : MonoBehaviour
    {
        private static int _lastSharedMetalFrame = -1;

        private Combatant _combatant;
        private AudioSource _source;
        private AudioClip[] _swings;
        private AudioClip[] _metalImpacts;
        private AudioClip[] _bodyHits;
        private AudioClip[] _postureBreaks;
        private AudioClip[] _cloth;
        private AudioClip[] _footsteps;
        private float _previousHealth;
        private float _previousPosture;
        private bool _wasStaggered;
        private bool _wasDead;
        private string _previousEvent;
        private Vector3 _previousPosition;
        private float _stepDistance;

        public void Configure(
            Combatant combatant,
            AudioClip[] swings,
            AudioClip[] metalImpacts,
            AudioClip[] bodyHits,
            AudioClip[] postureBreaks,
            AudioClip[] cloth,
            AudioClip[] footsteps)
        {
            if (_combatant != null) _combatant.Changed -= OnCombatChanged;
            _combatant = combatant;
            _swings = swings;
            _metalImpacts = metalImpacts;
            _bodyHits = bodyHits;
            _postureBreaks = postureBreaks;
            _cloth = cloth;
            _footsteps = footsteps;
            _previousHealth = combatant.Health;
            _previousPosture = combatant.Posture;
            _wasStaggered = combatant.IsStaggered;
            _wasDead = combatant.IsDead;
            _previousEvent = combatant.LastEvent;
            _previousPosition = transform.position;
            _stepDistance = 0f;

            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0.72f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 1.5f;
            _source.maxDistance = 18f;
            _source.dopplerLevel = 0f;
            _source.reverbZoneMix = 0.25f;
            combatant.Changed += OnCombatChanged;
        }

        private void Update()
        {
            if (_combatant == null) return;

            Vector3 current = transform.position;
            Vector2 horizontalDelta = new Vector2(current.x - _previousPosition.x, current.z - _previousPosition.z);
            _previousPosition = current;
            if (_combatant.IsDead || _combatant.IsStaggered || _combatant.IsHitReacting || _combatant.IsAttackActive)
            {
                _stepDistance = 0f;
                return;
            }

            float distance = horizontalDelta.magnitude;
            if (distance < 0.0001f)
            {
                _stepDistance = Mathf.Max(0f, _stepDistance - Time.deltaTime * 0.35f);
                return;
            }

            _stepDistance += distance;
            if (_stepDistance >= 0.72f)
            {
                _stepDistance %= 0.72f;
                PlayRandom(_footsteps, 0.3f, 0.93f, 1.07f);
            }
        }

        private void OnCombatChanged(Combatant combatant)
        {
            string currentEvent = combatant.LastEvent;
            bool eventChanged = currentEvent != _previousEvent;

            if (eventChanged && IsSwingEvent(currentEvent))
            {
                PlayRandom(_swings, currentEvent.Contains("蓄势") ? 0.68f : 0.54f, 0.9f, 1.08f);
            }
            else if (eventChanged && currentEvent == "闪身")
            {
                PlayRandom(_cloth, 0.58f, 0.86f, 1.02f);
            }

            if (eventChanged && currentEvent.StartsWith("背袭·"))
            {
                PlayRandom(_bodyHits, 0.82f, 0.78f, 0.92f);
                PlayRandom(_metalImpacts, 0.5f, 0.84f, 0.96f);
            }

            bool metalEvent = eventChanged &&
                (currentEvent == "拼刀" || currentEvent.Contains("挡住") || currentEvent.Contains("弹开") ||
                 currentEvent.Contains("被弹反") || currentEvent.Contains("刀势被截") ||
                 currentEvent.Contains("攻击被弹开") || currentEvent.Contains("重斩被弹开"));
            if (metalEvent && _lastSharedMetalFrame != Time.frameCount)
            {
                _lastSharedMetalFrame = Time.frameCount;
                PlayRandom(_metalImpacts, currentEvent == "拼刀" ? 1f : 0.72f,
                    currentEvent == "拼刀" ? 0.8f : 0.88f,
                    currentEvent == "拼刀" ? 0.94f : 1.04f);
            }

            if (!currentEvent.StartsWith("背袭·") && combatant.Health < _previousHealth - 0.01f)
            {
                PlayRandom(_bodyHits, 0.58f, 0.9f, 1.08f);
            }

            if (combatant.IsStaggered && !_wasStaggered && currentEvent == "架势崩溃")
            {
                PlayRandom(_postureBreaks, 0.92f, 0.88f, 0.98f);
            }

            if (combatant.IsDead && !_wasDead)
            {
                PlayRandom(_postureBreaks, 0.72f, 0.75f, 0.88f);
                PlayRandom(_cloth, 0.42f, 0.82f, 0.94f);
            }

            _previousHealth = combatant.Health;
            _previousPosture = combatant.Posture;
            _wasStaggered = combatant.IsStaggered;
            _wasDead = combatant.IsDead;
            _previousEvent = currentEvent;
        }

        private static bool IsSwingEvent(string eventText)
        {
            return eventText.Contains("起势") || eventText.Contains("蓄势") ||
                   eventText.StartsWith("明斩") || eventText.StartsWith("裂地") ||
                   eventText == "迎推·借线" || eventText.StartsWith("回锋·返刃");
        }

        private void PlayRandom(AudioClip[] clips, float volume, float minimumPitch, float maximumPitch)
        {
            if (_source == null || clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            _source.pitch = Random.Range(minimumPitch, maximumPitch);
            _source.PlayOneShot(clip, volume);
        }

        private void OnDestroy()
        {
            if (_combatant != null) _combatant.Changed -= OnCombatChanged;
        }
    }
}

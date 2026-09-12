using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    // Reuses one small particle system per fighter for hit, guard and posture feedback.
    // It observes Combatant changes and never changes combat state.
    public sealed class CombatFeedback : MonoBehaviour
    {
        private Combatant _combatant;
        private ParticleSystem _particles;
        private CombatImpactRing _impactRing;
        private Material _material;
        private Texture2D _sparkTexture;
        private float _previousHealth;
        private float _previousPosture;
        private bool _wasStaggered;
        private string _previousEvent;
        private Color _accent;
        private int _weaponContactCount;
        private Vector3 _lastWeaponContactPosition;

        public int WeaponContactCount => _weaponContactCount;
        public Vector3 LastWeaponContactPosition => _lastWeaponContactPosition;

        public void Bind(Combatant combatant, Color accent)
        {
            if (_combatant != null) _combatant.Changed -= OnCombatChanged;
            _combatant = combatant;
            _accent = accent;
            _previousHealth = combatant.Health;
            _previousPosture = combatant.Posture;
            _wasStaggered = combatant.IsStaggered;
            _previousEvent = combatant.LastEvent;
            CreateParticleSystem();
            combatant.Changed += OnCombatChanged;
        }

        private void CreateParticleSystem()
        {
            GameObject effectObject = new GameObject("Combat sparks");
            effectObject.transform.SetParent(transform, false);
            effectObject.transform.localPosition = Vector3.up * 0.95f;
            _particles = effectObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            main.gravityModifier = 0.35f;
            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.enabled = false;

            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                : Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            _material = new Material(shader) { name = "Runtime combat sparks" };
            if (_material.HasProperty("_Surface")) _material.SetFloat("_Surface", 1f);
            if (_material.HasProperty("_Blend")) _material.SetFloat("_Blend", 1f);
            if (_material.HasProperty("_SrcBlend")) _material.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (_material.HasProperty("_DstBlend")) _material.SetFloat("_DstBlend", (float)BlendMode.One);
            if (_material.HasProperty("_ZWrite")) _material.SetFloat("_ZWrite", 0f);
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.SetOverrideTag("RenderType", "Transparent");
            _material.renderQueue = (int)RenderQueue.Transparent;
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", Color.white);
            if (_material.HasProperty("_Color")) _material.SetColor("_Color", Color.white);
            _sparkTexture = CreateSparkTexture();
            if (_material.HasProperty("_BaseMap")) _material.SetTexture("_BaseMap", _sparkTexture);
            if (_material.HasProperty("_MainTex")) _material.SetTexture("_MainTex", _sparkTexture);
            ParticleSystemRenderer rendererComponent = _particles.GetComponent<ParticleSystemRenderer>();
            rendererComponent.sharedMaterial = _material;
            rendererComponent.renderMode = ParticleSystemRenderMode.Stretch;
            rendererComponent.lengthScale = 1.35f;
            rendererComponent.velocityScale = 0.038f;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;

            GameObject ringObject = new GameObject("Blade contact shock");
            ringObject.transform.SetParent(transform, false);
            _impactRing = ringObject.AddComponent<CombatImpactRing>();
            _impactRing.Configure();
        }

        private void OnCombatChanged(Combatant combatant)
        {
            if (_particles == null)
            {
                return;
            }

            bool eventChanged = combatant.LastEvent != _previousEvent;
            if (combatant.LastEvent == "拼刀" && _previousEvent != "拼刀")
            {
                EmitBurst(28, new Color(1f, 0.88f, 0.46f, 1f), 5.1f);
            }
            else if (eventChanged && combatant.LastEvent.Contains("抗衡得势"))
            {
                EmitBurst(24, new Color(0.46f, 0.86f, 1f, 1f), 3.7f);
            }
            else if (eventChanged && combatant.LastEvent == "迎推·借线")
            {
                EmitBurst(20, new Color(0.62f, 0.9f, 1f, 0.92f), 2.9f);
            }
            else if (eventChanged && combatant.LastEvent.StartsWith("迎推"))
            {
                EmitBurst(34, new Color(0.88f, 0.94f, 1f, 1f), 5.4f);
            }
            else if (eventChanged && combatant.LastEvent.StartsWith("回锋"))
            {
                EmitBurst(combatant.LastEvent.Contains("候隙") ? 16 : 32,
                    new Color(0.42f, 0.82f, 0.92f, 1f),
                    combatant.LastEvent.Contains("候隙") ? 2.4f : 4.9f);
            }
            else if (eventChanged && combatant.LastEvent.StartsWith("震烈"))
            {
                EmitBurst(combatant.LastEvent.Contains("蓄势") ? 22 : 52,
                    new Color(1f, 0.34f, 0.055f, 1f),
                    combatant.LastEvent.Contains("蓄势") ? 3f : 6.1f);
            }
            else if (eventChanged && combatant.LastEvent.StartsWith("背袭·"))
            {
                EmitBurst(27, new Color(1f, 0.2f, 0.045f, 1f), 5.2f);
            }
            else if (combatant.Health < _previousHealth)
            {
                EmitBurst(19, new Color(1f, 0.13f, 0.035f, 1f), 4.4f);
            }
            else if (combatant.Posture > _previousPosture + 0.01f)
            {
                EmitBurst(combatant.IsStaggered && !_wasStaggered ? 22 : 9,
                    combatant.IsStaggered ? new Color(1f, 0.34f, 0.035f, 1f) : _accent, 3f);
            }

            _previousHealth = combatant.Health;
            _previousPosture = combatant.Posture;
            _wasStaggered = combatant.IsStaggered;
            _previousEvent = combatant.LastEvent;
        }

        private void EmitBurst(int count, Color color, float speed)
        {
            Vector3 origin = transform.position + Vector3.up * 0.95f;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.PI * 2f / count) + (i % 3) * 0.17f;
                float lift = 0.35f + (i % 4) * 0.13f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), lift, Mathf.Sin(angle)).normalized;
                var parameters = new ParticleSystem.EmitParams
                {
                    position = origin,
                    velocity = direction * (speed * (0.72f + (i % 5) * 0.08f)),
                    startColor = color,
                    startLifetime = 0.2f + (i % 4) * 0.055f,
                    startSize = 0.045f + (i % 3) * 0.024f
                };
                _particles.Emit(parameters, 1);
            }
        }

        public void PlayWeaponContact(Vector3 position, Vector3 bladeDirection, CombatOutcome outcome)
        {
            if (_particles == null) return;
            _weaponContactCount++;
            _lastWeaponContactPosition = position;
            bool metalContact = outcome == CombatOutcome.Clashed || outcome == CombatOutcome.Parried ||
                                outcome == CombatOutcome.Guarded;
            Color color = outcome == CombatOutcome.Parried ? new Color(0.62f, 0.86f, 1f, 0.95f)
                : metalContact ? new Color(1f, 0.86f, 0.42f, 1f)
                : outcome == CombatOutcome.PostureBroken ? new Color(1f, 0.32f, 0.055f, 1f)
                : new Color(1f, 0.18f, 0.045f, 1f);
            int count = outcome == CombatOutcome.Parried ? 34
                : metalContact ? 48
                : outcome == CombatOutcome.PostureBroken ? 38 : 24;
            float speed = outcome == CombatOutcome.Parried ? 8.4f : metalContact ? 7.8f : 5.6f;
            Vector3 tangent = bladeDirection.sqrMagnitude > 0.001f ? bladeDirection.normalized : transform.right;
            Vector3 normal = Vector3.Cross(tangent, Vector3.up);
            if (normal.sqrMagnitude < 0.01f) normal = transform.forward;
            normal.Normalize();
            for (int i = 0; i < count; i++)
            {
                float signedSpread = ((i % 7) - 3f) / 3f;
                float side = i % 2 == 0 ? 1f : -1f;
                float azimuth = i * 2.39996f + (i % 3) * 0.11f;
                Vector3 radial = new Vector3(
                    Mathf.Cos(azimuth), 0.12f + (i % 5) * 0.075f, Mathf.Sin(azimuth));
                Vector3 velocityDirection = (radial * 0.82f +
                                             normal * side * 0.14f +
                                             tangent * signedSpread * 0.2f).normalized;
                var parameters = new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = velocityDirection * (speed * (0.76f + (i % 4) * 0.09f)),
                    startColor = color,
                    startLifetime = 0.16f + (i % 5) * 0.04f,
                    startSize = i < 3 ? 0.025f : 0.01f + (i % 3) * 0.004f
                };
                _particles.Emit(parameters, 1);
            }
            _impactRing?.Play(position, outcome);
        }

        private static Texture2D CreateSparkTexture()
        {
            const int width = 64;
            const int height = 8;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime tapered spark",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
            {
                float vertical = 1f - Mathf.Abs((y + 0.5f) / height * 2f - 1f);
                vertical = vertical * vertical * vertical;
                for (int x = 0; x < width; x++)
                {
                    float along = x / (float)(width - 1);
                    float taper = Mathf.Sin(along * Mathf.PI);
                    float alpha = vertical * taper * taper;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (_combatant != null) _combatant.Changed -= OnCombatChanged;
            if (_material != null) Destroy(_material);
            if (_sparkTexture != null) Destroy(_sparkTexture);
        }
    }
}

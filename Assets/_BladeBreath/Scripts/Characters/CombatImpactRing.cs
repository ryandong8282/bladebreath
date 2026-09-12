using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    // A short, camera-facing shock ring placed at the resolved blade contact.
    // CombatFeedback owns and reuses it, so clashes do not allocate scene objects.
    public sealed class CombatImpactRing : MonoBehaviour
    {
        private const int SegmentCount = 64;

        private LineRenderer _outerRing;
        private Material _outerMaterial;
        private MeshRenderer _rayBurst;
        private Material _rayMaterial;
        private Mesh _rayMesh;
        private Camera _camera;
        private float _startedAt;
        private float _duration;
        private Color _color;
        private Vector3 _worldPosition;
        private bool _playing;

        public bool IsPlaying => _playing;

        public void Configure()
        {
            if (_outerRing != null) return;
            _camera = Camera.main;
            _outerRing = CreateRing("Contact shockwave", out _outerMaterial, 0.018f);
            _rayBurst = CreateRayBurst();
            gameObject.SetActive(false);
        }

        public void Play(Vector3 position, CombatOutcome outcome)
        {
            if (_outerRing == null) Configure();
            bool parry = outcome == CombatOutcome.Parried;
            bool metal = parry || outcome == CombatOutcome.Clashed || outcome == CombatOutcome.Guarded;
            _color = parry
                ? new Color(0.72f, 0.9f, 1f, 1f)
                : metal
                    ? new Color(1f, 0.76f, 0.3f, 1f)
                    : new Color(1f, 0.22f, 0.04f, 1f);
            _duration = parry ? 0.28f : 0.22f;
            _startedAt = Time.unscaledTime;
            _worldPosition = position;
            transform.position = position;
            transform.localScale = Vector3.one * 0.2f;
            gameObject.SetActive(true);
            _playing = true;
            ApplyColor(_outerMaterial, new Color(_color.r * 1.3f, _color.g * 1.3f, _color.b * 1.3f, 0.46f));
            ApplyColor(_rayMaterial, new Color(1.45f, 1.7f, 2f, 0.62f));
        }

        private void LateUpdate()
        {
            if (!_playing) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera != null)
            {
                transform.rotation = _camera.transform.rotation;
                transform.position = _worldPosition - _camera.transform.forward * 0.065f;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Mathf.Max(_duration, 0.01f));
            float eased = 1f - (1f - progress) * (1f - progress);
            float alpha = (1f - progress) * (1f - progress);
            transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 1.25f, eased);
            _rayBurst.transform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.08f, eased);
            ApplyColor(_outerMaterial, new Color(
                _color.r * 1.3f, _color.g * 1.3f, _color.b * 1.3f, alpha * 0.46f));
            ApplyColor(_rayMaterial, new Color(1.45f, 1.7f, 2f, alpha * 0.56f));

            if (progress >= 1f)
            {
                _playing = false;
                gameObject.SetActive(false);
            }
        }

        private LineRenderer CreateRing(string objectName, out Material material, float width)
        {
            GameObject ringObject = new GameObject(objectName);
            ringObject.transform.SetParent(transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = SegmentCount;
            ring.startWidth = width;
            ring.endWidth = width;
            ring.numCornerVertices = 2;
            ring.numCapVertices = 2;
            ring.textureMode = LineTextureMode.Stretch;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i = 0; i < SegmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / SegmentCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }

            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                : Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader) { name = objectName + " material" };
            ConfigureTransparent(material, true);
            ring.sharedMaterial = material;
            return ring;
        }

        private MeshRenderer CreateRayBurst()
        {
            const int rayCount = 16;
            var vertices = new Vector3[rayCount * 4];
            var triangles = new int[rayCount * 6];
            var colors = new Color[rayCount * 4];
            for (int ray = 0; ray < rayCount; ray++)
            {
                float angle = ray * 2.39996f + Mathf.Sin(ray * 7.13f) * 0.18f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                float innerRadius = 0.08f + (ray % 3) * 0.018f;
                float outerRadius = 0.34f + (ray % 5) * 0.075f + Mathf.Abs(Mathf.Sin(ray * 2.31f)) * 0.08f;
                float innerWidth = 0.0045f + (ray % 2) * 0.0012f;
                float outerWidth = 0.0015f + (ray % 3) * 0.00065f;
                Vector2 inner = direction * innerRadius;
                Vector2 outer = direction * outerRadius;
                int vertex = ray * 4;
                vertices[vertex] = new Vector3(inner.x + tangent.x * innerWidth, inner.y + tangent.y * innerWidth, 0.003f);
                vertices[vertex + 1] = new Vector3(inner.x - tangent.x * innerWidth, inner.y - tangent.y * innerWidth, 0.003f);
                vertices[vertex + 2] = new Vector3(outer.x + tangent.x * outerWidth, outer.y + tangent.y * outerWidth, 0.003f);
                vertices[vertex + 3] = new Vector3(outer.x - tangent.x * outerWidth, outer.y - tangent.y * outerWidth, 0.003f);
                float rayAlpha = 0.32f + (ray % 4) * 0.11f;
                colors[vertex] = colors[vertex + 1] = new Color(0.82f, 0.94f, 1f, rayAlpha);
                colors[vertex + 2] = colors[vertex + 3] = new Color(0.68f, 0.82f, 1f, rayAlpha * 0.22f);
                int triangle = ray * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            _rayMesh = new Mesh { name = "Runtime parry rays" };
            _rayMesh.vertices = vertices;
            _rayMesh.triangles = triangles;
            _rayMesh.colors = colors;
            _rayMesh.RecalculateBounds();
            GameObject rayObject = new GameObject("Radial contact rays");
            rayObject.transform.SetParent(transform, false);
            rayObject.AddComponent<MeshFilter>().sharedMesh = _rayMesh;
            MeshRenderer rendererComponent = rayObject.AddComponent<MeshRenderer>();
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                : Shader.Find("Unlit/Color");
            _rayMaterial = new Material(shader) { name = "Radial contact ray material" };
            ConfigureTransparent(_rayMaterial, true);
            rendererComponent.sharedMaterial = _rayMaterial;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;
            return rendererComponent;
        }

        private static void ConfigureTransparent(Material material, bool additive)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private void OnDestroy()
        {
            if (_outerMaterial != null) Destroy(_outerMaterial);
            if (_rayMaterial != null) Destroy(_rayMaterial);
            if (_rayMesh != null) Destroy(_rayMesh);
        }
    }
}

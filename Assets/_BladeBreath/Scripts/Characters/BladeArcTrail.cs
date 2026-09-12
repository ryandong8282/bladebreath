using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    // Builds one translucent ribbon from the blade base and tip history. This follows
    // the complete blade segment instead of drawing an unrelated line behind its tip.
    [DefaultExecutionOrder(100)]
    public sealed class BladeArcTrail : MonoBehaviour
    {
        private readonly List<Sample> _samples = new List<Sample>(24);
        private Transform _bladeBase;
        private Transform _bladeTip;
        private Mesh _mesh;
        private Material _material;
        private Color _edgeColor;
        private float _lifetime = 0.13f;
        private bool _emitting;

        public Transform BladeBase => _bladeBase;
        public Transform BladeTip => _bladeTip;
        public bool IsEmitting => _emitting;
        public int SampleCount => _samples.Count;

        public void Configure(Transform bladeBase, Transform bladeTip, Color edgeColor)
        {
            _bladeBase = bladeBase;
            _bladeTip = bladeTip;
            _edgeColor = edgeColor;

            var filter = gameObject.AddComponent<MeshFilter>();
            var rendererComponent = gameObject.AddComponent<MeshRenderer>();
            _mesh = new Mesh { name = "Runtime blade arc mesh" };
            _mesh.MarkDynamic();
            filter.sharedMesh = _mesh;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _material = new Material(shader) { name = "Runtime blade arc material" };
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", Color.white);
            if (_material.HasProperty("_Color")) _material.SetColor("_Color", Color.white);
            rendererComponent.sharedMaterial = _material;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;
            rendererComponent.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        public void SetEmitting(bool emitting, float lifetime = 0.13f)
        {
            if (emitting && !_emitting) Clear();
            _emitting = emitting;
            _lifetime = Mathf.Clamp(lifetime, 0.06f, 0.24f);
        }

        private void LateUpdate()
        {
            if (_bladeBase == null || _bladeTip == null || _mesh == null) return;
            float now = Time.unscaledTime;
            if (_emitting)
            {
                Vector3 bladeBase = transform.InverseTransformPoint(_bladeBase.position);
                Vector3 bladeTip = transform.InverseTransformPoint(_bladeTip.position);
                if (_samples.Count == 0 ||
                    Vector3.Distance(_samples[_samples.Count - 1].Tip, bladeTip) > 0.018f)
                    _samples.Add(new Sample(bladeBase, bladeTip, now));
            }

            while (_samples.Count > 0 && now - _samples[0].Time > _lifetime)
                _samples.RemoveAt(0);
            while (_samples.Count > 22)
                _samples.RemoveAt(0);
            Rebuild(now);
        }

        private void Rebuild(float now)
        {
            _mesh.Clear(false);
            if (_samples.Count < 2) return;

            int count = _samples.Count;
            var vertices = new Vector3[count * 2];
            var colors = new Color[count * 2];
            var uv = new Vector2[count * 2];
            var triangles = new int[(count - 1) * 6];
            for (int i = 0; i < count; i++)
            {
                Sample sample = _samples[i];
                float fade = 1f - Mathf.Clamp01((now - sample.Time) / _lifetime);
                float head = Mathf.SmoothStep(0.25f, 1f, i / Mathf.Max(1f, count - 1f));
                // Keep the ribbon on the cutting half of the weapon. Filling the
                // full base-to-tip span produced a broad opaque fan at game speed.
                vertices[i * 2] = Vector3.Lerp(sample.Base, sample.Tip, 0.52f);
                vertices[i * 2 + 1] = sample.Tip;
                colors[i * 2] = new Color(_edgeColor.r, _edgeColor.g, _edgeColor.b, _edgeColor.a * fade * head * 0.045f);
                colors[i * 2 + 1] = new Color(_edgeColor.r, _edgeColor.g, _edgeColor.b,
                    _edgeColor.a * fade * head * 0.4f);
                uv[i * 2] = new Vector2(0f, i / Mathf.Max(1f, count - 1f));
                uv[i * 2 + 1] = new Vector2(1f, i / Mathf.Max(1f, count - 1f));
                if (i >= count - 1) continue;
                int triangle = i * 6;
                int vertex = i * 2;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
            _mesh.vertices = vertices;
            _mesh.colors = colors;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }

        private void Clear()
        {
            _samples.Clear();
            if (_mesh != null) _mesh.Clear(false);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }

        private readonly struct Sample
        {
            public Sample(Vector3 bladeBase, Vector3 bladeTip, float time)
            {
                Base = bladeBase;
                Tip = bladeTip;
                Time = time;
            }

            public Vector3 Base { get; }
            public Vector3 Tip { get; }
            public float Time { get; }
        }
    }
}

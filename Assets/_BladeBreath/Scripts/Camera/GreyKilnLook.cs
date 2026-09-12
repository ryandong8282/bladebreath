using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BladeBreath
{
    // Owns the one runtime post-processing profile used by the combat sandbox.
    // It changes presentation only and is safe to configure again after a reset.
    [RequireComponent(typeof(Camera))]
    public sealed class GreyKilnLook : MonoBehaviour
    {
        private Volume _volume;
        private VolumeProfile _profile;

        public bool IsConfigured => _profile != null;

        public void Configure()
        {
            Camera cameraComponent = GetComponent<Camera>();
            UniversalAdditionalCameraData cameraData =
                cameraComponent.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = cameraComponent.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            if (_volume == null)
            {
                _volume = GetComponent<Volume>();
                if (_volume == null) _volume = gameObject.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 20f;
            }

            if (_profile != null) Destroy(_profile);
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "Runtime Grey Kiln Look";
            _volume.sharedProfile = _profile;

            Tonemapping tonemapping = _profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = _profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(1f);
            color.contrast.Override(12f);
            color.saturation.Override(-12f);
            color.colorFilter.Override(new Color(0.97f, 0.95f, 0.9f, 1f));

            Bloom bloom = _profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.28f);
            bloom.scatter.Override(0.52f);
            bloom.tint.Override(new Color(1f, 0.72f, 0.46f, 1f));

            Vignette vignette = _profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.012f, 0.009f, 0.008f, 1f));
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.78f);
            vignette.rounded.Override(false);
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
        }
    }
}

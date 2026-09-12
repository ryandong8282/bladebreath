using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    // Builds a camera-matched 2.5D environment from an authored colour plate and
    // its relative inverse-depth map. The mesh stays in world space so the fixed
    // combat camera gets restrained parallax while fighters remain fully 3D.
    public sealed class DepthReconstructedBackdrop : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _material;

        public void Build(Camera sourceCamera, Texture2D albedo, Texture2D inverseDepth)
        {
            if (sourceCamera == null || albedo == null || inverseDepth == null)
            {
                Debug.LogError("Depth backdrop needs a camera, colour plate and readable depth texture.", this);
                enabled = false;
                return;
            }

            const int columns = 128;
            int rows = Mathf.Max(2, Mathf.RoundToInt(columns / sourceCamera.aspect));
            var vertices = new Vector3[columns * rows];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(columns - 1) * (rows - 1) * 6];
            float tangent = Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            const float nearDistance = 12f;
            const float farDistance = 32f;
            const float overscan = 1.035f;

            for (int row = 0; row < rows; row++)
            {
                float v = row / (float)(rows - 1);
                for (int column = 0; column < columns; column++)
                {
                    float u = column / (float)(columns - 1);
                    float relativeInverseDepth = Mathf.Clamp01(inverseDepth.GetPixelBilinear(u, v).r);
                    float distance = nearDistance +
                        Mathf.Pow(1f - relativeInverseDepth, 1.35f) * (farDistance - nearDistance);
                    float halfHeight = tangent * distance * overscan;
                    float halfWidth = halfHeight * sourceCamera.aspect;
                    int index = row * columns + column;
                    vertices[index] = new Vector3(
                        (u * 2f - 1f) * halfWidth,
                        (v * 2f - 1f) * halfHeight,
                        distance);
                    uv[index] = new Vector2(u, v);
                }
            }

            int triangle = 0;
            for (int row = 0; row < rows - 1; row++)
            {
                for (int column = 0; column < columns - 1; column++)
                {
                    int bottomLeft = row * columns + column;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + columns;
                    int topRight = topLeft + 1;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomRight;
                    triangles[triangle++] = bottomRight;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = topRight;
                }
            }

            _mesh = new Mesh { name = "Grey Kiln depth-reconstructed backdrop" };
            _mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                Debug.LogError("No unlit shader is available for the depth-reconstructed backdrop.", this);
                enabled = false;
                return;
            }

            _material = new Material(shader) { name = "Grey Kiln depth plate material" };
            if (_material.HasProperty("_BaseMap")) _material.SetTexture("_BaseMap", albedo);
            if (_material.HasProperty("_MainTex")) _material.SetTexture("_MainTex", albedo);
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", Color.white);
            if (_material.HasProperty("_Cull")) _material.SetFloat("_Cull", (float)CullMode.Off);
            _material.renderQueue = (int)RenderQueue.Geometry - 25;

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }
    }
}

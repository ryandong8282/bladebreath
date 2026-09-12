using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BladeBreathEditor
{
    // Produces a temporary, normalized rest-pose reference for Blender fitting.
    // The OBJ and JSON stay under Logs and are not runtime assets.
    public static class RestPoseObjExporter
    {
        [MenuItem("BladeBreath/Art/Export Blender Fitting Reference")]
        public static void Export()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidAssetImporter.ModelPath);
            if (source == null) throw new FileNotFoundException("Missing MakeHuman source model.", HumanoidAssetImporter.ModelPath);

            GameObject instance = Object.Instantiate(source);
            instance.name = "MakeHumanRestReference";
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 1.8f / bounds.size.y;
                instance.transform.localScale = Vector3.one * scale;
                instance.transform.position = Vector3.up * (-bounds.min.y * scale);

                string root = Environment.GetEnvironmentVariable("BLADEBREATH_REFERENCE_ROOT");
                if (string.IsNullOrWhiteSpace(root)) root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string logs = Path.Combine(root, "Logs");
                Directory.CreateDirectory(logs);
                WriteObj(instance, Path.Combine(logs, "makehuman-rest-reference.obj"));
                WriteBones(instance, Path.Combine(logs, "makehuman-rest-bones.json"));
                Debug.Log("Blender fitting reference exported to " + logs);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void WriteObj(GameObject root, string path)
        {
            var text = new StringBuilder(4 * 1024 * 1024);
            text.AppendLine("# BladeBreath temporary MakeHuman rest-pose fitting reference");
            int vertexOffset = 1;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Mesh mesh = null;
                bool temporary = false;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    mesh = new Mesh();
                    skinned.BakeMesh(mesh, false);
                    temporary = true;
                }
                else
                {
                    mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                }
                if (mesh == null) continue;

                text.AppendLine("o " + SafeName(renderer.name));
                Matrix4x4 matrix = renderer.localToWorldMatrix;
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertex);
                    text.AppendFormat(CultureInfo.InvariantCulture, "v {0:R} {1:R} {2:R}\n", point.x, point.y, point.z);
                }
                int[] triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                    text.AppendFormat(CultureInfo.InvariantCulture, "f {0} {1} {2}\n",
                        vertexOffset + triangles[i], vertexOffset + triangles[i + 1], vertexOffset + triangles[i + 2]);
                vertexOffset += mesh.vertexCount;
                if (temporary) Object.DestroyImmediate(mesh);
            }
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static void WriteBones(GameObject root, string path)
        {
            string[] wanted =
            {
                "hips", "spine", "chest", "chest-1", "neck", "head",
                "clavicle.L", "clavicle.R", "upper_arm.L", "upper_arm.R",
                "forearm.L", "forearm.R", "hand.L", "hand.R",
                "thigh.L", "thigh.R", "shin.L", "shin.R", "foot.L", "foot.R"
            };
            var transforms = root.GetComponentsInChildren<Transform>()
                .Where(item => wanted.Contains(item.name, StringComparer.OrdinalIgnoreCase))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            var text = new StringBuilder();
            text.AppendLine("{");
            for (int i = 0; i < transforms.Length; i++)
            {
                Vector3 point = transforms[i].position;
                text.AppendFormat(CultureInfo.InvariantCulture,
                    "  \"{0}\": [{1:R}, {2:R}, {3:R}]{4}\n",
                    transforms[i].name, point.x, point.y, point.z, i + 1 == transforms.Length ? string.Empty : ",");
            }
            text.AppendLine("}");
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static string SafeName(string value)
        {
            return new string(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        }
    }
}

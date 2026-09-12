using System;
using System.IO;
using System.Linq;
using BladeBreath;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BladeBreathEditor
{
    public static class PrototypeScreenshot
    {
        internal const string OutputPath = "Logs/grey-kiln-character-surfaces-actions.png";
        internal const string CurrentViewRequestPath = "Logs/capture-current-game-view.request";
        internal const string CloseupOutputPath = "Logs/grey-kiln-character-texture-closeup.png";
        internal const string CloseupRequestPath = "Logs/capture-fighter-texture-closeup.request";
        internal const string CharacterProofOutputPath = "Logs/realistic-character-proof.png";
        internal const string ActionProofFolder = "Logs/action-module-proof-frames";

        private static int _readyFrames;
        private static int _currentViewReadyFrames;
        private static bool _replaceSceneForProof;

        [InitializeOnLoadMethod]
        private static void ResumeQueuedCloseupCapture()
        {
            if (!File.Exists(CloseupRequestPath)) return;
            _readyFrames = 0;
            EditorApplication.update -= ProcessQueuedCloseupCapture;
            EditorApplication.update += ProcessQueuedCloseupCapture;
        }

        // File requests make capture deterministic even when editor shortcuts conflict.
        [InitializeOnLoadMethod]
        private static void ResumeQueuedCurrentViewCapture()
        {
            if (!File.Exists(CurrentViewRequestPath)) return;
            _currentViewReadyFrames = 0;
            EditorApplication.update -= ProcessQueuedCurrentViewCapture;
            EditorApplication.update += ProcessQueuedCurrentViewCapture;
        }

        private static void ProcessQueuedCurrentViewCapture()
        {
            if (!File.Exists(CurrentViewRequestPath))
            {
                EditorApplication.update -= ProcessQueuedCurrentViewCapture;
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return;

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<PlayerController>() == null || Camera.main == null)
            {
                _currentViewReadyFrames = 0;
                return;
            }

            if (++_currentViewReadyFrames < 24) return;
            File.Delete(CurrentViewRequestPath);
            EditorApplication.update -= ProcessQueuedCurrentViewCapture;
            CaptureCurrentGameView();
        }

        private static void ProcessQueuedCloseupCapture()
        {
            if (!File.Exists(CloseupRequestPath))
            {
                EditorApplication.update -= ProcessQueuedCloseupCapture;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return;

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<PlayerController>() == null ||
                UnityEngine.Object.FindFirstObjectByType<EnemyController>() == null || Camera.main == null)
            {
                _readyFrames = 0;
                return;
            }

            if (++_readyFrames < 36) return;
            File.Delete(CloseupRequestPath);
            EditorApplication.update -= ProcessQueuedCloseupCapture;
            CaptureFighterTextureCloseup();
        }

        [MenuItem("BladeBreath/Art/Capture Current Game View")]
        public static void CaptureCurrentGameView()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before capturing the current Game view.");
            Directory.CreateDirectory("Logs");
            string absolutePath = Path.GetFullPath(OutputPath);
            ScreenCapture.CaptureScreenshot(absolutePath, 1);
            Debug.Log("Queued Game view screenshot: " + absolutePath);
        }

        [MenuItem("BladeBreath/Art/Capture Fighter Texture Closeup _F10")]
        public static void CaptureFighterTextureCloseup()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before capturing a fighter closeup.");
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            EnemyController enemy = UnityEngine.Object.FindFirstObjectByType<EnemyController>();
            Camera camera = Camera.main;
            if (player == null || enemy == null || camera == null)
                throw new InvalidOperationException("The running HumanoidSandbox needs both fighters and a Main Camera.");

            TopDownCamera follow = camera.GetComponent<TopDownCamera>();
            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            float originalSize = camera.orthographicSize;
            RenderTexture originalTarget = camera.targetTexture;
            RenderTexture originalActive = RenderTexture.active;
            bool followWasEnabled = follow != null && follow.enabled;
            var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                if (follow != null) follow.enabled = false;
                Vector3 center = (player.transform.position + enemy.transform.position) * 0.5f + Vector3.up * 0.95f;
                camera.transform.position = center + new Vector3(0f, 3.3f, -5.4f);
                camera.transform.LookAt(center + Vector3.up * 0.05f, Vector3.up);
                camera.orthographicSize = 2.35f;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
                image.Apply(false, false);
                Directory.CreateDirectory("Logs");
                string absolutePath = Path.GetFullPath(CloseupOutputPath);
                File.WriteAllBytes(absolutePath, image.EncodeToPNG());
                Debug.Log("Captured fighter texture closeup: " + absolutePath);
            }
            finally
            {
                camera.targetTexture = originalTarget;
                RenderTexture.active = originalActive;
                camera.transform.position = originalPosition;
                camera.transform.rotation = originalRotation;
                camera.orthographicSize = originalSize;
                if (follow != null) follow.enabled = followWasEnabled;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        [MenuItem("BladeBreath/Art/Render Realistic Character Proof")]
        public static void RenderRealisticCharacterProof()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rendering the character proof.");
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidSandboxBuilder.PlayerPath);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidSandboxBuilder.EnemyPath);
            if (playerPrefab == null || enemyPrefab == null)
                throw new InvalidOperationException("Build the realistic fighter prefabs before rendering their proof image.");

            // A normal additive scene is required for Camera.Render. Preview-scene
            // objects are intentionally excluded by parts of the render pipeline.
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                _replaceSceneForProof ? NewSceneMode.Single : NewSceneMode.Additive);
            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbient = RenderSettings.ambientLight;
            var createdMaterials = new System.Collections.Generic.List<Material>();
            try
            {
                GameObject player = UnityEngine.Object.Instantiate(playerPrefab);
                GameObject enemy = UnityEngine.Object.Instantiate(enemyPrefab);
                SceneManager.MoveGameObjectToScene(player, preview);
                SceneManager.MoveGameObjectToScene(enemy, preview);
                player.transform.SetPositionAndRotation(new Vector3(-0.72f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f));
                enemy.transform.SetPositionAndRotation(new Vector3(0.72f, 0f, 0.08f), Quaternion.Euler(0f, 180f, 0f));
                PoseForProof(player);
                PoseForProof(enemy);
                player.GetComponent<FighterPresentation>().PreviewGuardPose(false);
                enemy.GetComponent<FighterPresentation>().PreviewAttackPose(3, 0.54f);

                Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline != null
                    ? "Universal Render Pipeline/Lit"
                    : "Standard");
                if (shader == null) throw new InvalidOperationException("The active pipeline has no Lit shader for the proof floor.");
                var floorMaterial = new Material(shader) { name = "Proof floor" };
                createdMaterials.Add(floorMaterial);
                if (floorMaterial.HasProperty("_BaseColor"))
                    floorMaterial.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.145f));
                if (floorMaterial.HasProperty("_Color"))
                    floorMaterial.SetColor("_Color", new Color(0.12f, 0.13f, 0.145f));
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Proof floor";
                floor.transform.SetPositionAndRotation(new Vector3(0f, -0.02f, 0.15f), Quaternion.identity);
                floor.transform.localScale = new Vector3(0.65f, 1f, 0.55f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                SceneManager.MoveGameObjectToScene(floor, preview);

                CreateProofLight(preview, "Cool key", LightType.Directional,
                    new Color(1f, 0.93f, 0.86f), 1.35f, new Vector3(2f, 4f, -3f), Quaternion.Euler(38f, -28f, 0f));
                CreateProofLight(preview, "Warm rim", LightType.Point,
                    new Color(1f, 0.34f, 0.1f), 0.8f, new Vector3(1.7f, 1.7f, 0.25f), Quaternion.identity, 4.5f);

                var cameraObject = new GameObject("Proof camera");
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.04f, 0.052f);
                camera.fieldOfView = 29f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 30f;
                camera.transform.position = new Vector3(0f, 1.28f, -4.4f);
                camera.transform.LookAt(new Vector3(0f, 0.95f, 0f), Vector3.up);

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.28f, 0.26f, 0.25f);
                Directory.CreateDirectory("Logs");
                RenderCameraToPng(camera, Path.GetFullPath(CharacterProofOutputPath), 1920, 1080);
                Debug.Log("Rendered realistic character proof: " + Path.GetFullPath(CharacterProofOutputPath));
            }
            finally
            {
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbient;
                foreach (Material material in createdMaterials) UnityEngine.Object.DestroyImmediate(material);
                if (!_replaceSceneForProof) EditorSceneManager.CloseScene(preview, true);
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void RenderRealisticCharacterProofAndExit()
        {
            _replaceSceneForProof = true;
            try
            {
                RenderRealisticCharacterProof();
            }
            finally
            {
                _replaceSceneForProof = false;
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("BladeBreath/Art/Render Action Module Proof")]
        public static void RenderActionModuleProofAndExit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rendering the action module proof.");
            string previousScenePath = SceneManager.GetActiveScene().path;
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidSandboxBuilder.PlayerPath);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidSandboxBuilder.EnemyPath);
            if (playerPrefab == null || enemyPrefab == null)
                throw new InvalidOperationException("Build the fighter prefabs before rendering the action module proof.");

            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbient = RenderSettings.ambientLight;
            var createdMaterials = new System.Collections.Generic.List<Material>();
            var fighters = new System.Collections.Generic.List<GameObject>();
            try
            {
                Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline != null
                    ? "Universal Render Pipeline/Lit"
                    : "Standard");
                if (shader == null) throw new InvalidOperationException("The active pipeline has no Lit shader for action proofing.");
                var floorMaterial = new Material(shader) { name = "Action proof floor" };
                createdMaterials.Add(floorMaterial);
                if (floorMaterial.HasProperty("_BaseColor"))
                    floorMaterial.SetColor("_BaseColor", new Color(0.105f, 0.115f, 0.13f));
                if (floorMaterial.HasProperty("_Color"))
                    floorMaterial.SetColor("_Color", new Color(0.105f, 0.115f, 0.13f));
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Action proof floor";
                floor.transform.localScale = new Vector3(0.38f, 1f, 0.32f);
                floor.transform.position = new Vector3(0f, -0.02f, 0.12f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                SceneManager.MoveGameObjectToScene(floor, preview);

                CreateProofLight(preview, "Action key", LightType.Directional,
                    new Color(1f, 0.94f, 0.86f), 1.45f, new Vector3(2f, 4f, -3f), Quaternion.Euler(38f, -28f, 0f));
                CreateProofLight(preview, "Action rim", LightType.Point,
                    new Color(1f, 0.32f, 0.08f), 0.72f, new Vector3(1.6f, 1.55f, 0.1f), Quaternion.identity, 4f);

                var cameraObject = new GameObject("Action proof camera");
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.026f, 0.031f, 0.042f);
                camera.fieldOfView = 32f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 30f;
                camera.transform.position = new Vector3(0f, 1.25f, -4.1f);
                camera.transform.LookAt(new Vector3(0f, 0.9f, 0f), Vector3.up);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.28f, 0.26f, 0.25f);
                Directory.CreateDirectory(ActionProofFolder);
                foreach (string staleFrame in Directory.GetFiles(ActionProofFolder, "*.png"))
                    File.Delete(staleFrame);

                FighterPresentation Spawn(GameObject prefab, Vector3 position, float yaw)
                {
                    GameObject fighter = UnityEngine.Object.Instantiate(prefab);
                    fighter.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                    SceneManager.MoveGameObjectToScene(fighter, preview);
                    PoseForProof(fighter);
                    fighters.Add(fighter);
                    return fighter.GetComponent<FighterPresentation>();
                }

                void ClearFighters()
                {
                    foreach (GameObject fighter in fighters) UnityEngine.Object.DestroyImmediate(fighter);
                    fighters.Clear();
                }

                void RenderFrame(string fileName)
                {
                    Bounds bounds = default;
                    bool hasBounds = false;
                    foreach (GameObject fighter in fighters)
                    {
                        foreach (Renderer rendererComponent in fighter.GetComponentsInChildren<Renderer>(true))
                        {
                            if (!hasBounds)
                            {
                                bounds = rendererComponent.bounds;
                                hasBounds = true;
                            }
                            else bounds.Encapsulate(rendererComponent.bounds);
                        }
                    }
                    if (hasBounds)
                    {
                        float tangent = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                        float verticalDistance = bounds.extents.y / Mathf.Max(tangent, 0.01f);
                        float horizontalDistance = bounds.extents.x /
                                                   Mathf.Max(tangent * (1280f / 720f), 0.01f);
                        float distance = Mathf.Max(verticalDistance, horizontalDistance) +
                                         bounds.extents.z + 0.72f;
                        Vector3 focus = bounds.center + Vector3.up * 0.04f;
                        camera.transform.position = focus + Vector3.back * distance;
                        camera.transform.LookAt(focus, Vector3.up);
                    }
                    RenderCameraToPng(camera, Path.GetFullPath(ActionProofFolder + "/" + fileName), 1280, 720);
                    ClearFighters();
                }

                Spawn(playerPrefab, Vector3.zero, 180f).PreviewAttackPose(1, 0.5f);
                RenderFrame("01-rising-cut.png");
                Spawn(playerPrefab, Vector3.zero, 180f).PreviewAttackPose(2, 0.5f);
                RenderFrame("02-reversing-cut.png");
                Spawn(playerPrefab, Vector3.zero, 180f).PreviewAttackPose(3, 0.2f);
                RenderFrame("03-turning-finisher.png");
                Spawn(playerPrefab, Vector3.zero, 180f).PreviewAttackPose(1, 0.42f, true);
                RenderFrame("04-dash-cut.png");

                FighterPresentation guard = Spawn(playerPrefab, new Vector3(-0.72f, 0f, 0f), 90f);
                FighterPresentation parryTarget = Spawn(enemyPrefab, new Vector3(0.72f, 0f, 0f), 270f);
                guard.PreviewGuardPose(true);
                parryTarget.PreviewAttackPose(1, 0.5f);
                RenderFrame("05-guard-parry.png");

                FighterPresentation firstBlade = Spawn(playerPrefab, new Vector3(-0.72f, 0f, 0f), 90f);
                FighterPresentation secondBlade = Spawn(enemyPrefab, new Vector3(0.72f, 0f, 0f), 270f);
                firstBlade.PreviewAttackPose(1, 0.5f);
                secondBlade.PreviewAttackPose(1, 0.5f);
                RenderFrame("06-clash.png");

                Spawn(playerPrefab, Vector3.zero, 180f).PreviewDodgePose(-1f, 0.5f);
                RenderFrame("07-dodge-left.png");
                Spawn(playerPrefab, Vector3.zero, 180f).PreviewDodgePose(1f, 0.5f);
                RenderFrame("08-dodge-right.png");
                Debug.Log("Rendered action module proof frames: " + Path.GetFullPath(ActionProofFolder));
            }
            finally
            {
                foreach (GameObject fighter in fighters) UnityEngine.Object.DestroyImmediate(fighter);
                foreach (Material material in createdMaterials) UnityEngine.Object.DestroyImmediate(material);
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbient;
                if (!string.IsNullOrEmpty(previousScenePath)) EditorSceneManager.OpenScene(previousScenePath);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void PoseForProof(GameObject fighter)
        {
            Animator animator = fighter.GetComponent<FighterPresentation>().Animator;
            animator.Rebind();
            animator.SetFloat("Speed", 0f);
            animator.Play("Base Layer.Locomotion", 0, 0.1f);
            animator.Update(0f);
            bool player = fighter.name.StartsWith("Wuming", StringComparison.Ordinal);
            Transform socket = fighter.GetComponent<FighterPresentation>().WeaponSocket;
            socket.localPosition = Vector3.zero;
            socket.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Transform jaw = fighter.GetComponentsInChildren<Transform>()
                .FirstOrDefault(transform => transform.name == "jaw");
            if (jaw != null) jaw.localRotation *= Quaternion.Euler(8f, 0f, 0f);
        }

        private static void CreateProofLight(
            Scene scene,
            string name,
            LightType type,
            Color color,
            float intensity,
            Vector3 position,
            Quaternion rotation,
            float range = 10f)
        {
            var lightObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            lightObject.transform.SetPositionAndRotation(position, rotation);
            Light light = lightObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private static void RenderCameraToPng(Camera camera, string absolutePath, int width, int height)
        {
            RenderTexture originalTarget = camera.targetTexture;
            RenderTexture originalActive = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = originalTarget;
                RenderTexture.active = originalActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}

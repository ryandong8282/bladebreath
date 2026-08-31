using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private static PrototypeBootstrap _instance;

        private GameObject _arenaRoot;
        private PlayerController _player;
        private EnemyController _enemy;
        private bool _rebuilding;

        public static PrototypeBootstrap Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            PrototypeBootstrap existing = FindFirstObjectByType<PrototypeBootstrap>();
            if (existing == null)
            {
                new GameObject("BladeBreath Prototype Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            BuildPrototype();
        }

        private void Update()
        {
            if (PrototypeInput.ResetPressed)
            {
                ResetPrototype();
            }
        }

        public void ResetPrototype()
        {
            if (_rebuilding)
            {
                return;
            }

            StartCoroutine(RebuildRoutine());
        }

        private IEnumerator RebuildRoutine()
        {
            _rebuilding = true;
            if (_arenaRoot != null)
            {
                Destroy(_arenaRoot);
            }

            yield return null;
            BuildPrototype();
            _rebuilding = false;
        }

        private void BuildPrototype()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            _arenaRoot = new GameObject("BladeBreath Runtime Greybox");

            Material floorMaterial = CreateMaterial(new Color(0.16f, 0.17f, 0.16f));
            Material wallMaterial = CreateMaterial(new Color(0.26f, 0.24f, 0.21f));
            Material playerMaterial = CreateMaterial(new Color(0.18f, 0.45f, 0.52f));
            Material enemyMaterial = CreateMaterial(new Color(0.48f, 0.18f, 0.14f));
            Material bladeMaterial = CreateMaterial(new Color(0.72f, 0.72f, 0.68f));

            CreateArena(floorMaterial, wallMaterial);
            CreateLighting();

            _player = CreatePlayer(new Vector3(-2.4f, 0f, -0.6f), playerMaterial, bladeMaterial);
            _enemy = CreateEnemy(new Vector3(2.8f, 0f, 1.2f), enemyMaterial, bladeMaterial, _player.Combatant);
            ConfigureCamera(_player.transform);

            PrototypeHud hud = new GameObject("Prototype HUD").AddComponent<PrototypeHud>();
            hud.transform.SetParent(_arenaRoot.transform);
            hud.Configure(_player.Combatant, _enemy.Combatant);

            _player.Combatant.SetEvent("靠近执刃者，先观察再出刀");
            _enemy.Combatant.SetEvent("等待交锋");
        }

        private void CreateArena(Material floorMaterial, Material wallMaterial)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Grey Kiln Courtyard";
            floor.transform.SetParent(_arenaRoot.transform);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(1.55f, 1f, 1.35f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            CreateBox("North Wall", new Vector3(0f, 0.65f, 6.5f), new Vector3(16f, 1.3f, 0.45f), wallMaterial);
            CreateBox("South Wall", new Vector3(0f, 0.65f, -6.5f), new Vector3(16f, 1.3f, 0.45f), wallMaterial);
            CreateBox("East Wall", new Vector3(7.5f, 0.65f, 0f), new Vector3(0.45f, 1.3f, 14f), wallMaterial);
            CreateBox("West Wall", new Vector3(-7.5f, 0.65f, 0f), new Vector3(0.45f, 1.3f, 14f), wallMaterial);

            for (int i = -2; i <= 2; i++)
            {
                CreateBox(
                    $"Kiln Stone {i + 3}",
                    new Vector3(i * 2.4f, 0.16f, 4.8f + Mathf.Abs(i) * 0.2f),
                    new Vector3(1.25f, 0.32f, 0.9f),
                    wallMaterial);
            }
        }

        private void CreateBox(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = objectName;
            box.transform.SetParent(_arenaRoot.transform);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private PlayerController CreatePlayer(Vector3 position, Material bodyMaterial, Material bladeMaterial)
        {
            GameObject root = new GameObject("Player - Wuming");
            root.transform.SetParent(_arenaRoot.transform);
            root.transform.position = position;

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.42f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.28f;

            Transform sword = CreateFighterVisual(root.transform, bodyMaterial, bladeMaterial, true);

            Combatant combatant = root.AddComponent<Combatant>();
            combatant.Configure(true, 100f, 100f);

            PlayerController controller = root.AddComponent<PlayerController>();
            controller.ConfigureSword(sword);
            return controller;
        }

        private EnemyController CreateEnemy(
            Vector3 position,
            Material bodyMaterial,
            Material bladeMaterial,
            Combatant target)
        {
            GameObject root = new GameObject("Enemy - Statue Office Bladebearer");
            root.transform.SetParent(_arenaRoot.transform);
            root.transform.position = position;

            CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 1.8f;
            capsuleCollider.radius = 0.44f;
            capsuleCollider.center = new Vector3(0f, 0.9f, 0f);

            CreateFighterVisual(root.transform, bodyMaterial, bladeMaterial, false);

            Combatant combatant = root.AddComponent<Combatant>();
            combatant.Configure(false, 120f, 100f);

            EnemyController controller = root.AddComponent<EnemyController>();
            controller.Configure(target, new Color(0.48f, 0.18f, 0.14f));
            return controller;
        }

        private static Transform CreateFighterVisual(
            Transform parent,
            Material bodyMaterial,
            Material bladeMaterial,
            bool isPlayer)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.82f, 0.9f, 0.72f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            GameObject facingMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            facingMark.name = "Facing Mark";
            facingMark.transform.SetParent(parent);
            facingMark.transform.localPosition = new Vector3(0f, 1.05f, 0.48f);
            facingMark.transform.localScale = new Vector3(0.18f, 0.18f, 0.12f);
            Destroy(facingMark.GetComponent<Collider>());
            facingMark.GetComponent<Renderer>().sharedMaterial = bladeMaterial;

            GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Longblade";
            sword.transform.SetParent(parent);
            sword.transform.localPosition = isPlayer
                ? new Vector3(0.58f, 0.88f, 0.42f)
                : new Vector3(-0.58f, 0.88f, 0.42f);
            sword.transform.localRotation = Quaternion.Euler(12f, 0f, isPlayer ? -18f : 18f);
            sword.transform.localScale = new Vector3(0.09f, 0.09f, 1.45f);
            Destroy(sword.GetComponent<Collider>());
            sword.GetComponent<Renderer>().sharedMaterial = bladeMaterial;

            return sword.transform;
        }

        private void ConfigureCamera(Transform target)
        {
            Camera cameraComponent = Camera.main;
            if (cameraComponent == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraComponent = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.055f, 0.06f, 0.065f);
            cameraComponent.nearClipPlane = 0.1f;
            cameraComponent.farClipPlane = 100f;

            TopDownCamera follow = cameraComponent.GetComponent<TopDownCamera>();
            if (follow == null)
            {
                follow = cameraComponent.gameObject.AddComponent<TopDownCamera>();
            }

            follow.Configure(target);
        }

        private void CreateLighting()
        {
            Light existing = FindFirstObjectByType<Light>();
            if (existing != null)
            {
                existing.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
                existing.intensity = 1.15f;
                return;
            }

            GameObject lightObject = new GameObject("Cold Courtyard Light");
            lightObject.transform.SetParent(_arenaRoot.transform);
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            Light lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.15f;
            lightComponent.color = new Color(0.78f, 0.86f, 1f);
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

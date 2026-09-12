using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private static PrototypeBootstrap _instance;

        [SerializeField] private FighterPresentation playerVisualPrefab;
        [SerializeField] private FighterPresentation enemyVisualPrefab;
        [SerializeField] private Texture2D stoneAlbedo;
        [SerializeField] private Texture2D stoneNormal;
        [SerializeField] private Texture2D wallAlbedo;
        [SerializeField] private Texture2D wallNormal;
        [SerializeField] private Texture2D woodAlbedo;
        [SerializeField] private Texture2D woodNormal;
        [SerializeField] private GameObject environmentLandmarks;
        [SerializeField] private Texture2D environmentColossusAlbedo;
        [SerializeField] private Texture2D environmentBackdropAlbedo;
        [SerializeField] private Texture2D environmentBackdropDepth;
        [SerializeField] private Texture2D skillIconAtlas;
        [SerializeField] private AudioClip[] swingClips;
        [SerializeField] private AudioClip[] metalImpactClips;
        [SerializeField] private AudioClip[] bodyHitClips;
        [SerializeField] private AudioClip[] postureBreakClips;
        [SerializeField] private AudioClip[] clothClips;
        [SerializeField] private AudioClip[] footstepClips;

        private GameObject _arenaRoot;
        private PlayerController _player;
        private EnemyController _enemy;
        private bool _rebuilding;
        private readonly List<Material> _runtimeMaterials = new List<Material>();
        private readonly List<Texture2D> _runtimeTextures = new List<Texture2D>();

        public static PrototypeBootstrap Instance => _instance;
        public bool HasHumanoidVisuals => playerVisualPrefab != null && enemyVisualPrefab != null;

        public void ConfigureVisuals(FighterPresentation player, FighterPresentation enemy)
        {
            playerVisualPrefab = player;
            enemyVisualPrefab = enemy;
        }

        public void ConfigureEnvironmentTextures(
            Texture2D stoneColor,
            Texture2D stoneNormalMap,
            Texture2D wallColor,
            Texture2D wallNormalMap,
            Texture2D woodColor,
            Texture2D woodNormalMap)
        {
            stoneAlbedo = stoneColor;
            stoneNormal = stoneNormalMap;
            wallAlbedo = wallColor;
            wallNormal = wallNormalMap;
            woodAlbedo = woodColor;
            woodNormal = woodNormalMap;
        }

        public void ConfigureEnvironmentLandmarks(GameObject landmarks, Texture2D colossusAlbedo = null)
        {
            environmentLandmarks = landmarks;
            environmentColossusAlbedo = colossusAlbedo;
        }

        public void ConfigureEnvironmentBackdrop(Texture2D albedo, Texture2D inverseDepth)
        {
            environmentBackdropAlbedo = albedo;
            environmentBackdropDepth = inverseDepth;
        }

        public void ConfigureSkillIcons(Texture2D atlas)
        {
            skillIconAtlas = atlas;
        }

        public void ConfigureAudio(
            AudioClip[] swings,
            AudioClip[] metalImpacts,
            AudioClip[] bodyHits,
            AudioClip[] postureBreaks,
            AudioClip[] cloth,
            AudioClip[] footsteps)
        {
            swingClips = swings;
            metalImpactClips = metalImpacts;
            bodyHitClips = bodyHits;
            postureBreakClips = postureBreaks;
            clothClips = cloth;
            footstepClips = footsteps;
        }

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
            ReleaseMaterials();

            yield return null;
            BuildPrototype();
            _rebuilding = false;
        }

        private void BuildPrototype()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            _arenaRoot = new GameObject(HasHumanoidVisuals ? "BladeBreath Humanoid Sandbox" : "BladeBreath Runtime Greybox");

            Material playerMaterial = CreateMaterial(new Color(0.18f, 0.45f, 0.52f));
            Material enemyMaterial = CreateMaterial(new Color(0.48f, 0.18f, 0.14f));
            Material bladeMaterial = CreateMaterial(new Color(0.72f, 0.72f, 0.68f));

            GameObject courtyardObject = new GameObject("Grey Kiln Courtyard Art Pass");
            courtyardObject.transform.SetParent(_arenaRoot.transform);
            GreyKilnCourtyard courtyard = courtyardObject.AddComponent<GreyKilnCourtyard>();
            courtyard.Build(
                stoneAlbedo,
                stoneNormal,
                wallAlbedo,
                wallNormal,
                woodAlbedo,
                woodNormal,
                environmentLandmarks,
                environmentColossusAlbedo);

            _player = CreatePlayer(new Vector3(-2.1f, 0f, -0.75f), playerMaterial, bladeMaterial);
            _enemy = CreateEnemy(new Vector3(2.35f, 0f, 1.15f), enemyMaterial, bladeMaterial, _player.Combatant);
            Material contactShadow = CreateContactShadowMaterial();
            CreateContactShadow(_player.transform, contactShadow, new Vector2(0.9f, 0.48f));
            CreateContactShadow(_enemy.transform, contactShadow, new Vector2(0.82f, 0.44f));
            ConfigureCamera(_player.transform, _enemy.transform);
            if (environmentBackdropAlbedo != null && environmentBackdropDepth != null)
            {
                // The production plate already contains atmospheric perspective.
                // Applying runtime distance fog again would crush its far wall and
                // kiln mouths to black because the depth mesh spans twelve to
                // thirty-two metres from the camera.
                RenderSettings.fog = false;
                courtyard.SetGeometryVisible(false);
                GameObject backdropObject = new GameObject("Depth-reconstructed Grey Kiln environment");
                backdropObject.transform.SetParent(_arenaRoot.transform, false);
                backdropObject.AddComponent<DepthReconstructedBackdrop>().Build(
                    Camera.main,
                    environmentBackdropAlbedo,
                    environmentBackdropDepth);
            }

            PrototypeHud hud = new GameObject("Prototype HUD").AddComponent<PrototypeHud>();
            hud.transform.SetParent(_arenaRoot.transform);
            hud.Configure(_player.Combatant, _enemy.Combatant, _player, skillIconAtlas);

            _player.Combatant.SetEvent("靠近执刃者，先观察再出刀");
            _enemy.Combatant.SetEvent("等待交锋");
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

            Combatant combatant = root.AddComponent<Combatant>();
            combatant.Configure(true, 100f, 100f);

            FighterPresentation presentation = CreatePresentation(root.transform, combatant, playerVisualPrefab, bodyMaterial, bladeMaterial);

            PlayerController controller = root.AddComponent<PlayerController>();
            controller.ConfigurePresentation(presentation);
            if (!presentation.IsAnimated) controller.ConfigureSword(presentation.WeaponSocket);
            root.AddComponent<CombatFeedback>().Bind(combatant, new Color(0.55f, 0.82f, 1f, 0.95f));
            root.AddComponent<CombatAudio>().Configure(
                combatant, swingClips, metalImpactClips, bodyHitClips,
                postureBreakClips, clothClips, footstepClips);
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

            Combatant combatant = root.AddComponent<Combatant>();
            combatant.Configure(false, 120f, 100f);

            FighterPresentation presentation = CreatePresentation(root.transform, combatant, enemyVisualPrefab, bodyMaterial, bladeMaterial);

            EnemyController controller = root.AddComponent<EnemyController>();
            controller.ConfigurePresentation(presentation);
            controller.Configure(target, new Color(0.48f, 0.18f, 0.14f));
            root.AddComponent<CombatFeedback>().Bind(combatant, new Color(1f, 0.52f, 0.08f, 0.95f));
            root.AddComponent<CombatAudio>().Configure(
                combatant, swingClips, metalImpactClips, bodyHitClips,
                postureBreakClips, clothClips, footstepClips);
            return controller;
        }

        private static FighterPresentation CreatePresentation(
            Transform parent, Combatant combatant, FighterPresentation prefab, Material body, Material blade)
        {
            FighterPresentation presentation;
            if (prefab != null)
            {
                presentation = Instantiate(prefab, parent, false);
                presentation.name = "ModelRoot";
            }
            else
            {
                Transform modelRoot = new GameObject("ModelRoot").transform;
                modelRoot.SetParent(parent, false);
                Transform sword = CreateFighterVisual(modelRoot, body, blade, combatant.IsPlayer);
                presentation = modelRoot.gameObject.AddComponent<FighterPresentation>();
                presentation.Configure(null, sword);
            }
            presentation.Bind(combatant);
            return presentation;
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

        private void ConfigureCamera(Transform target, Transform opponent)
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
            cameraComponent.backgroundColor = new Color(0.012f, 0.014f, 0.017f);
            cameraComponent.nearClipPlane = 0.1f;
            cameraComponent.farClipPlane = 100f;
            cameraComponent.allowHDR = true;
            cameraComponent.allowMSAA = true;

            TopDownCamera follow = cameraComponent.GetComponent<TopDownCamera>();
            if (follow == null)
            {
                follow = cameraComponent.gameObject.AddComponent<TopDownCamera>();
            }

            // Keep the player in the near-left foreground and the enemy against the kiln
            // wall. Perspective depth makes attacks and contact readable without changing
            // movement or combat authority.
            follow.Configure(target, opponent, new Vector3(0f, 0.56f, -1f), 7.8f);
            follow.BindFeedback(_player.Combatant, _enemy.Combatant);

            GreyKilnLook look = cameraComponent.GetComponent<GreyKilnLook>();
            if (look == null) look = cameraComponent.gameObject.AddComponent<GreyKilnLook>();
            look.Configure();
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            _runtimeMaterials.Add(material);
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

        private Material CreateContactShadowMaterial()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime soft contact shadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                    byte alpha = (byte)Mathf.RoundToInt(falloff * falloff * 108f);
                    pixels[y * size + x] = new Color32(0, 0, 0, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _runtimeTextures.Add(texture);

            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Unlit/Transparent");
            var material = new Material(shader) { name = "Runtime contact shadow material" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            _runtimeMaterials.Add(material);
            return material;
        }

        private static void CreateContactShadow(Transform fighter, Material material, Vector2 size)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "Soft contact shadow";
            shadow.transform.SetParent(fighter, false);
            shadow.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            shadow.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(size.x, size.y, 1f);
            Collider shadowCollider = shadow.GetComponent<Collider>();
            if (shadowCollider != null) Destroy(shadowCollider);
            MeshRenderer shadowRenderer = shadow.GetComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = material;
            shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
            shadowRenderer.lightProbeUsage = LightProbeUsage.Off;
            shadowRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void ReleaseMaterials()
        {
            foreach (Material material in _runtimeMaterials) Destroy(material);
            _runtimeMaterials.Clear();
            foreach (Texture2D texture in _runtimeTextures) Destroy(texture);
            _runtimeTextures.Clear();
        }

        private void OnDestroy()
        {
            ReleaseMaterials();
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

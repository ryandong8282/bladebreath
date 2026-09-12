using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BladeBreath;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BladeBreathEditor
{
    public static class HumanoidSandboxBuilder
    {
        private const string ArtPassRequestPath = "Logs/apply-grey-kiln-art-pass.request";
        internal const string PrefabFolder = "Assets/_BladeBreath/Prefabs/Characters";
        internal const string PlayerPath = PrefabFolder + "/WumingVisual.prefab";
        internal const string EnemyPath = PrefabFolder + "/BladebearerVisual.prefab";
        internal const string ScenePath = "Assets/_BladeBreath/Scenes/HumanoidSandbox.unity";
        internal const string ControllerPath = HumanoidAssetImporter.GeneratedRoot + "/Fighter.controller";
        internal const string ArmorMeshFolder = HumanoidAssetImporter.GeneratedRoot + "/Meshes";
        internal const string CostumeModelPath = HumanoidAssetImporter.GeneratedRoot + "/Models/NorthernQiCostume.fbx";
        internal const string LongbladeModelPath = "Assets/_BladeBreath/Art/Weapons/Generated/NorthernQiLongblade.fbx";
        internal const string EnvironmentLandmarksPath = "Assets/_BladeBreath/Art/Environment/Generated/Models/GreyKilnLandmarks.fbx";
        internal const string EnvironmentColossusAlbedoPath = "Assets/_BladeBreath/Art/Environment/Generated/Textures/GreyKilnColossus_Albedo.png";
        internal const string EnvironmentBackdropAlbedoPath = "Assets/_BladeBreath/Art/Environment/Generated/Textures/GreyKilnDepthBackdrop_Albedo.png";
        internal const string EnvironmentBackdropDepthPath = "Assets/_BladeBreath/Art/Environment/Generated/Textures/GreyKilnDepthBackdrop_Depth.png";
        internal const string SkillIconAtlasPath = "Assets/_BladeBreath/Art/UI/Generated/SkillIcons_LongBlade.png";
        internal const string PipelineAssetPath = "Assets/_BladeBreath/Settings/BladeBreathMobileURP.asset";
        internal const string RendererAssetPath = "Assets/_BladeBreath/Settings/BladeBreathMobileRenderer.asset";
        internal const string StoneAlbedoPath = "Assets/_BladeBreath/ThirdParty/PolyHaven/StoneFloor/stone_floor_diff_1k.jpg";
        internal const string StoneNormalPath = "Assets/_BladeBreath/ThirdParty/PolyHaven/StoneFloor/stone_floor_nor_dx_1k.jpg";
        internal const string WallAlbedoPath = "Assets/_BladeBreath/ThirdParty/PolyHaven/RockWall/rock_wall_08_diff_1k.jpg";
        internal const string WallNormalPath = "Assets/_BladeBreath/ThirdParty/PolyHaven/RockWall/rock_wall_08_nor_dx_1k.jpg";
        internal const string WoodAlbedoPath = "Assets/_BladeBreath/ThirdParty/PolyHaven/WoodPlanks/wood_planks_diff_1k.jpg";
        internal const string WoodNormalPath = "Assets/_BladeBreath/ThirdParty/PolyHaven/WoodPlanks/wood_planks_nor_dx_1k.jpg";
        internal const string CharacterTextureFolder = HumanoidAssetImporter.GeneratedRoot + "/Textures";
        internal const string CeramicAlbedoPath = CharacterTextureFolder + "/ceramic_glaze_albedo_1k.jpg";
        internal const string CeramicNormalPath = CharacterTextureFolder + "/ceramic_glaze_normal_1k.png";
        internal const string ClothAlbedoPath = CharacterTextureFolder + "/old_cloth_albedo_1k.jpg";
        internal const string ClothNormalPath = CharacterTextureFolder + "/old_cloth_normal_1k.png";
        internal const string MetalAlbedoPath = CharacterTextureFolder + "/worn_metal_albedo_1k.jpg";
        internal const string MetalNormalPath = CharacterTextureFolder + "/worn_metal_normal_1k.png";
        internal const string WumingCeramicAlbedoPath = CharacterTextureFolder + "/wuming_ceramic_albedo_1k.jpg";
        internal const string WumingCeramicNormalPath = CharacterTextureFolder + "/wuming_ceramic_normal_1k.png";
        internal const string WumingClothAlbedoPath = CharacterTextureFolder + "/wuming_cloth_albedo_1k.jpg";
        internal const string WumingClothNormalPath = CharacterTextureFolder + "/wuming_cloth_normal_1k.png";
        internal const string BladebearerCeramicAlbedoPath = CharacterTextureFolder + "/bladebearer_ceramic_albedo_1k.jpg";
        internal const string BladebearerCeramicNormalPath = CharacterTextureFolder + "/bladebearer_ceramic_normal_1k.png";
        private const string RealisticMaleRoot =
            "Assets/_BladeBreath/ThirdParty/MakeHuman/Characters/RealisticMale";
        private const string MaleSkinPath = RealisticMaleRoot + "/young_lightskinned_male_diffuse.png";
        private const string MaleHairPath = RealisticMaleRoot + "/male01_diffuse_black.png";
        private const string MaleEyesPath = RealisticMaleRoot + "/brown_eye.png";
        private const string MaleShoesPath = RealisticMaleRoot + "/shoes02_default.png";
        private const string SwishRoot = "Assets/_BladeBreath/ThirdParty/OpenGameArt/Swishes";
        private const string ImpactRoot = "Assets/_BladeBreath/ThirdParty/Kenney/ImpactSounds";
        private const string RpgAudioRoot = "Assets/_BladeBreath/ThirdParty/Kenney/RpgAudio";

        [InitializeOnLoadMethod]
        private static void ApplyRequestedArtPass()
        {
            if (!File.Exists(ArtPassRequestPath)) return;
            File.Delete(ArtPassRequestPath);
            EditorApplication.delayCall += ApplyGreyKilnArtPass;
        }

        [MenuItem("BladeBreath/Art/Reimport Bundled Motions")]
        public static void ReimportBundledMotions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before reimporting motions.");
            Dictionary<string, AnimationClip> clips = HumanoidAssetImporter.Import(true);
            CreateController(clips);
            AssetDatabase.SaveAssets();
            HumanoidSetupValidation.ValidateAssets();
        }

        [MenuItem("BladeBreath/Art/Create Humanoid Sandbox")]
        public static void CreateHumanoidSandbox()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before creating the humanoid sandbox.");
            PrepareRenderPipeline();
            Dictionary<string, AnimationClip> clips = HumanoidAssetImporter.Import();
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(HumanoidAssetImporter.GeneratedRoot + "/Materials");
            Directory.CreateDirectory(ArmorMeshFolder);
            Directory.CreateDirectory("Assets/_BladeBreath/Scenes");
            AssetDatabase.Refresh();
            PrepareCostumeModel();
            PrepareLongbladeModel();
            GameObject environmentLandmarks = PrepareEnvironmentLandmarks();
            Texture2D environmentColossusAlbedo = PrepareEnvironmentColossusAlbedo();
            Texture2D[] environmentBackdrop = PrepareEnvironmentBackdrop();
            Texture2D skillIcons = PrepareSkillIcons();
            CreateArmorMeshes();
            Texture2D[] environmentTextures = PrepareEnvironmentTextures();
            PrepareCharacterTextures();
            AudioClip[][] audioClips = PrepareAudioClips();
            AnimatorController controller = CreateController(clips);
            FighterPresentation player = CreateVisual(true, controller);
            FighterPresentation enemy = CreateVisual(false, controller);
            AssetDatabase.SaveAssets();
            HumanoidSetupValidation.ValidateAssets();

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                PrototypeBootstrap existingBootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>();
                if (existingBootstrap == null)
                    throw new InvalidOperationException("HumanoidSandbox needs a PrototypeBootstrap root.");
                existingBootstrap.ConfigureVisuals(player, enemy);
                existingBootstrap.ConfigureEnvironmentTextures(
                    environmentTextures[0], environmentTextures[1], environmentTextures[2], environmentTextures[3],
                    environmentTextures[4], environmentTextures[5]);
                existingBootstrap.ConfigureEnvironmentLandmarks(environmentLandmarks, environmentColossusAlbedo);
                existingBootstrap.ConfigureEnvironmentBackdrop(environmentBackdrop[0], environmentBackdrop[1]);
                existingBootstrap.ConfigureSkillIcons(skillIcons);
                existingBootstrap.ConfigureAudio(
                    audioClips[0], audioClips[1], audioClips[2], audioClips[3], audioClips[4], audioClips[5]);
                EditorUtility.SetDirty(existingBootstrap);
                EditorSceneManager.SaveScene(existingBootstrap.gameObject.scene);
            }
            else
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var bootstrap = new GameObject("BladeBreath Humanoid Bootstrap").AddComponent<PrototypeBootstrap>();
                bootstrap.ConfigureVisuals(player, enemy);
                bootstrap.ConfigureEnvironmentTextures(
                    environmentTextures[0], environmentTextures[1], environmentTextures[2], environmentTextures[3],
                    environmentTextures[4], environmentTextures[5]);
                bootstrap.ConfigureEnvironmentLandmarks(environmentLandmarks, environmentColossusAlbedo);
                bootstrap.ConfigureEnvironmentBackdrop(environmentBackdrop[0], environmentBackdrop[1]);
                bootstrap.ConfigureSkillIcons(skillIcons);
                bootstrap.ConfigureAudio(
                    audioClips[0], audioClips[1], audioClips[2], audioClips[3], audioClips[4], audioClips[5]);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException($"Could not save {ScenePath}.");
            }

            // Keep the original greybox scene available for comparison.
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            BladeBreathProjectSetup.ApplyProjectDefaults();
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Human models are ready. Press Play in HumanoidSandbox. WASD: move; J: combo; L: heavy; K: guard; H/U/I: longblade skills; Space: dodge; R: reset. Existing generated assets were preserved.");
        }

        [MenuItem("BladeBreath/Art/Apply Grey Kiln Art Pass")]
        public static void ApplyGreyKilnArtPass()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the Grey Kiln art pass.");

            PrepareRenderPipeline();
            Dictionary<string, AnimationClip> clips = HumanoidAssetImporter.Import();
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(HumanoidAssetImporter.GeneratedRoot + "/Materials");
            Directory.CreateDirectory(ArmorMeshFolder);
            AssetDatabase.Refresh();
            PrepareCostumeModel();
            PrepareLongbladeModel();
            GameObject environmentLandmarks = PrepareEnvironmentLandmarks();
            Texture2D environmentColossusAlbedo = PrepareEnvironmentColossusAlbedo();
            Texture2D[] environmentBackdrop = PrepareEnvironmentBackdrop();
            Texture2D skillIcons = PrepareSkillIcons();
            CreateArmorMeshes();
            Texture2D[] environmentTextures = PrepareEnvironmentTextures();
            PrepareCharacterTextures();
            AudioClip[][] audioClips = PrepareAudioClips();
            AnimatorController controller = CreateController(clips);
            FighterPresentation player = CreateVisual(true, controller, true);
            FighterPresentation enemy = CreateVisual(false, controller, true);
            AssetDatabase.SaveAssets();

            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("Create the Humanoid Sandbox before applying its art pass.", ScenePath);
            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            PrototypeBootstrap bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>();
            if (bootstrap == null)
                throw new InvalidOperationException("HumanoidSandbox needs a PrototypeBootstrap root.");
            bootstrap.ConfigureVisuals(player, enemy);
            bootstrap.ConfigureEnvironmentTextures(
                environmentTextures[0], environmentTextures[1], environmentTextures[2], environmentTextures[3],
                environmentTextures[4], environmentTextures[5]);
            bootstrap.ConfigureEnvironmentLandmarks(environmentLandmarks, environmentColossusAlbedo);
            bootstrap.ConfigureEnvironmentBackdrop(environmentBackdrop[0], environmentBackdrop[1]);
            bootstrap.ConfigureSkillIcons(skillIcons);
            bootstrap.ConfigureAudio(
                audioClips[0], audioClips[1], audioClips[2], audioClips[3], audioClips[4], audioClips[5]);
            EditorUtility.SetDirty(bootstrap);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException($"Could not save {ScenePath}.");

            HumanoidSetupValidation.ValidateAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Grey Kiln art pass applied: rebuilt fighter silhouettes and refreshed HumanoidSandbox. Press Play to inspect the courtyard.");
        }

        private static void PrepareRenderPipeline()
        {
            Directory.CreateDirectory("Assets/_BladeBreath/Settings");
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer == null)
            {
                const string packageRenderer =
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRendererData.asset";
                if (!AssetDatabase.CopyAsset(packageRenderer, RendererAssetPath))
                    throw new IOException("Could not create the project-owned URP renderer asset.");
                AssetDatabase.ImportAsset(RendererAssetPath, ImportAssetOptions.ForceSynchronousImport);
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            }
            if (renderer == null)
                throw new InvalidOperationException("The mobile URP renderer asset could not be loaded.");

            ScreenSpaceAmbientOcclusion ssao = renderer.rendererFeatures
                .OfType<ScreenSpaceAmbientOcclusion>()
                .FirstOrDefault();
            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "Mobile Contact Shadows";
                renderer.rendererFeatures.Add(ssao);
                AssetDatabase.AddObjectToAsset(ssao, renderer);
            }

            // Mobile-friendly contact shadowing: half-resolution, four samples and a
            // small radius. It grounds feet, armor layers and masonry without turning
            // this one-room slice into a desktop-only renderer.
            var serializedSsao = new SerializedObject(ssao);
            SerializedProperty settings = serializedSsao.FindProperty("m_Settings");
            settings.FindPropertyRelative("Downsample").boolValue = true;
            settings.FindPropertyRelative("Source").enumValueIndex = 1;
            settings.FindPropertyRelative("NormalSamples").enumValueIndex = 0;
            settings.FindPropertyRelative("Intensity").floatValue = 1.15f;
            settings.FindPropertyRelative("DirectLightingStrength").floatValue = 0.18f;
            settings.FindPropertyRelative("Radius").floatValue = 0.04f;
            settings.FindPropertyRelative("Samples").enumValueIndex = 2;
            settings.FindPropertyRelative("BlurQuality").enumValueIndex = 2;
            serializedSsao.ApplyModifiedPropertiesWithoutUndo();
            ssao.Create();
            renderer.SetDirty();

            UniversalRenderPipelineAsset pipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "BladeBreath Mobile URP";
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 22f;
            pipeline.maxAdditionalLightsCount = 4;
            var serializedPipeline = new SerializedObject(pipeline);
            serializedPipeline.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serializedPipeline.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            serializedPipeline.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serializedPipeline.FindProperty("m_AdditionalLightsRenderingMode").enumValueIndex =
                (int)LightRenderingMode.PerPixel;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
        }

        private static Texture2D[] PrepareEnvironmentTextures()
        {
            string[] paths =
            {
                StoneAlbedoPath, StoneNormalPath, WallAlbedoPath, WallNormalPath, WoodAlbedoPath, WoodNormalPath
            };
            for (int i = 0; i < paths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                if (importer == null)
                    throw new FileNotFoundException("Missing Grey Kiln surface texture.", paths[i]);
                bool isNormal = i % 2 == 1;
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }

            return paths.Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path)).ToArray();
        }

        private static Texture2D PrepareSkillIcons()
        {
            var importer = AssetImporter.GetAtPath(SkillIconAtlasPath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Missing generated longblade skill icon atlas.", SkillIconAtlasPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(SkillIconAtlasPath);
        }

        private static void PrepareCharacterTextures()
        {
            string[] paths =
            {
                CeramicAlbedoPath, CeramicNormalPath,
                ClothAlbedoPath, ClothNormalPath,
                MetalAlbedoPath, MetalNormalPath,
                WumingCeramicAlbedoPath, WumingCeramicNormalPath,
                WumingClothAlbedoPath, WumingClothNormalPath,
                BladebearerCeramicAlbedoPath, BladebearerCeramicNormalPath
            };
            for (int i = 0; i < paths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                if (importer == null)
                    throw new FileNotFoundException("Missing generated character surface texture. Run scripts/process_generated_character_textures.py.", paths[i]);
                bool isNormal = i % 2 == 1;
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static void PrepareCostumeModel()
        {
            var importer = AssetImporter.GetAtPath(CostumeModelPath) as ModelImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Missing Blender-generated Northern Qi costume. Run scripts/blender/build_northern_qi_costume.py.",
                    CostumeModelPath);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.isReadable = false;
            importer.SaveAndReimport();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CostumeModelPath) == null)
                throw new InvalidOperationException("Unity could not import the Blender-generated costume FBX.");
        }

        private static void PrepareLongbladeModel()
        {
            var importer = AssetImporter.GetAtPath(LongbladeModelPath) as ModelImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Missing Blender-generated Northern Qi ring-pommel longblade. Run scripts/blender/build_northern_qi_longblade.py.",
                    LongbladeModelPath);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.importBlendShapes = false;
            importer.addCollider = false;
            importer.SaveAndReimport();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LongbladeModelPath) == null)
                throw new InvalidOperationException("Unity could not import the Blender-generated longblade FBX.");

            string legacyMeshPath = HumanoidAssetImporter.GeneratedRoot + "/Longblade.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(legacyMeshPath) != null)
                AssetDatabase.DeleteAsset(legacyMeshPath);
        }

        private static GameObject PrepareEnvironmentLandmarks()
        {
            var importer = AssetImporter.GetAtPath(EnvironmentLandmarksPath) as ModelImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Missing Blender-generated Grey Kiln landmarks. Run scripts/blender/build_grey_kiln_landmarks.py.",
                    EnvironmentLandmarksPath);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importBlendShapes = false;
            importer.addCollider = false;
            importer.SaveAndReimport();
            GameObject landmarks = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentLandmarksPath);
            if (landmarks == null)
                throw new InvalidOperationException("Grey Kiln landmark FBX did not import as a GameObject.");
            return landmarks;
        }

        private static Texture2D PrepareEnvironmentColossusAlbedo()
        {
            var importer = AssetImporter.GetAtPath(EnvironmentColossusAlbedoPath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Missing the generated Grey Kiln colossus texture.",
                    EnvironmentColossusAlbedoPath);

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 2;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 70;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(EnvironmentColossusAlbedoPath);
            if (texture == null)
                throw new InvalidOperationException("Unity could not import the generated Grey Kiln colossus texture.");
            return texture;
        }

        private static Texture2D[] PrepareEnvironmentBackdrop()
        {
            var albedoImporter = AssetImporter.GetAtPath(EnvironmentBackdropAlbedoPath) as TextureImporter;
            var depthImporter = AssetImporter.GetAtPath(EnvironmentBackdropDepthPath) as TextureImporter;
            if (albedoImporter == null || depthImporter == null)
                throw new FileNotFoundException(
                    "Missing the generated Grey Kiln depth-backdrop colour or depth texture.");

            albedoImporter.textureType = TextureImporterType.Default;
            albedoImporter.sRGBTexture = true;
            albedoImporter.isReadable = false;
            albedoImporter.mipmapEnabled = true;
            albedoImporter.npotScale = TextureImporterNPOTScale.None;
            albedoImporter.wrapMode = TextureWrapMode.Clamp;
            albedoImporter.filterMode = FilterMode.Bilinear;
            albedoImporter.anisoLevel = 1;
            albedoImporter.maxTextureSize = 1024;
            albedoImporter.textureCompression = TextureImporterCompression.Compressed;
            albedoImporter.compressionQuality = 72;
            albedoImporter.SaveAndReimport();

            depthImporter.textureType = TextureImporterType.Default;
            depthImporter.sRGBTexture = false;
            depthImporter.isReadable = true;
            depthImporter.mipmapEnabled = false;
            depthImporter.npotScale = TextureImporterNPOTScale.None;
            depthImporter.wrapMode = TextureWrapMode.Clamp;
            depthImporter.filterMode = FilterMode.Bilinear;
            depthImporter.maxTextureSize = 256;
            depthImporter.textureCompression = TextureImporterCompression.Uncompressed;
            depthImporter.SaveAndReimport();

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(EnvironmentBackdropAlbedoPath);
            Texture2D depth = AssetDatabase.LoadAssetAtPath<Texture2D>(EnvironmentBackdropDepthPath);
            if (albedo == null || depth == null)
                throw new InvalidOperationException("Unity could not import the Grey Kiln depth backdrop textures.");
            return new[] { albedo, depth };
        }

        private static AudioClip[][] PrepareAudioClips()
        {
            string[][] paths =
            {
                new[] { 1, 3, 5, 7, 10, 12 }.Select(number => $"{SwishRoot}/swish-{number}.wav").ToArray(),
                Enumerable.Range(0, 5).Select(number => $"{ImpactRoot}/impactMetal_heavy_{number:000}.ogg").ToArray(),
                new[] { $"{RpgAudioRoot}/knifeSlice.ogg", $"{RpgAudioRoot}/knifeSlice2.ogg" },
                Enumerable.Range(0, 3).Select(number => $"{ImpactRoot}/impactBell_heavy_{number:000}.ogg").ToArray(),
                new[] { $"{RpgAudioRoot}/cloth1.ogg", $"{RpgAudioRoot}/cloth2.ogg" },
                Enumerable.Range(0, 6).Select(number => $"{RpgAudioRoot}/footstep{number:00}.ogg").ToArray()
            };

            foreach (string path in paths.SelectMany(group => group))
            {
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) throw new FileNotFoundException("Missing combat audio source.", path);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.52f;
                settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = false;
                importer.ambisonic = false;
                importer.SaveAndReimport();
            }

            return paths.Select(group => group.Select(path =>
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                return clip != null ? clip : throw new InvalidOperationException("Unity could not import audio clip: " + path);
            }).ToArray()).ToArray();
        }

        private static void CreateArmorMeshes()
        {
            Directory.CreateDirectory(ArmorMeshFolder);
            AssetDatabase.Refresh();
            // The first art pass used flat extrusions and several cubes. From the game
            // camera they read as boxes, even with a detailed material. These lightweight
            // shells curve around the body and keep the same generated asset paths/GUIDs.
            CreateOrUpdateCurvedPanelMesh("CuirassPlate", 6, 4, 0.76f, 1f, 0.64f, 0.2f);
            CreateOrUpdateCurvedPanelMesh("CeramicMask", 6, 5, 0.72f, 1f, 0.72f, 0.16f);
            CreateOrUpdateCurvedPanelMesh("ShoulderPlate", 5, 3, 0.82f, 1f, 0.82f, 0.18f);
            CreateOrUpdateCurvedPanelMesh("SkirtLappet", 4, 3, 1f, 0.68f, 0.22f, 0.1f);
            CreateOrUpdateCurvedPanelMesh("CuirassTrim", 5, 2, 0.9f, 1f, 0.62f, 0.2f);
            CreateOrUpdateCurvedPanelMesh("ForearmGuard", 5, 3, 0.9f, 0.76f, 0.76f, 0.16f);
            CreateOrUpdateCurvedPanelMesh("MantlePanel", 5, 4, 1f, 0.82f, 0.18f, 0.08f);
        }

        private static void CreateOrUpdateCurvedPanelMesh(
            string name,
            int columns,
            int rows,
            float topWidth,
            float bottomWidth,
            float bulge,
            float thickness)
        {
            if (columns < 2 || rows < 1) throw new ArgumentOutOfRangeException(nameof(columns));
            string path = ArmorMeshFolder + "/" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh();
            else mesh.Clear();
            mesh.name = name;

            int stride = columns + 1;
            int surfaceVertexCount = stride * (rows + 1);
            var vertices = new List<Vector3>(surfaceVertexCount * 2);
            var uv = new List<Vector2>(surfaceVertexCount * 2);
            for (int surface = 0; surface < 2; surface++)
            {
                for (int row = 0; row <= rows; row++)
                {
                    float v = row / (float)rows;
                    float width = Mathf.Lerp(bottomWidth, topWidth, v);
                    for (int column = 0; column <= columns; column++)
                    {
                        float u = column / (float)columns;
                        float normalizedX = u * 2f - 1f;
                        float arc = Mathf.Cos(Mathf.Abs(normalizedX) * Mathf.PI * 0.5f);
                        float front = Mathf.Lerp(-0.15f, 0.5f, arc * bulge + (1f - bulge));
                        vertices.Add(new Vector3(normalizedX * width * 0.5f, v - 0.5f,
                            surface == 0 ? front : front - thickness));
                        uv.Add(new Vector2(u, v));
                    }
                }
            }

            var triangles = new List<int>(columns * rows * 12 + (columns + rows) * 12);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int a = row * stride + column;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.AddRange(new[] { a, b, d, a, d, c });
                    int back = surfaceVertexCount;
                    triangles.AddRange(new[] { back + a, back + d, back + b, back + a, back + c, back + d });
                }
            }

            int backOffset = surfaceVertexCount;
            for (int row = 0; row < rows; row++)
            {
                int left = row * stride;
                int leftNext = left + stride;
                triangles.AddRange(new[] { left, backOffset + leftNext, backOffset + left,
                    left, leftNext, backOffset + leftNext });
                int right = left + columns;
                int rightNext = right + stride;
                triangles.AddRange(new[] { right, backOffset + right, backOffset + rightNext,
                    right, backOffset + rightNext, rightNext });
            }
            for (int column = 0; column < columns; column++)
            {
                int bottom = column;
                triangles.AddRange(new[] { bottom, backOffset + bottom, backOffset + bottom + 1,
                    bottom, backOffset + bottom + 1, bottom + 1 });
                int top = rows * stride + column;
                triangles.AddRange(new[] { top, backOffset + top + 1, backOffset + top,
                    top, top + 1, backOffset + top + 1 });
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
        }

        private static AnimatorController CreateController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            else
            {
                AnimatorStateMachine oldMachine = controller.layers[0].stateMachine;
                foreach (ChildAnimatorState child in oldMachine.states) oldMachine.RemoveState(child.state);
                foreach (ChildAnimatorStateMachine child in oldMachine.stateMachines) oldMachine.RemoveStateMachine(child.stateMachine);
                foreach (BlendTree oldBlend in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().ToArray())
                    Object.DestroyImmediate(oldBlend, true);
                controller.parameters = Array.Empty<AnimatorControllerParameter>();
            }
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("LocomotionRate", AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            var locomotion = machine.AddState("Locomotion");
            var blend = new BlendTree { name = "Idle and Run", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(clips["idle"], 0f);
            blend.AddChild(clips["run"], 2.25f);
            locomotion.motion = blend;
            locomotion.speedParameter = "LocomotionRate";
            locomotion.speedParameterActive = true;
            machine.defaultState = locomotion;
            AddState(machine, "Guard", clips["guard"]);
            AddState(machine, "GuardEnter", clips["guard_enter"]);
            AddState(machine, "GuardImpact", clips["guard_impact"]);
            AddState(machine, "Attack", clips["attack"], true);
            AddState(machine, "Attack2", clips["attack2"], true);
            AddState(machine, "Attack3", clips["attack3"], true);
            AddState(machine, "HeavyAttack", clips["heavy"], true);
            AddState(machine, "SkillAttack", clips["attack2"], true);
            AddState(machine, "Hit", clips["hit"]);
            AddState(machine, "Dodge", clips["dodge"], true);
            AddState(machine, "DodgeLeft", clips["dodge_left"], true);
            AddState(machine, "DodgeRight", clips["dodge_right"], true);
            AddState(machine, "Death", clips["death"]);
            AddState(machine, "Stagger", clips["hit"], true);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddState(AnimatorStateMachine machine, string name, AnimationClip clip, bool sampled = false)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            // Combat clocks drive these three states explicitly. Other states play normally.
            state.speed = sampled ? 0f : 1f;
            state.writeDefaultValues = true;
        }

        private static FighterPresentation CreateVisual(bool player, AnimatorController controller, bool replaceExisting = false)
        {
            string path = player ? PlayerPath : EnemyPath;
            FighterPresentation existing = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<FighterPresentation>();
            if (existing != null && !replaceExisting) return existing;
            GameObject root = new GameObject(player ? "WumingVisual" : "BladebearerVisual");
            try
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidAssetImporter.ModelPath);
                if (source == null)
                    throw new InvalidOperationException("The realistic MakeHuman male FBX is unavailable. Restore the recorded source files and reimport the character.");
                GameObject model = Object.Instantiate(source, root.transform);
                model.name = "Humanoid";
                model.transform.localPosition = Vector3.zero;
                Avatar characterAvatar = HumanoidAssetImporter.CreateOrUpdateCharacterAvatar(model);

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new InvalidOperationException("The imported character has no renderers.");
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.y < 0.1f) throw new InvalidOperationException("Unexpected character height. Check the FBX axis and scale import settings.");
                float scale = 1.8f / bounds.size.y;
                model.transform.localScale *= scale;
                model.transform.localPosition = Vector3.up * (-bounds.min.y * scale);

                // Use the fine, even weave for the fitted under-tunic. The larger
                // repair atlas remains available for outer cloth, but on this UV
                // layout it produced a bright modern-looking square on the back.
                Texture2D clothAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ClothAlbedoPath);
                Texture2D clothNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(ClothNormalPath);
                Texture2D metalAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalAlbedoPath);
                Texture2D metalNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalNormalPath);
                Material cloth = Material(player ? "OldBlueCloth" : "SootRedCloth", player
                        ? new Color(0.36f, 0.41f, 0.43f) : new Color(0.42f, 0.16f, 0.11f), 0f,
                    clothAlbedo, clothNormal, player ? new Vector2(1.15f, 1.15f) : new Vector2(3.2f, 3.2f));
                Material accentCloth = Material(player ? "WumingCinnabarMantle" : "BladebearerAshMantle",
                    player ? new Color(0.46f, 0.19f, 0.12f) : new Color(0.28f, 0.17f, 0.14f), 0f,
                    clothAlbedo, clothNormal, new Vector2(1.65f, 1.65f), 0.12f);
                Material bronze = Material("WornBronze", new Color(0.78f, 0.58f, 0.30f), 0.55f,
                    metalAlbedo, metalNormal, new Vector2(2.6f, 2.6f));
                Material iron = Material("DarkIron", new Color(0.74f, 0.77f, 0.76f), 0.75f,
                    metalAlbedo, metalNormal, new Vector2(3f, 3f));
                Material polishedEdge = Material("PolishedBladeEdge", new Color(0.9f, 0.96f, 0.94f), 0.92f,
                    metalAlbedo, metalNormal, new Vector2(4f, 4f), 0.58f);
                Texture2D ceramicAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    player ? WumingCeramicAlbedoPath : BladebearerCeramicAlbedoPath);
                Texture2D ceramicNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    player ? WumingCeramicNormalPath : BladebearerCeramicNormalPath);
                Material ceramic = Material(player ? "WumingCeramic" : "BladebearerCeramic",
                    player ? new Color(0.48f, 0.46f, 0.40f) : new Color(0.50f, 0.18f, 0.12f),
                    0.05f, ceramicAlbedo, ceramicNormal, new Vector2(1.7f, 1.7f));
                Material maskCeramic = Material("EnemyMaskCeramic", new Color(0.76f, 0.72f, 0.63f),
                    0.02f,
                    AssetDatabase.LoadAssetAtPath<Texture2D>(CeramicAlbedoPath),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(CeramicNormalPath),
                    new Vector2(1.25f, 1.25f));
                Material soot = Material("MaskSoot", new Color(0.025f, 0.018f, 0.014f), 0.05f);
                ConfigureDoubleSided(maskCeramic);
                ConfigureDoubleSided(soot);
                Material boundHair = Material("BoundHair", Color.white, 0f,
                    AssetDatabase.LoadAssetAtPath<Texture2D>(MaleHairPath));
                ApplyRealisticMaleMaterials(renderers, player, cloth);

                Transform Bone(params string[] names)
                {
                    Transform match = model.GetComponentsInChildren<Transform>()
                        .FirstOrDefault(transform => names.Any(name =>
                            string.Equals(transform.name, name, StringComparison.OrdinalIgnoreCase)));
                    return match ?? throw new InvalidOperationException(
                        "Realistic character is missing required equipment bone: " + string.Join(" or ", names));
                }
                Transform jaw = Bone("jaw");
                jaw.localRotation *= Quaternion.Euler(8f, 0f, 0f);

                AttachNorthernQiCostume(root.transform, player, names => Bone(names),
                    cloth, accentCloth, bronze, iron, ceramic, maskCeramic, soot, boundHair);

                Transform socket = new GameObject("WeaponSocket").transform;
                socket.SetParent(Bone("hand.R", "hand_r"), false);
                socket.localPosition = Vector3.zero;
                // MakeHuman's right-hand +Y axis follows the fingers. The authored blade points
                // along socket +Z, so this correction keeps the grip in the palm and the edge up.
                socket.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                CreateLongblade(socket, iron, polishedEdge, bronze, cloth);

                Animator animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                Animation legacyAnimation = model.GetComponent<Animation>();
                if (legacyAnimation != null) Object.DestroyImmediate(legacyAnimation);
                animator.avatar = characterAvatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var presentation = root.AddComponent<FighterPresentation>();
                presentation.Configure(animator, socket);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new IOException($"Could not save {path}.");
                return prefab.GetComponent<FighterPresentation>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Piece(
            string name,
            PrimitiveType shape,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material)
        {
            GameObject piece = GameObject.CreatePrimitive(shape);
            piece.name = name;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = size;
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AttachNorthernQiCostume(
            Transform parent,
            bool player,
            Func<string[], Transform> resolveBone,
            Material cloth,
            Material accentCloth,
            Material bronze,
            Material iron,
            Material ceramic,
            Material maskCeramic,
            Material soot,
            Material hair)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(CostumeModelPath);
            if (source == null) throw new InvalidOperationException("The Blender-generated costume FBX is unavailable.");
            GameObject costume = Object.Instantiate(source, parent, false);
            costume.name = "NorthernQiCostume";
            costume.transform.localPosition = Vector3.zero;
            costume.transform.localRotation = Quaternion.identity;
            costume.transform.localScale = Vector3.one;

            Renderer[] costumeRenderers = costume.GetComponentsInChildren<Renderer>(true);
            if (costumeRenderers.Length < 12)
                throw new InvalidOperationException("The Northern Qi costume FBX is incomplete.");

            foreach (Renderer renderer in costumeRenderers)
            {
                string name = renderer.name;
                string boneName = name switch
                {
                    "SkirtLeft" => "thigh.L",
                    "SkirtRight" => "thigh.R",
                    "ShoulderGuardLeft" => "upper_arm.L",
                    "ShoulderGuardRight" => "upper_arm.R",
                    "SleeveLeft" => "upper_arm.L",
                    "SleeveRight" => "upper_arm.R",
                    "BracerLeft" => "forearm.L",
                    "BracerRight" => "forearm.R",
                    "KneeGuardLeft" => "thigh.L",
                    "KneeGuardRight" => "thigh.R",
                    "HairKnot" => "head",
                    "HairBand" => "head",
                    "EnemyFaceDetails" => "head",
                    "TunicTorso" => "chest",
                    "LamellarFront" => "chest",
                    "LamellarBack" => "chest",
                    "LamellarRivetsFront" => "chest",
                    "LamellarRivetsBack" => "chest",
                    "MantleBack" => "chest",
                    "MantleShoulderLeft" => "upper_arm.L",
                    "MantleShoulderRight" => "upper_arm.R",
                    "BeltPendant" => "hips",
                    "FrontPlacket" => "chest",
                    "BlankNameplate" => "chest",
                    "RoundCollar" => "chest-1",
                    _ => "hips"
                };
                renderer.transform.SetParent(resolveBone(new[] { boneName }), true);
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;

                if (name == "EnemyFaceDetails")
                {
                    renderer.gameObject.SetActive(!player);
                    Transform head = resolveBone(new[] { "head" });
                    Transform leftEye = resolveBone(new[] { "eye.L" });
                    Transform rightEye = resolveBone(new[] { "eye.R" });
                    Vector3 eyeCenter = (leftEye.position + rightEye.position) * 0.5f;
                    Vector3 faceDirection = Vector3.ProjectOnPlane(eyeCenter - head.position, parent.up).normalized;
                    if (faceDirection.sqrMagnitude < 0.5f)
                        throw new InvalidOperationException("The realistic fighter eye bones do not define a usable facial direction.");
                    renderer.transform.rotation = Quaternion.LookRotation(faceDirection, parent.up);
                    renderer.transform.position = eyeCenter - parent.up * 0.035f - faceDirection * 0.01f;
                    int subMeshes = renderer is MeshRenderer
                        ? renderer.GetComponent<MeshFilter>()?.sharedMesh?.subMeshCount ?? 1
                        : 1;
                    Material[] faceMaterials = new Material[subMeshes];
                    for (int i = 0; i < faceMaterials.Length; i++)
                        faceMaterials[i] = i == 1 || i == 2 ? soot : maskCeramic;
                    renderer.sharedMaterials = faceMaterials;
                }
                else if (name.Contains("Mantle") || name.Contains("Pendant") ||
                         (player && (name == "SkirtBack" || name == "SkirtLeft" || name == "SkirtRight")))
                    renderer.sharedMaterial = accentCloth;
                else if (name.Contains("Tunic") || name.Contains("Skirt") || name.Contains("Sleeve"))
                    renderer.sharedMaterial = cloth;
                else if (name.Contains("Rivet") || name.Contains("Trim"))
                    renderer.sharedMaterial = bronze;
                else if (name.Contains("Lamellar") || name.Contains("Shoulder") || name.Contains("Bracer") || name.Contains("Knee"))
                    renderer.sharedMaterial = iron;
                else if (name.Contains("Collar") || name.Contains("Belt") || name.Contains("Placket"))
                    renderer.sharedMaterial = bronze;
                else if (name.Contains("Nameplate"))
                    renderer.sharedMaterial = ceramic;
                else if (name.Contains("Hair"))
                    renderer.sharedMaterial = hair;
            }

            // All visible mesh transforms now live directly on their animation bones.
            Object.DestroyImmediate(costume);
        }

        private static Mesh ArmorMesh(string name)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ArmorMeshFolder + "/" + name + ".asset");
            if (mesh == null) throw new InvalidOperationException("Missing generated armor mesh: " + name);
            return mesh;
        }

        private static void MeshPiece(
            string name,
            Mesh mesh,
            Transform bone,
            Vector3 worldPosition,
            Vector3 size,
            Material material,
            Quaternion? worldRotation = null)
        {
            var piece = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            piece.transform.position = worldPosition;
            piece.transform.rotation = worldRotation ?? Quaternion.identity;
            piece.transform.localScale = size;
            piece.transform.SetParent(bone, true);
            piece.GetComponent<MeshFilter>().sharedMesh = mesh;
            piece.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material Material(
            string name,
            Color color,
            float metallic = 0f,
            Texture2D albedo = null,
            Texture2D normal = null,
            Vector2? tiling = null,
            float smoothness = 0.25f)
        {
            string path = HumanoidAssetImporter.GeneratedRoot + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(
                GraphicsSettings.currentRenderPipeline != null ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null)
                throw new InvalidOperationException("The active render pipeline's Lit shader is unavailable. Restore packages first.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            Vector2 scale = tiling ?? Vector2.one;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTextureScale("_BaseMap", scale);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", albedo);
                material.SetTextureScale("_MainTex", scale);
            }
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", scale);
                if (normal != null) material.EnableKeyword("_NORMALMAP");
                else material.DisableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureDoubleSided(Material material)
        {
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
        }

        private static void ApplyRealisticMaleMaterials(Renderer[] renderers, bool player, Material underTunic)
        {
            if (!renderers.SelectMany(renderer => renderer.sharedMaterials).Any(material => material != null))
                throw new InvalidOperationException("The realistic human body has no imported renderer material.");
            Texture2D Load(string path, bool normal = false)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("Missing realistic male texture.", path);
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            Material skin = Material("RealisticMaleSkin", Color.white, 0f, Load(MaleSkinPath));
            Material hair = Material("RealisticMaleHair", Color.white, 0f, Load(MaleHairPath));
            Material eyes = Material("RealisticMaleEyes", Color.white, 0f, Load(MaleEyesPath));
            Material shoes = Material("RealisticMaleShoes", Color.white, 0f, Load(MaleShoesPath));

            foreach (Renderer renderer in renderers)
            {
                string rendererKey = ((renderer as SkinnedMeshRenderer)?.sharedMesh?.name ?? renderer.name).ToLowerInvariant();
                Material Resolve(Material source)
                {
                    string key = ((source != null ? source.name : string.Empty) + " " + rendererKey).ToLowerInvariant();
                    // Keep the fitted mesh only as a cloth underlayer. Replacing its business
                    // texture removes the shirt, tie and lapel colors before the long tunic is added.
                    if (key.Contains("suit")) return underTunic;
                    if (key.Contains("shoe")) return shoes;
                    if (key.Contains("hair") || key.Contains("male01")) return hair;
                    if (key.Contains("eye") || key.Contains("brown")) return eyes;
                    return skin;
                }
                Material[] sourceMaterials = renderer.sharedMaterials;
                renderer.sharedMaterials = sourceMaterials.Length == 0
                    ? new[] { Resolve(null) }
                    : sourceMaterials.Select(Resolve).ToArray();
            }
        }

        private static void CreateLongblade(
            Transform socket,
            Material iron,
            Material polishedEdge,
            Material bronze,
            Material cloth)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(LongbladeModelPath);
            if (source == null)
                throw new InvalidOperationException("The generated Northern Qi longblade model is unavailable.");

            var blade = new GameObject("Longblade");
            blade.transform.SetParent(socket, false);
            // MakeHuman stores centimetre-scale bones under a compensating model scale. Cancel
            // that inherited scale so the authored metre dimensions remain metre dimensions.
            Vector3 inherited = socket.lossyScale;
            blade.transform.localScale = new Vector3(
                Mathf.Approximately(inherited.x, 0f) ? 1f : 1f / inherited.x,
                Mathf.Approximately(inherited.y, 0f) ? 1f : 1f / inherited.y,
                Mathf.Approximately(inherited.z, 0f) ? 1f : 1f / inherited.z);
            GameObject geometry = Object.Instantiate(source, blade.transform);
            geometry.name = "NorthernQiLongbladeGeometry";
            geometry.transform.localPosition = Vector3.zero;
            geometry.transform.localRotation = Quaternion.identity;
            geometry.transform.localScale = Vector3.one;
            Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 4)
                throw new InvalidOperationException(
                    $"Northern Qi longblade must import as four mobile material groups, found {renderers.Length}.");
            foreach (Renderer renderer in renderers)
            {
                string key = renderer.name.ToLowerInvariant();
                renderer.sharedMaterial = key.Contains("edge") ? polishedEdge
                    : key.Contains("bronze") ? bronze
                    : key.Contains("grip") ? cloth
                    : iron;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            foreach (string name in new[] { "TrailBase", "TrailTip" })
            {
                Transform point = new GameObject(name).transform;
                point.SetParent(blade.transform, false);
                point.localPosition = new Vector3(0f, 0f, name == "TrailBase" ? 0.14f : 0.94f);
            }
        }
    }
}

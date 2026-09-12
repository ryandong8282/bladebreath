using System;
using System.Linq;
using BladeBreath;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BladeBreathEditor
{
    public static class HumanoidSetupValidation
    {
        [MenuItem("BladeBreath/Art/Validate Humanoid Setup")]
        public static void ValidateAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before validating generated assets.");
            foreach (string name in HumanoidAssetImporter.ClipNames)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HumanoidAssetImporter.GeneratedRoot + "/Animations/" + name + ".anim");
                Require(clip != null && clip.humanMotion && clip.length > 0f, $"Missing or invalid Humanoid motion '{name}'. Run Create Humanoid Sandbox.");
            }
            ImportLog kayKitImportLog = AssetImporter.GetImportLog(HumanoidAssetImporter.KayKitAnimationSourcePath);
            string[] kayKitImportProblems = kayKitImportLog == null || kayKitImportLog.logEntries == null
                ? Array.Empty<string>()
                : kayKitImportLog.logEntries
                    .Where(entry => (entry.flags & (ImportLogFlags.Warning | ImportLogFlags.Error)) != 0)
                    .Select(entry => entry.message)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct()
                    .ToArray();
            Require(kayKitImportProblems.Length == 0,
                "KayKit animation FBX has import warnings: " + string.Join(" | ", kayKitImportProblems));
            ImportLog ual2ImportLog = AssetImporter.GetImportLog(HumanoidAssetImporter.Quaternius2AnimationSourcePath);
            string[] ual2ImportProblems = ual2ImportLog == null || ual2ImportLog.logEntries == null
                ? Array.Empty<string>()
                : ual2ImportLog.logEntries
                    .Where(entry => (entry.flags & (ImportLogFlags.Warning | ImportLogFlags.Error)) != 0)
                    .Select(entry => entry.message)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct()
                    .ToArray();
            Require(ual2ImportProblems.Length == 0,
                "Quaternius UAL2 animation FBX has import warnings: " + string.Join(" | ", ual2ImportProblems));
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HumanoidSandboxBuilder.ControllerPath);
            Require(controller != null, "Missing Fighter.controller. Run Create Humanoid Sandbox.");
            string[] states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            foreach (string name in new[]
                     {
                         "Locomotion", "Guard", "GuardEnter", "GuardImpact", "Attack", "Attack2", "Attack3", "HeavyAttack", "SkillAttack",
                         "Hit", "Dodge", "DodgeLeft", "DodgeRight", "Death", "Stagger"
                     })
                Require(states.Contains(name), "Fighter.controller is missing state " + name);
            var stateMotions = controller.layers[0].stateMachine.states.ToDictionary(s => s.state.name, s => s.state.motion);
            Require(stateMotions["Attack"] != stateMotions["Attack2"] &&
                    stateMotions["Attack2"] != stateMotions["Attack3"] &&
                    stateMotions["Attack3"] != stateMotions["HeavyAttack"],
                "The three light attacks and heavy chop must use four distinct source clips.");
            Require(stateMotions["DodgeLeft"] != stateMotions["DodgeRight"],
                "Left and right dodge states must use distinct directional clips.");
            Require(controller.parameters.Any(p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float), "Fighter.controller needs the Speed float parameter.");
            Require(controller.parameters.Any(p => p.name == "LocomotionRate" && p.type == AnimatorControllerParameterType.Float), "Fighter.controller needs the LocomotionRate float parameter.");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                HumanoidSandboxBuilder.PipelineAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                HumanoidSandboxBuilder.RendererAssetPath);
            Require(pipeline != null && renderer != null,
                "Missing the project-owned mobile URP pipeline. Apply the Grey Kiln art pass.");
            Require(GraphicsSettings.defaultRenderPipeline == pipeline && QualitySettings.renderPipeline == pipeline,
                "The mobile URP pipeline exists but is not active for the current quality level.");
            Require(pipeline.supportsCameraDepthTexture && !pipeline.supportsCameraOpaqueTexture &&
                    pipeline.msaaSampleCount == 2 && pipeline.maxAdditionalLightsCount == 4,
                "The Grey Kiln mobile renderer budget changed unexpectedly.");
            Require(renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().Any(),
                "The mobile renderer is missing its low-cost contact shadow feature.");

            ValidateVisual(HumanoidSandboxBuilder.PlayerPath);
            ValidateVisual(HumanoidSandboxBuilder.EnemyPath);
            ValidateEnvironmentLandmarks();
            ValidateSkillIconAtlas();
            ValidateSurfaceTexture(HumanoidSandboxBuilder.StoneAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.StoneNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WallAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WallNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WoodAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WoodNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.EnvironmentColossusAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.EnvironmentBackdropAlbedoPath, false);
            ValidateDepthTexture(HumanoidSandboxBuilder.EnvironmentBackdropDepthPath);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.CeramicAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.CeramicNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.ClothAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.ClothNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.MetalAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.MetalNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WumingCeramicAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WumingCeramicNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WumingClothAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.WumingClothNormalPath, true);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.BladebearerCeramicAlbedoPath, false);
            ValidateSurfaceTexture(HumanoidSandboxBuilder.BladebearerCeramicNormalPath, true);
            foreach (string meshName in new[]
                     {
                         "CuirassPlate", "CeramicMask", "ShoulderPlate", "SkirtLappet",
                         "CuirassTrim", "ForearmGuard", "MantlePanel"
                     })
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(HumanoidSandboxBuilder.ArmorMeshFolder + "/" + meshName + ".asset");
                Require(mesh != null && mesh.vertexCount >= 24 && mesh.triangles.Length >= 36 && mesh.uv.Length == mesh.vertexCount,
                    "Missing or invalid generated armor mesh: " + meshName);
            }
            RunCombatChecks();
            Debug.Log("Humanoid setup passed: real male bodies, four distinct UAL2 sword actions, authored sword block, directional side dodges, a one-metre-class longblade, a depth-reconstructed Grey Kiln plate, 1K runtime surfaces, no root motion/colliders on visuals, and combat outcome checks. Play Mode and iPhone acceptance are still required.");
        }

        private static void ValidateSurfaceTexture(string path, bool normalMap)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture != null && texture.width <= 1024 && texture.height <= 1024,
                "Missing or oversized Grey Kiln texture: " + path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null && importer.maxTextureSize == 1024 && importer.mipmapEnabled && !importer.isReadable,
                "Grey Kiln texture import settings are not mobile-safe: " + path);
            TextureImporterType expected = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            Require(importer.textureType == expected, "Unexpected texture type for " + path);
        }

        private static void ValidateDepthTexture(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(texture != null && texture.width == 256 && texture.height == 144,
                "Missing or wrongly sized Grey Kiln inverse-depth texture: " + path);
            Require(importer != null && importer.maxTextureSize == 256 && importer.isReadable &&
                    !importer.mipmapEnabled && !importer.sRGBTexture &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed,
                "Grey Kiln inverse-depth texture must stay readable, linear and uncompressed at 256 pixels wide.");
        }

        private static void ValidateEnvironmentLandmarks()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidSandboxBuilder.EnvironmentLandmarksPath);
            Require(model != null, "Missing Blender-generated Grey Kiln landmark model.");
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            Require(renderers.Length >= 12 && renderers.Length <= 18,
                "Grey Kiln landmarks should remain a compact, cullable renderer set.");
            MeshFilter generatedColossus = renderers
                .Where(renderer => renderer.name.Contains("AI_BrokenColossus"))
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .FirstOrDefault(filter => filter != null && filter.sharedMesh != null);
            Require(generatedColossus != null &&
                    generatedColossus.sharedMesh.triangles.Length / 3 <= 30000,
                "Grey Kiln needs the mobile-decimated generated colossus mesh.");
            int triangles = renderers
                .OfType<MeshRenderer>()
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null)
                .Sum(filter => filter.sharedMesh.triangles.Length / 3);
            Require(triangles >= 45000 && triangles <= 60000,
                "Grey Kiln landmark triangle budget changed unexpectedly: " + triangles);
            Require(model.GetComponentsInChildren<Collider>(true).Length == 0,
                "Landmark FBX is presentation-only; arena collision remains in GreyKilnCourtyard.");
        }

        private static void ValidateSkillIconAtlas()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(HumanoidSandboxBuilder.SkillIconAtlasPath);
            Require(texture != null && texture.width <= 1024 && texture.height <= 512,
                "Missing or oversized longblade skill icon atlas.");
            float aspect = texture.height <= 0 ? 0f : (float)texture.width / texture.height;
            Require(aspect > 2.9f && aspect < 3.1f,
                "Longblade skill atlas must keep exactly three equal horizontal cells.");
            var importer = AssetImporter.GetAtPath(HumanoidSandboxBuilder.SkillIconAtlasPath) as TextureImporter;
            Require(importer != null && importer.maxTextureSize == 1024 && !importer.mipmapEnabled &&
                    !importer.isReadable && importer.wrapMode == TextureWrapMode.Clamp &&
                    importer.npotScale == TextureImporterNPOTScale.None,
                "Longblade skill icon atlas import settings are not mobile-safe.");
        }

        private static void ValidateVisual(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, "Missing character prefab: " + path);
            FighterPresentation visual = prefab.GetComponent<FighterPresentation>();
            Require(visual != null && visual.IsAnimated, "Invalid character Avatar: " + path);
            Require(visual.Animator.runtimeAnimatorController != null, "Missing Animator controller: " + path);
            Require(!visual.Animator.applyRootMotion, "Disable root motion: movement belongs to the combat controllers.");
            Require(prefab.GetComponentsInChildren<Collider>(true).Length == 0, "Visual prefabs must not add combat colliders.");
            Require(visual.WeaponSocket != null &&
                    (visual.WeaponSocket.parent.name == "hand_r" || visual.WeaponSocket.parent.name == "hand.R"),
                "Longblade must be bound to the right hand.");
            Transform longblade = visual.WeaponSocket.Find("Longblade");
            Require(longblade != null && longblade.Find("TrailBase") != null && longblade.Find("TrailTip") != null,
                "Missing weapon blade-arc anchors.");
            MeshFilter[] weaponMeshes = longblade.GetComponentsInChildren<MeshFilter>(true);
            Require(weaponMeshes.Length == 4, "Longblade must keep its four mobile material groups.");
            MeshFilter bladeFilter = weaponMeshes.FirstOrDefault(filter =>
                filter.name.IndexOf("steel", StringComparison.OrdinalIgnoreCase) >= 0);
            Mesh blade = bladeFilter?.sharedMesh;
            float bladeLength = blade == null ? 0f : Mathf.Max(blade.bounds.size.x, blade.bounds.size.y, blade.bounds.size.z);
            Require(blade != null && bladeLength > 0.75f && bladeLength < 1f,
                "Longblade must remain within the approved one-handed blade proportion.");
            Vector3 bladeDirection = longblade.InverseTransformDirection(
                bladeFilter.transform.TransformDirection(Vector3.up)).normalized;
            Require(Mathf.Abs(Vector3.Dot(bladeDirection, Vector3.forward)) > 0.98f,
                "Imported longblade axis must align with the existing +Z trail and combat presentation contract.");
            long weaponTriangles = weaponMeshes.Where(filter => filter.sharedMesh != null)
                .Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
                    .Sum(subMesh => (long)filter.sharedMesh.GetIndexCount(subMesh) / 3L));
            Require(weaponTriangles >= 2500 && weaponTriangles <= 4000,
                "Longblade geometry must remain detailed enough to read and below the M0 mobile budget.");
            Require(longblade.GetComponentsInChildren<Collider>(true).Length == 0,
                "Weapon art must not add a second combat collision owner.");
            Require(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length > 0, "Missing skinned character mesh.");
            Require(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(renderer => renderer.sharedMesh.vertexCount) >= 20000,
                "Character body regressed to the temporary low-poly mesh.");
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>())
                Require(renderer.sharedMaterials.All(m => m != null && m.shader != null), "Missing character material on " + renderer.name);
            foreach (Material material in prefab.GetComponentsInChildren<Renderer>().SelectMany(renderer => renderer.sharedMaterials).Distinct())
            {
                if (!material.HasProperty("_BaseMap")) continue;
                // The narrow recessed eye slits are intentionally a flat soot
                // color. At the combat camera distance an albedo/normal pair would
                // add memory without adding visible surface information.
                if (material.name == "MaskSoot") continue;
                Require(material.GetTexture("_BaseMap") != null, "Character material is missing its generated albedo: " + material.name);
                if (!material.name.Contains("Hair", StringComparison.OrdinalIgnoreCase) &&
                    !material.name.Contains("Skin", StringComparison.OrdinalIgnoreCase) &&
                    !material.name.Contains("Eyes", StringComparison.OrdinalIgnoreCase) &&
                    !material.name.Contains("Shoes", StringComparison.OrdinalIgnoreCase))
                    Require(material.GetTexture("_BumpMap") != null, "Character material is missing its generated normal map: " + material.name);
            }

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject instance = Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(instance, preview);
                Animator animator = instance.GetComponent<FighterPresentation>().Animator;
                animator.Rebind();
                animator.Update(0f);
                Require(animator.GetBoneTransform(HumanBodyBones.RightHand) != null, "Right hand failed Humanoid retargeting.");
                Require(animator.GetBoneTransform(HumanBodyBones.Head) != null, "Head failed Humanoid retargeting.");
                Transform modelRoot = animator.transform;
                Vector3 start = modelRoot.localPosition;
                Quaternion rotation = modelRoot.localRotation;
                animator.Play("Base Layer.Attack", 0, 0.1f);
                animator.Update(0f);
                Vector3 firstHand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                animator.Play("Base Layer.Attack", 0, 0.6f);
                animator.Update(0f);
                Vector3 secondHand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                Require(Vector3.Distance(firstHand, secondHand) > 0.02f, "Attack did not animate the retargeted hand.");
                Require(Vector3.Distance(start, modelRoot.localPosition) < 0.001f && Quaternion.Angle(rotation, modelRoot.localRotation) < 0.1f, "Animation moved the model root. Check root motion bake settings.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // These checks exercise actual Combatant code in Unity, without adding a test framework dependency.
        private static void RunCombatChecks()
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var attackerObject = new GameObject("Validation attacker");
                var defenderObject = new GameObject("Validation defender");
                SceneManager.MoveGameObjectToScene(attackerObject, preview);
                SceneManager.MoveGameObjectToScene(defenderObject, preview);
                Combatant attacker = attackerObject.AddComponent<Combatant>();
                Combatant defender = defenderObject.AddComponent<Combatant>();
                attacker.Configure(true);
                defender.Configure(false);
                var normal = new AttackData(13f, 24f);
                defender.BeginGuard(0f);
                Require(defender.ReceiveAttack(attacker, normal) == CombatOutcome.Guarded && defender.Health == 100f && defender.Posture == 24f, "Guard regression.");
                defender.ResetState();
                defender.BeginGuard();
                Require(defender.ReceiveAttack(attacker, normal) == CombatOutcome.Parried && attacker.IsStaggered && defender.Health == 100f, "Parry regression.");
                defender.ResetState();
                defender.BeginGuard();
                Require(defender.ReceiveAttack(attacker, new AttackData(30f, 48f, true)) == CombatOutcome.Hit && defender.Health == 70f, "Unblockable attack regression.");
                defender.ResetState();
                defender.SetInvulnerable(0.1f);
                Require(defender.ReceiveAttack(attacker, normal) == CombatOutcome.Dodged && defender.Health == 100f, "Dodge regression.");
                attacker.ResetState();
                defender.ResetState();
                attacker.BeginAttackWindow();
                defender.BeginAttackWindow();
                Require(defender.ReceiveAttack(attacker, normal) == CombatOutcome.Clashed &&
                        attacker.Health == 100f && defender.Health == 100f &&
                        attacker.IsStaggered && defender.IsStaggered &&
                        !attacker.IsAttackActive && !defender.IsAttackActive,
                    "Overlapping blockable attack windows must resolve once as a weapon clash.");
                attacker.ResetState();
                defender.ResetState();
                defender.transform.rotation = Quaternion.Euler(0f, 67f, 0f);
                Quaternion facing = defender.transform.rotation;
                Require(defender.AddPosture(100f) && defender.IsStaggered, "Posture break regression.");
                Require(Quaternion.Angle(facing, defender.transform.rotation) < 0.01f, "Stagger changed collision-root facing.");
                Require(defender.CanBeExecutedBy(attacker, 1.65f), "Execution eligibility regression.");
                defender.Execute(attacker);
                Require(attacker.IsInvulnerable, "Execution grants attacker invulnerability regression.");
                Require(defender.IsDead && defender.Health == 0f && defender.transform.localScale == Vector3.one, "Death changed collision-root scale.");
                defender.ResetState();
                Require(!defender.IsDead && !defender.IsStaggered && defender.Posture == 0f && defender.Health == 100f, "Reset regression.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

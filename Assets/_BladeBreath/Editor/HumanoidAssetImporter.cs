using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using BladeBreath;
using UnityEditor;
using UnityEngine;

namespace BladeBreathEditor
{
    internal sealed class SkillIconAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath != HumanoidSandboxBuilder.SkillIconAtlasPath) return;

            var importer = (TextureImporter)assetImporter;
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
        }
    }

    internal static class HumanoidAssetImporter
    {
        internal const string SourceRoot = "Assets/_BladeBreath/ThirdParty/Quaternius";
        internal const string ModelPath =
            "Assets/_BladeBreath/ThirdParty/MakeHuman/Characters/RealisticMale/makehuman.fbx";
        internal const string GeneratedRoot = "Assets/_BladeBreath/Art/Characters/Generated";
        internal const string QuaterniusAnimationSourcePath = SourceRoot + "/Animations/Editor/Extracted/UAL1_Standard.fbx";
        internal const string KayKitAnimationSourcePath =
            "Assets/_BladeBreath/ThirdParty/KayKit/Animations/Editor/KayKit_Selected.fbx";
        internal const string Quaternius2AnimationSourcePath =
            "Assets/_BladeBreath/ThirdParty/Quaternius/Animations2/Editor/Extracted/UAL2_Standard.fbx";

        private enum MotionSource
        {
            Quaternius,
            Quaternius2,
            KayKit
        }

        private readonly struct MotionSpec
        {
            internal MotionSpec(string output, string source, MotionSource sourceAsset,
                bool loop = false, bool mirror = false, bool bakeHorizontalPosition = true)
            {
                Output = output;
                Source = source;
                SourceAsset = sourceAsset;
                Loop = loop;
                Mirror = mirror;
                BakeHorizontalPosition = bakeHorizontalPosition;
            }

            internal string Output { get; }
            internal string Source { get; }
            internal MotionSource SourceAsset { get; }
            internal bool Loop { get; }
            internal bool Mirror { get; }
            internal bool BakeHorizontalPosition { get; }
        }

        private static readonly MotionSpec[] Motions =
        {
            new MotionSpec("idle", "Idle_Loop", MotionSource.Quaternius, true),
            new MotionSpec("run", "Jog_Fwd_Loop", MotionSource.Quaternius, true),
            // The combat controller owns every lunge. Import the authored sword paths while
            // stripping source XZ root motion so presentation and CharacterController agree.
            new MotionSpec("attack", "Sword_Regular_A", MotionSource.Quaternius2, bakeHorizontalPosition: false),
            new MotionSpec("attack2", "Sword_Regular_B", MotionSource.Quaternius2, bakeHorizontalPosition: false),
            new MotionSpec("attack3", "Sword_Regular_C", MotionSource.Quaternius2, bakeHorizontalPosition: false),
            new MotionSpec("heavy", "Sword_Dash_RM", MotionSource.Quaternius2, bakeHorizontalPosition: false),
            new MotionSpec("guard", "Sword_Block", MotionSource.Quaternius2),
            new MotionSpec("guard_enter", "Sword_Block", MotionSource.Quaternius2),
            new MotionSpec("guard_impact", "Block_Hit", MotionSource.KayKit),
            new MotionSpec("hit", "Hit_Chest", MotionSource.Quaternius),
            new MotionSpec("dodge", "Roll", MotionSource.Quaternius),
            new MotionSpec("dodge_left", "Dodge_Left", MotionSource.KayKit),
            new MotionSpec("dodge_right", "Dodge_Right", MotionSource.KayKit),
            new MotionSpec("death", "Death01", MotionSource.Quaternius)
        };

        internal static readonly string[] ClipNames = Motions.Select(motion => motion.Output).ToArray();

        internal static Dictionary<string, AnimationClip> Import(bool refreshGeneratedMotions = false)
        {
            ExtractAnimations();
            if (!File.Exists(KayKitAnimationSourcePath))
                throw new FileNotFoundException(
                    "Restore or regenerate the selected KayKit animation FBX with scripts/blender/extract_kaykit_actions.py.",
                    KayKitAnimationSourcePath);
            Directory.CreateDirectory(GeneratedRoot + "/Animations");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
                throw new InvalidOperationException($"Unity could not import the realistic MakeHuman FBX character at {ModelPath}.");
            ImportRig(ModelPath, false, RealisticMaleHumanMap(), true);
            var importedByOutput = new Dictionary<string, AnimationClip>();
            foreach (IGrouping<MotionSource, MotionSpec> sourceGroup in Motions.GroupBy(motion => motion.SourceAsset))
            {
                string sourcePath = sourceGroup.Key switch
                {
                    MotionSource.KayKit => KayKitAnimationSourcePath,
                    MotionSource.Quaternius2 => Quaternius2AnimationSourcePath,
                    _ => QuaterniusAnimationSourcePath
                };
                HumanBone[] sourceMap = sourceGroup.Key == MotionSource.KayKit ? KayKitHumanMap() : HumanMap();
                ModelImporter importer = ImportRig(
                    sourcePath,
                    true,
                    sourceMap,
                    manualMapping: sourceGroup.Key == MotionSource.KayKit,
                    translationDof: sourceGroup.Key == MotionSource.KayKit,
                    deferReimport: true);
                ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
                var configuredClips = new List<ModelImporterClipAnimation>();
                foreach (MotionSpec motion in sourceGroup)
                {
                    ModelImporterClipAnimation source = defaults.FirstOrDefault(c =>
                        c.takeName == motion.Source || c.name == motion.Source ||
                        c.takeName.EndsWith("|" + motion.Source, StringComparison.Ordinal) ||
                        c.name.EndsWith("|" + motion.Source, StringComparison.Ordinal));
                    if (source == null)
                        throw new InvalidOperationException(
                            $"Missing {sourceGroup.Key} motion '{motion.Source}' in {sourcePath}.");

                    configuredClips.Add(new ModelImporterClipAnimation
                    {
                        name = motion.Output,
                        takeName = source.takeName,
                        firstFrame = source.firstFrame,
                        lastFrame = source.lastFrame,
                        wrapMode = source.wrapMode,
                        loopTime = motion.Loop,
                        loopPose = motion.Loop,
                        mirror = motion.Mirror,
                        lockRootRotation = true,
                        keepOriginalOrientation = true,
                        lockRootPositionXZ = motion.BakeHorizontalPosition,
                        keepOriginalPositionXZ = motion.BakeHorizontalPosition,
                        lockRootHeightY = true,
                        keepOriginalPositionY = true,
                        events = Array.Empty<AnimationEvent>()
                    });
                }
                importer.clipAnimations = configuredClips.ToArray();
                importer.SaveAndReimport();
                ValidateAvatar(sourcePath);

                foreach (AnimationClip importedClip in AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>())
                    if (configuredClips.Any(configured => configured.name == importedClip.name))
                        importedByOutput[importedClip.name] = importedClip;
            }

            var result = new Dictionary<string, AnimationClip>();
            foreach (string name in ClipNames)
            {
                string path = GeneratedRoot + "/Animations/" + name + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null || refreshGeneratedMotions)
                {
                    if (!importedByOutput.TryGetValue(name, out AnimationClip source) ||
                        source == null || !source.humanMotion || source.length <= 0f)
                        throw new InvalidOperationException($"Motion '{name}' did not import as Humanoid. Check the animation FBX Rig tab.");
                    if (clip == null)
                    {
                        clip = UnityEngine.Object.Instantiate(source);
                        clip.name = name;
                        AssetDatabase.CreateAsset(clip, path);
                    }
                    else
                    {
                        // Preserve the GUID referenced by the Animator controller.
                        EditorUtility.CopySerialized(source, clip);
                        clip.name = name;
                        EditorUtility.SetDirty(clip);
                    }
                }
                result.Add(name, clip);
            }
            return result;
        }

        private static void ExtractAnimations()
        {
            // Keep the original 23 MB FBX byte-for-byte inside a 4.8 MB archive. The extracted editor
            // source is ignored by Git; only the selected standalone .anim clips can enter a player build.
            ExtractAnimationSource(
                SourceRoot + "/Animations/UAL1_Standard.zip",
                "UAL1_Standard.fbx",
                QuaterniusAnimationSourcePath);
            ExtractAnimationSource(
                SourceRoot + "/Animations2/UAL2_Standard.zip",
                "UAL2_Standard.fbx",
                Quaternius2AnimationSourcePath);
        }

        private static void ExtractAnimationSource(string archivePath, string entryName, string outputPath)
        {
            if (!File.Exists(archivePath)) throw new FileNotFoundException("Restore the bundled Quaternius animation archive.", archivePath);
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                if (entry == null) throw new InvalidDataException($"{archivePath} must contain {entryName}.");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                using (Stream source = entry.Open())
                using (var memory = new MemoryStream())
                {
                    source.CopyTo(memory);
                    byte[] bytes = memory.ToArray();
                    if (File.Exists(outputPath))
                    {
                        using (SHA256 hash = SHA256.Create())
                        {
                            if (hash.ComputeHash(bytes).SequenceEqual(hash.ComputeHash(File.ReadAllBytes(outputPath)))) return;
                        }
                    }
                    File.WriteAllBytes(outputPath, bytes);
                }
            }
        }

        private static ModelImporter ImportRig(
            string path,
            bool withAnimations,
            HumanBone[] humanMap,
            bool withMaterials = false,
            bool manualMapping = false,
            bool translationDof = false,
            bool deferReimport = false)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException($"Unity could not import {path} as an FBX model.");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // The imported MakeHuman and Quaternius rigs rely on Unity's proven auto mapping.
            // KayKit needs the explicit chest/wrist map below or Unity discards authored curves.
            importer.autoGenerateAvatarMappingIfUnspecified = !manualMapping;
            importer.importAnimation = withAnimations;
            importer.materialImportMode = withMaterials
                ? ModelImporterMaterialImportMode.ImportStandard
                : ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false; // Hand, chest and head must remain available for equipment.
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            // Unity 6.3's Optimal reducer produced two content hashes for the same manually
            // mapped KayKit FBX and raised an importer-consistency warning on every refresh.
            // The retained KayKit subset is small, so keep its source curves deterministic.
            importer.animationCompression = manualMapping
                ? ModelImporterAnimationCompression.Off
                : ModelImporterAnimationCompression.Optimal;

            HumanDescription description = importer.humanDescription;
            description.human = humanMap;
            // KayKit uses an explicit map to retain its intermediate chest curves. Manual Humanoid
            // mapping also needs the FBX bind pose; the other two sources keep their proven auto map.
            description.skeleton = manualMapping ? BuildSkeleton(path) : Array.Empty<SkeletonBone>();
            description.armStretch = 0.05f;
            description.legStretch = 0.05f;
            description.upperArmTwist = description.lowerArmTwist = 0.5f;
            description.upperLegTwist = description.lowerLegTwist = 0.5f;
            description.hasTranslationDoF = translationDof;
            importer.humanDescription = description;
            if (!deferReimport)
            {
                importer.SaveAndReimport();
                ValidateAvatar(path);
            }
            return importer;
        }

        private static void ValidateAvatar(string path)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException($"Invalid Humanoid Avatar in {path}. Open Rig > Configure and inspect the required bones.");
        }

        private static SkeletonBone[] BuildSkeleton(string path)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
                throw new InvalidOperationException($"Cannot read the source skeleton from {path}.");
            return source.GetComponentsInChildren<Transform>(true)
                .Select(transform => new SkeletonBone
                {
                    name = transform.name,
                    position = transform.localPosition,
                    rotation = transform.localRotation,
                    scale = transform.localScale
                })
                .ToArray();
        }

        internal static Avatar CreateOrUpdateCharacterAvatar(GameObject model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("The realistic male FBX does not contain a valid Unity Humanoid Avatar.");
            return avatar;
        }

        private static HumanBone[] RealisticMaleHumanMap()
        {
            var bones = new List<HumanBone>();
            void Add(string human, string bone)
            {
                var boneType = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), human.Replace(" ", ""));
                bones.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)boneType],
                    boneName = bone,
                    limit = new HumanLimit { useDefaultValues = true }
                });
            }
            Add("Hips", "hips");
            Add("Spine", "spine");
            Add("Chest", "chest");
            Add("UpperChest", "chest-1");
            Add("Neck", "neck");
            Add("Head", "head");
            foreach (string side in new[] { "Left", "Right" })
            {
                string suffix = side == "Left" ? ".L" : ".R";
                Add(side + "Shoulder", "clavicle" + suffix);
                Add(side + "UpperArm", "upper_arm" + suffix);
                Add(side + "LowerArm", "forearm" + suffix);
                Add(side + "Hand", "wrist" + suffix);
                Add(side + "UpperLeg", "thigh" + suffix);
                Add(side + "LowerLeg", "shin" + suffix);
                Add(side + "Foot", "foot" + suffix);
                Add(side + "Toes", "toe" + suffix);
                foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                {
                    string bone = finger switch
                    {
                        "Thumb" => "thumb",
                        "Index" => "f_index",
                        "Middle" => "f_middle",
                        "Ring" => "f_ring",
                        _ => "f_pinky"
                    };
                    Add(side + " " + finger + " Proximal", bone + ".01" + suffix);
                    Add(side + " " + finger + " Intermediate", bone + ".02" + suffix);
                    Add(side + " " + finger + " Distal", bone + ".03" + suffix);
                }
            }
            return bones.ToArray();
        }

        private static HumanBone[] HumanMap()
        {
            var bones = new List<HumanBone>();
            void Add(string human, string bone)
            {
                var boneType = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), human.Replace(" ", ""));
                bones.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)boneType],
                    boneName = bone,
                    limit = new HumanLimit { useDefaultValues = true }
                });
            }
            Add("Hips", "pelvis");
            Add("Spine", "spine_01");
            Add("Chest", "spine_02");
            Add("UpperChest", "spine_03");
            Add("Neck", "neck_01");
            Add("Head", "Head");
            foreach (string side in new[] { "Left", "Right" })
            {
                string suffix = side == "Left" ? "_l" : "_r";
                Add(side + "Shoulder", "clavicle" + suffix);
                Add(side + "UpperArm", "upperarm" + suffix);
                Add(side + "LowerArm", "lowerarm" + suffix);
                Add(side + "Hand", "hand" + suffix);
                Add(side + "UpperLeg", "thigh" + suffix);
                Add(side + "LowerLeg", "calf" + suffix);
                Add(side + "Foot", "foot" + suffix);
                Add(side + "Toes", "ball" + suffix);
                foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                {
                    string bone = finger == "Little" ? "pinky" : finger.ToLowerInvariant();
                    Add(side + " " + finger + " Proximal", bone + "_01" + suffix);
                    Add(side + " " + finger + " Intermediate", bone + "_02" + suffix);
                    Add(side + " " + finger + " Distal", bone + "_03" + suffix);
                }
            }
            return bones.ToArray();
        }

        private static HumanBone[] KayKitHumanMap()
        {
            var bones = new List<HumanBone>();
            void Add(string human, string bone)
            {
                var boneType = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), human.Replace(" ", ""));
                bones.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)boneType],
                    boneName = bone,
                    limit = new HumanLimit { useDefaultValues = true }
                });
            }
            Add("Hips", "hips");
            Add("Spine", "spine");
            Add("Chest", "chest");
            Add("Head", "head");
            foreach (string side in new[] { "Left", "Right" })
            {
                string suffix = side == "Left" ? ".l" : ".r";
                Add(side + "UpperArm", "upperarm" + suffix);
                Add(side + "LowerArm", "lowerarm" + suffix);
                // KayKit's warning-free Humanoid map ends at its wrist deform bone. The newer
                // UAL2 sword actions below use a complete palm-and-finger rig instead.
                Add(side + "Hand", "wrist" + suffix);
                Add(side + "UpperLeg", "upperleg" + suffix);
                Add(side + "LowerLeg", "lowerleg" + suffix);
                Add(side + "Foot", "foot" + suffix);
                Add(side + "Toes", "toes" + suffix);
            }
            return bones.ToArray();
        }
    }
}

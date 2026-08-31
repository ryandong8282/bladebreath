using System.IO;
using BladeBreath;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BladeBreathEditor
{
    public static class BladeBreathProjectSetup
    {
        private const string SceneFolder = "Assets/_BladeBreath/Scenes";
        private const string ScenePath = SceneFolder + "/CombatSandbox.unity";

        [MenuItem("BladeBreath/Prototype/Create Combat Sandbox")]
        public static void CreateCombatSandbox()
        {
            Directory.CreateDirectory(SceneFolder);
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("BladeBreath Prototype Bootstrap").AddComponent<PrototypeBootstrap>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"Could not save combat sandbox at {ScenePath}.");
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            ApplyProjectDefaults();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
            Debug.Log($"BladeBreath combat sandbox created: {ScenePath}");
        }

        [MenuItem("BladeBreath/Project/Apply iOS Defaults")]
        public static void ApplyProjectDefaults()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "ryandong8282";
            PlayerSettings.productName = "Wuming: Night of Zhangcheng";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.ryandong8282.bladebreath");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            QualitySettings.vSyncCount = 0;
            Debug.Log("BladeBreath project defaults applied: landscape, Linear color, iOS IL2CPP.");
        }
    }
}

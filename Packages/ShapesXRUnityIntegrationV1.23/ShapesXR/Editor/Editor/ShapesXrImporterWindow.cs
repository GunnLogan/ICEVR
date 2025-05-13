
using ShapesXR.Import.Settings;
using UnityEditor;
using UnityEngine;

namespace ShapesXR.Import.Editor
{
    public class ShapesXrImporterWindow : EditorWindow
    {
        private const string ShapesXRPluginSettingsOpenedKey = "shapes_xr_plugin_settings_opened";
        private const string AccessCodeKey = "shapes_xr_plugin_space_access_code";

        private static SerializedObject _importSettingsObject = null!;

        private static SerializedProperty _materialMapProperty = null!;
        private static SerializedProperty _importedDataDirectoryProperty = null!;
        private static SerializedProperty _materialModeProperty = null!;
        private static SerializedProperty _gltfMaterialTextureProperties = null!;

        private static string? ErrorMessage { get; set; } = "";

        private static bool SettingsOpened
        {
            get => EditorPrefs.GetBool(ShapesXRPluginSettingsOpenedKey);
            set => EditorPrefs.SetBool(ShapesXRPluginSettingsOpenedKey, value);
        }

        private static string AccessCode
        {
            get => EditorPrefs.GetString(AccessCodeKey);
            set => EditorPrefs.SetString(AccessCodeKey, value);
        }

        private void OnEnable()
        {
            _importSettingsObject = new SerializedObject(ImportSettings.Instance);

            _importedDataDirectoryProperty = _importSettingsObject.FindProperty("_importedDataDirectory");
            _materialMapProperty = _importSettingsObject.FindProperty("_materialMap");
            _materialModeProperty = _importSettingsObject.FindProperty("_materialMode");
            _gltfMaterialTextureProperties = _importSettingsObject.FindProperty("_gltfMainTextureProperties");

            ImportSettingsProvider.ImportSettings = ImportSettings.Instance;
        }

        private void OnGUI()
        {
            _importSettingsObject.Update();

            EditorGUILayout.BeginVertical();

            DrawLogo();
            if (!string.IsNullOrEmpty(SpaceImporter.ErrorMessage))
                ErrorMessage = SpaceImporter.ErrorMessage;

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                EditorGUILayout.HelpBox(ErrorMessage, MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();

            AccessCode = GUILayout.TextField(AccessCode);

            if (GUILayout.Button("Import Space"))
            {
                ErrorMessage = "";

                _importSettingsObject.ApplyModifiedProperties();

                var trimmedCode = AccessCode.Trim().Replace(" ", "");

                if (string.IsNullOrEmpty(trimmedCode))
                {
                    Analytics.SendEvent(EventStatus.incorrect_code_signature);
                    ErrorMessage = "You haven't entered a code. Enter the code to import space";

                    Debug.LogError(ErrorMessage);
                }
                else
                    SpaceImporter.ImportSpace(AccessCode.Replace(" ", "").ToLower());
            }

            EditorGUILayout.EndHorizontal();

            SettingsOpened = EditorGUILayout.Foldout(SettingsOpened, "Settings", EditorStyles.foldoutHeader);

            if (!SettingsOpened)
            {
                EditorGUILayout.EndVertical();
                _importSettingsObject.ApplyModifiedProperties();

                return;
            }

            try
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(_importedDataDirectoryProperty);
                SpaceImporter.SendAnalytics = EditorGUILayout.Toggle("Send Analytics", SpaceImporter.SendAnalytics);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(_materialModeProperty, new GUIContent("Import Mode"));
                EditorGUILayout.PropertyField(_materialMapProperty, new GUIContent("Material Map"));

                GUILayout.Space(10f);
                EditorGUILayout.HelpBox(
                    "If you're using GLB/glTF models in your space and want to import them with textures, you can add material properties for main textures that your GLB/glTF plugin is using below.\n\n" +
                    "Please note that more than one texture per renderer is not supported for import right now.",
                    MessageType.Info
                );

                EditorGUILayout.PropertyField(_gltfMaterialTextureProperties, new GUIContent("GLB/glTF Material Texture Properties:"));

                var rawString = _importedDataDirectoryProperty.stringValue;
                _importedDataDirectoryProperty.stringValue = rawString.Trim(' ', '\\', '/');
                

                _importSettingsObject.ApplyModifiedProperties();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        [MenuItem("ShapesXR/Importer")]
        public static void ShowWindow()
        {
            GetWindow<ShapesXrImporterWindow>(false, "ShapesXR Importer", true);
        }

        private void DrawLogo()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.Label(ImportResources.ShapesXrLogo);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }
    }
}
using System.IO;
using ShapesXR.Import;

using UnityEditor;
using UnityEngine;

namespace ShapesXR.Editor
{
    [CustomEditor(typeof(SpaceDescriptor))]
    public class SpaceDescriptorEditor : UnityEditor.Editor
    {
        private SpaceDescriptor? _spaceDescriptor;
        
        private void OnEnable()
        {
            _spaceDescriptor = serializedObject.targetObject as SpaceDescriptor;
        }

        public override void OnInspectorGUI()
        {
            if (!_spaceDescriptor)
            {
                return;
            }

            var sceneCount = _spaceDescriptor!.SceneObjects.Count;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.SelectableLabel($"Space: {_spaceDescriptor.AccessCode.Insert(3, " ")}", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(sceneCount == 0);

            _spaceDescriptor.ActiveScene = EditorGUILayout.IntSlider(
                $"Scene: {_spaceDescriptor.ActiveScene + 1}/{sceneCount}",
                _spaceDescriptor.ActiveScene + 1,
                1,
                sceneCount
            ) - 1;

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            if (GUILayout.Button("Reimport"))
                Reimport();
            EditorGUILayout.EndVertical();
        }

        private void Reimport()
        {
            if (_spaceDescriptor == null)
                return;
            
            var spaceDataPath = PathUtils.GetSpaceDataPath(_spaceDescriptor);

            if (Directory.Exists(spaceDataPath))
                Directory.Delete(spaceDataPath, true);
            
            var metaPath = spaceDataPath + ".meta";

            if (File.Exists(metaPath))
                File.Delete(metaPath);
            
            SpaceImporter.ImportSpace(_spaceDescriptor.AccessCode.ToLower());
            DestroyImmediate(_spaceDescriptor.gameObject);
        }
    }
}
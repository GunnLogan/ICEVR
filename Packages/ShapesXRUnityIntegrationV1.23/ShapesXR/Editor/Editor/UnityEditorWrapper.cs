using ShapesXR.ImportCore.Utils;
using UnityEditor;
using UnityEngine;

namespace ShapesXR.Editor.Editor
{
    public class UnityEditorWrapper : IUnityEditorWrapper
    {
        public T? LoadAssetAtPath<T>(string assetPath) where T : Object => AssetDatabase.LoadAssetAtPath<T>(assetPath);

        public void CreateAsset(Object asset, string path) => AssetDatabase.CreateAsset(asset, path);

        public void ImportAsset(string path, int importAssetOption = 0) => AssetDatabase.ImportAsset(path, (ImportAssetOptions)importAssetOption);

        public void StartAssetEditing() => AssetDatabase.StartAssetEditing();

        public void StopAssetEditing() => AssetDatabase.StopAssetEditing();
    }
}
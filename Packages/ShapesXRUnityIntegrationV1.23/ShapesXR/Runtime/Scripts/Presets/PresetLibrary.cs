using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace ShapesXR.Import.Presets
{
    public class PresetLibrary : ScriptableObject
    {
        [SerializeField] private List<BasePreset> _presets = new();

        private readonly Dictionary<Guid, BasePreset> _presetLib = new();

        public List<BasePreset> Presets => _presets;

        public bool TryGetPreset(Guid guid, out BasePreset preset)
        {
            if (_presetLib.Count == 0)
            {
                foreach (var prefab in Presets)
                    _presetLib[prefab.PresetID] = prefab;
            }
            
            return _presetLib.TryGetValue(guid, out preset);
        }

#if UNITY_EDITOR
        public void AddPresetsFromUnityAssetDatabase()
        {
            _presets.Clear();

            var guids = new List<string>(AssetDatabase.FindAssets("t:BasePreset"));
            var counter = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == null)
                    continue;

                var preset = AssetDatabase.LoadAssetAtPath<BasePreset>(path);
                if (preset != null)
                {
                    _presets.Add(preset);
                    counter++;
                }
            }

            if (counter > 0)
                Debug.Log($"Found {counter} prefabs with {typeof(BasePreset).Name} component for the local library of assets");
            else
                Debug.Log($"Haven't found any prefabs with {typeof(BasePreset).Name} component for the local library of assets");
        }
#endif
    }
}
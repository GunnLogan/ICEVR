using System;
using Ti.Core.SerializableData;
using UnityEditor;
using UnityEngine;

namespace ShapesXR.Import.Presets
{
    public class BasePreset : ScriptableObject
    {
        [Header("Base settings")]
        [SerializeField] protected GameObject _asset = null!;
        [SerializeField] protected string _displayName = null!;
        [SerializeField] protected Texture2D _thumbnail = null!;
        [SerializeField] protected bool _visibleInLibrary = true;

        // Let's keep it just in case
        [Obsolete, SerializeField, HideInInspector] private byte[] _serializedGuid = new byte[16];
        [SerializeField, HideInInspector] private SerializableGuid _guid;

        public virtual GameObject Asset => _asset;
        public Guid PresetID => _guid;
        public string Name => !string.IsNullOrEmpty(_displayName) ? _displayName : name;
        
        public Texture2D Thumbnail 
        { 
            get => _thumbnail;
            set => _thumbnail = value;
        }
        public bool VisibleInLibrary => _visibleInLibrary;

#if UNITY_EDITOR
        public void Initialize(GameObject target)
        {
            _asset = target;
        }

        private void OnValidate()
        {
            EditorUtility.SetDirty(this);
        }

        public void GenerateID()
        {
            _guid = Guid.NewGuid();
            
            Debug.Log($"Generated new GUID for {name} prefab. GUID: {_guid}");
        }

        [ContextMenu("Copy Preset Id")]
        public void CopyPresetId()
        {
            GUIUtility.systemCopyBuffer = _guid.ToString();
        }

        [ContextMenu("Re-generate Preset Id (Very Dangerous)")]
        public void RegeneratePresetId()
        {
            GenerateID();
        }

        [ContextMenu("Migrate Preset Id From Old (Very Dangerous)")]
        public void MigratePresetId()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            _guid = Guid.Parse(_serializedGuid.ToString());
#pragma warning restore CS0612 // Type or member is obsolete
        }
#endif
    }
}

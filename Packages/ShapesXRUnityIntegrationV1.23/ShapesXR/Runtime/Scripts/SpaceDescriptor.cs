using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ShapesXR.Import;
using ShapesXR.Import.Presets;
using ShapesXR.Import.Settings;
using ShapesXR.ImportCore.Entities;
using Ti.Common.Modules.Spaces.Models;
using Ti.Common.Modules.Spaces.Props;
using UnityEngine;
using Space = ShapesXR.Import.Space;

namespace ShapesXR
{
    public class SpaceDescriptor : MonoBehaviour
#if UNITY_EDITOR
    , ISpaceDescriptor
    {
        [SerializeField, HideInInspector] private List<GameObject> _sceneObjects = new();
        [SerializeField, HideInInspector] private string _accessCode = null!;
        [SerializeField, HideInInspector] private string _instanceId = null!;
        [SerializeField] private int _activeScene;

        private readonly Dictionary<Guid, GameObject> _gameObjects = new();

        private Space Space { get; set; } = null!;
        
        public List<GameObject> SceneObjects => _sceneObjects;
        public IPropertyHub PropertyHub => Space.PropertyHub;
        public InstanceCache InstanceCache => Space.InstanceCache;
        public Dictionary<Guid, Resource> Resources => Space.Resources;
        public IEnumerable<SpaceObject> Objects => Space.GetEntities<SpaceObject>(EntityType.Object);
        public Dictionary<Guid, BasePreset?> ObjectPresets { get; } = new();
        public Dictionary<Guid, Guid> ObjectParents { get; } = new();

        public SpaceObject? GetObject(Guid entityId) => Space.GetEntity<SpaceObject>(entityId);
        
        public bool GetObject(Guid entityId, [NotNullWhen(true)] out SpaceObject? spaceObject) =>
            Space.TryGetEntity(entityId, out spaceObject);

        public GameObject? GetGameObject(Guid entityId) => _gameObjects.GetValueOrDefault(entityId);

        public void AddGameObjectForEntity(Guid entityId, GameObject go, string gameObjectName)
        {
            go.name = gameObjectName;
            _gameObjects[entityId] = go;
            
            var spaceObject = GetObject(entityId);
            if (spaceObject != null)
            {
                // Need to add name to space object to correctly name materials after
                spaceObject.SetName(go.name); 
            }
        }

        public string AccessCode
        {
            get => _accessCode;
            private set
            {
                _accessCode = value;
                _instanceId = Guid.NewGuid().ToString().Substring(0, 8);
            }
        }

        public string InstanceId => _instanceId;

        public static SpaceDescriptor Create(Space space, string accessCode)
        {
            var  descriptor = Instantiate(ImportResources.SpaceDescriptorPrefab);
            descriptor.name = $"Space - {accessCode}";
            var instance = descriptor.GetComponent<SpaceDescriptor>();
            instance.Space = space;
            instance.AccessCode = accessCode;
            return instance;
        }

        public int ActiveScene
        {
            get => _activeScene;
            set
            {
                _activeScene = value;
                
                for (var i = 0; i < SceneObjects.Count; i++)
                {
                    if (SceneObjects[i])
                        SceneObjects[i].SetActive(_activeScene == i);
                }
            }
        }
        
        public void ReadObjectProperties()
        {
            foreach (var obj in Objects)
            {
                var presetId = obj.Get<Guid>(Properties.PRESET_GUID);
                ImportResources.PresetLibrary.TryGetPreset(presetId, out var preset);
                
                ObjectPresets[obj.Id] = preset;

                var parentId = obj.Get<ParentInfo>(Properties.PARENT).ParentId;
                if (parentId != default)
                    ObjectParents[obj.Id] = parentId;
            }
        }
    }
#else
        {}
#endif
}
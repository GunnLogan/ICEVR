using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ShapesXR.Common;
using ShapesXR.Common.ProceduralMesh;
using ShapesXR.Common.Reactors;
using ShapesXR.Import.Initializers;
using ShapesXR.Import.Presets;
using ShapesXR.Import.Presets.Object;
using ShapesXR.Import.Presets.Special;
using ShapesXR.Import.Presets.Staging;
using ShapesXR.Import.Settings;
using Ti.Common.Modules.Spaces.Models;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Properties = Ti.Common.Modules.Spaces.Props.Properties;

namespace ShapesXR.Import
{
    public class ObjectSpawner
    {
        private readonly SpaceDescriptor _spaceDescriptor;
        private readonly List<Guid> _specialObjectIds = new();
        private readonly HashSet<Guid> _groups = new();
        private readonly InitializerFactory _initializerFactory;
            
        public ObjectSpawner(SpaceDescriptor spaceDescriptor)
        {
            _spaceDescriptor = spaceDescriptor;
            _initializerFactory = new InitializerFactory();
            
            SpawnAllObjects();
        }

        private void SpawnAllObjects()
        {
            SpawnViewpoints();
            SpawnScenes();
            
            var objectCounter = 0;
            foreach (var obj in _spaceDescriptor.Objects)
            {
                var objectId = obj.Id;
                if (_specialObjectIds.Contains(objectId) || !NeedsSpawning(objectId))
                    continue;

                objectCounter++;

                var preset = _spaceDescriptor.ObjectPresets.GetValueOrDefault(objectId);
                var name = preset != null ? $"{preset.Name} {objectCounter}" : 
                    $"Object {objectCounter}";
                    
                SpawnObject(objectId, name);
                
                if (preset == null)
                {
                    Debug.LogWarning($"Preset not found for object '{objectId}'. Skipping initialization.");
                    continue;
                }

                if (NeedsInitialization(objectId))
                    InitializeObject(objectId);
            }

            InitializeGroups();

            foreach (var toRemove in _spaceDescriptor.gameObject.GetComponentsInChildren<RemoveAfterImportBehaviour>(true))
                Object.DestroyImmediate(toRemove);
            
            // This is needed to enable correct objects for current active scene in descriptor
            _spaceDescriptor.ActiveScene = _spaceDescriptor.ActiveScene;
        }

        private void SpawnViewpoints()
        {
            var spaceRoot = _spaceDescriptor.transform;

            var viewpointsRoot = new GameObject("Viewpoints");
            viewpointsRoot.transform.SetParent(spaceRoot);
            viewpointsRoot.transform.ResetLocalTransform();

            var spaceId = Ti.Common.Modules.Spaces.Constants.Entities.SpaceEntityGuid;
            var viewpointIds = _spaceDescriptor.PropertyHub.Get<Guid[]>(spaceId, Properties.Space.VIEWPOINT_ORDER_GUID_ARR);

            if (viewpointIds == null || viewpointIds.Length == 0)
            {
                Debug.LogWarning("Failed to find any viewpoints in space!");
                return;
            }

            for (int i = 0; i < viewpointIds.Length; i++)
            {
                var id = viewpointIds[i];
                SpawnObject(id, $"Viewpoint {i + 1}", viewpointsRoot.transform);
                _specialObjectIds.Add(id);
            }
        }

        private void SpawnScenes()
        {
            var spaceRoot = _spaceDescriptor.transform;
            var scenesRoot = new GameObject("Scenes");
            scenesRoot.transform.SetParent(spaceRoot);
            scenesRoot.transform.ResetLocalTransform();
            
            var spaceId = Ti.Common.Modules.Spaces.Constants.Entities.SpaceEntityGuid;
            var sceneIds = _spaceDescriptor.PropertyHub.Get<Guid[]>(spaceId, Properties.Space.SCENE_ORDER_GUID_ARR);

            if (sceneIds == null || sceneIds.Length == 0)
            {
                Debug.LogWarning("Failed to find any scenes in space!");
                return;
            }

            for (var i = 0; i < sceneIds.Length; i++)
            {
                var sceneID = sceneIds[i];
                var sceneObject = SpawnObject(sceneID, $"Scene {i}");
                
                _specialObjectIds.Add(sceneID);
                _spaceDescriptor.SceneObjects.Add(sceneObject);
            }
        }

        private GameObject SpawnObject(Guid objectId, string name, Transform? rootTransform = null)
        {
            if (rootTransform == null)
            {
                var sceneId = _spaceDescriptor.PropertyHub.Get<Guid>(objectId, Properties.SCENE_GUID);
                if (sceneId != default)
                {
                    var scene = _spaceDescriptor.GetGameObject(sceneId);
                    if (scene != null)
                        rootTransform = scene.transform;
                    else
                        Debug.LogWarning($"Stage with Id '{sceneId}' no longer exists but object '{objectId}' has reference to it. Putting object to space root");
                }
            }

            if (rootTransform == null)
                rootTransform = _spaceDescriptor.transform;

            var gameObject = Object.Instantiate(ImportResources.EmptyObjectPrefab, rootTransform);
            var transformInfo = _spaceDescriptor.PropertyHub.Get<TransformInfo>(objectId, Properties.TRANSFORM);

            gameObject.transform.localPosition = transformInfo.LocalPosition;
            gameObject.transform.localRotation = transformInfo.LocalRotation;
            gameObject.transform.localScale = transformInfo.LocalScale;

            _spaceDescriptor.AddGameObjectForEntity(objectId, gameObject, name);

            return gameObject;
        }

        private void InitializeObject(Guid objectId)
        {
            var objectContainer = _spaceDescriptor.GetGameObject(objectId)!;
            var preset = _spaceDescriptor.ObjectPresets[objectId];

            GameObject instance;
             // Temp solution for icon presets
            if (preset is ImagePreset image)
            {
                instance = Object.Instantiate(ImportResources.ImageAssetContainer, objectContainer.transform);
                instance.transform.ResetLocalTransform();
                
                var texture = image.Image;
                if (texture != null) // Adjust aspect ratio to texture
                {
                    var currentScale = instance.transform.localScale;
                    instance.transform.localScale = new Vector3(
                        currentScale.x,
                        currentScale.y * texture.height / texture.width,
                        currentScale.z
                    );
                }
            }
            else if (preset is IconPreset icon)
            {
                instance = Object.Instantiate(ImportResources.IconAssetContainer, objectContainer.transform);
                var mesh = instance.GetComponentInChildren<ProceduralMeshBase>();
                ((SVGGenerator)mesh.Generator).SetSprite(icon.Icon!);
                
                mesh.SetDirty();
                mesh.ForceRefresh();
                instance.transform.ResetLocalTransform();
            }
            else if (preset is ProceduralMeshPreset proceduralMeshPreset)
            {
                instance = Object.Instantiate(preset.Asset, objectContainer.transform);
                var mesh = instance.GetComponentInChildren<IProceduralMesh>();
                ProceduralMeshObjectInitializer.InitializeFromPreset(mesh, proceduralMeshPreset);
                mesh.ForceRefresh();
                instance.transform.ResetLocalTransform();
            }
            else
            {
                instance = Object.Instantiate(preset!.Asset, objectContainer.transform);
                instance.transform.ResetLocalTransform();
            }
            
            instance.name = preset.name;
                
            InitializeReactors(objectId, preset, instance);
        }

        private void InitializeGroups()
        {
            var reactorsToDestroy = new HashSet<Component>();
            
            foreach (var (child, parent) in _spaceDescriptor.ObjectParents)
            {
                var parentObject = _spaceDescriptor.GetGameObject(parent);
                if (parentObject == null)
                    continue;
                
                var reactor = parentObject.transform.GetComponentInChildren<GroupPropertyReactor>(true);
                if (reactor == null)
                    continue;
                
                var groupPreset = _spaceDescriptor.ObjectPresets[parent]!;
                var initializer = _initializerFactory.GetInitializer(reactor, groupPreset);
                initializer?.Initialize(_spaceDescriptor, child, reactor.gameObject);

                reactorsToDestroy.Add(reactor);
                _groups.Remove(parent);
            }

            // Destroy reactor gameobjects because objects are parented under space object
            foreach (var reactor in reactorsToDestroy)
                Object.DestroyImmediate(reactor.gameObject);

            // destroy empty groups
            foreach (var groupId in _groups)
            {
                var go = _spaceDescriptor.GetGameObject(groupId);
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        private void InitializeReactors(Guid objectId, BasePreset preset, GameObject createdObject)
        {
            var reactors = createdObject.transform.parent.GetComponentsInChildren<PropertyReactorComponent>(true);

            IInitializer? initializer;
            SpaceMaterialReactor materialReactor = null!;

            foreach (var reactor in reactors)
            {
                if (reactor is SpaceMaterialReactor mr)
                {
                    materialReactor = mr;
                    continue;
                }

                // Groups are initialized separately
                if (reactor is GroupPropertyReactor)
                {
                    _groups.Add(objectId);
                    continue;
                }

                initializer = _initializerFactory.GetInitializer(reactor, preset);
                initializer?.Initialize(_spaceDescriptor, objectId, reactor.gameObject);
            }

            if (materialReactor == null)
            {
                return;
            }

            initializer = _initializerFactory.GetInitializer(materialReactor, preset);
            initializer?.Initialize(_spaceDescriptor, objectId, materialReactor.gameObject);
        }

        private bool NeedsInitialization(Guid objectId) => _spaceDescriptor.ObjectPresets[objectId] is not ScenePreset;

        private bool NeedsSpawning(Guid objectId)
        {
            var preset = _spaceDescriptor.ObjectPresets[objectId];
            return preset != null && preset is not EnvironmentSettingsPreset;
        }
    }
}
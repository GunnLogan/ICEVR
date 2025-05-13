using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShapesXR.Import.Presets;
using ShapesXR.Import.Presets.Object;
using Ti.Common.Modules.Spaces.Props;
using UnityEngine;

namespace ShapesXR.Import.Resources
{
    public static class ResourceDownloader
    {
        private static readonly HashSet<Guid> DownloadingResources = new();

        public static void DownloadAllResources(SpaceDescriptor spaceDescriptor)
        {
            foreach (var kvp in spaceDescriptor.ObjectPresets)
            {
                if (kvp.Value is not ResourcePreset preset)
                {
                    continue;
                }

                var objectId = kvp.Key;
                var resourceId = spaceDescriptor.PropertyHub.Get<Guid>(objectId, Properties.RESOURCE_GUID);

                if (spaceDescriptor.Resources.ContainsKey(resourceId))
                {
                    continue;
                }

                var resourceResponse =
                    ResourceDownloaderHelper.DownloadResource(Configuration.ServerURL, spaceDescriptor, resourceId,
                        preset.ResourceType);

                if (resourceResponse == null)
                {
                    Debug.LogWarning($"Resource with id {resourceId} not found on server. Skipping object import");
                    continue;
                }

                var resource = new Resource(preset.ResourceType, resourceId, resourceResponse);
                spaceDescriptor.Resources.Add(resourceId, resource);
            }
        }

        public static void DownloadAllResourcesInParallel(SpaceDescriptor spaceDescriptor)
        {
            var presetsToDownload = spaceDescriptor.ObjectPresets.Where(
                p => p.Value is ResourcePreset
            ).ToList();
            
            DownloadingResources.Clear();

            var tasks = presetsToDownload.Select(kvp => Task.Run(() => DownloadAndAddResource(spaceDescriptor, kvp))).ToList();
            while (!tasks.TrueForAll(t => t.IsCompleted))
            {
            }
        }

        private static async Task DownloadAndAddResource(ISpaceDescriptor spaceDescriptor, KeyValuePair<Guid, BasePreset?> kvp)
        {
            var resource = await DownloadResourcePresetAsync(spaceDescriptor, kvp.Key, kvp.Value!);
            if (resource != null)
                spaceDescriptor.Resources.Add(resource.Id, resource);
        }
        
        private static async Task<Resource?> DownloadResourcePresetAsync(ISpaceDescriptor spaceDescriptor, Guid objectId, BasePreset preset)
        {
            var resourceId = spaceDescriptor.PropertyHub.Get<Guid>(objectId, Properties.RESOURCE_GUID);
            if (spaceDescriptor.Resources.ContainsKey(resourceId) || DownloadingResources.Contains(resourceId))
            {
                return null;
            }

            DownloadingResources.Add(resourceId);

            var resourceType = ((ResourcePreset)preset).ResourceType;
            var resourceResponse = await ResourceDownloaderHelper.DownloadResourceAsync(
                Configuration.ServerURL, spaceDescriptor, resourceId,
                resourceType
            );

            if (resourceResponse == null)
            {
                Debug.LogWarning($"Resource with id {resourceId} not found on server. Skipping object import");
                return null;
            }

            return new Resource(resourceType, resourceId, resourceResponse);
        }
    }
}
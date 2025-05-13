using System;
using System.Collections.Generic;
using System.Linq;
using ShapesXR.Common.Reactors;
using ShapesXr.Import;
using ShapesXR.Import.Presets;
using ShapesXR.Import.Presets.Brush;
using ShapesXR.Import.Presets.Object;
using Ti.Core.Materials.MaterialsCollection;
using UnityEngine;

namespace ShapesXR.Import.Initializers
{
    public class InitializerFactory
    {
        // These should work in unity 2020
        // ReSharper disable ArrangeObjectCreationWhenTypeEvident
        private static readonly SizeInitializer SizeInitializer = new SizeInitializer();
        private static readonly TextInitializer TextInitializer = new TextInitializer();
        private static readonly ImageResourceInitializer ImageResourceInitializer = new ImageResourceInitializer();
        private static readonly ModelResourceInitializer ModelResourceInitializer = new ModelResourceInitializer();
        private static readonly ProceduralMeshObjectInitializer ProceduralObjectInitializer = new ProceduralMeshObjectInitializer();
        private static readonly IconInitializer IconInitializer = new IconInitializer();
        private static readonly GroupInitializer GroupInitializer = new GroupInitializer();
        // ReSharper restore ArrangeObjectCreationWhenTypeEvident

        public IInitializer? GetInitializer(PropertyReactorComponent reactor, BasePreset preset)
        {
            switch (reactor)
            {
                case CharacterReactor:
                    if (preset is CharacterPreset characterPreset)
                        return new CharacterInitializer(characterPreset.Pose);
                    
                    return new NullInitializer(preset.name);
                case SizePropertyReactor:
                    return SizeInitializer;
                case TextReactorComponent:
                    return TextInitializer;
                case StrokePropertyReactor:
                    return GetStrokeInitializer(preset);
                // Image container is used both for resources and ordinary images
                case ImageReactor:
                {
                    // Images do not need any initialization
                    return preset is ImagePreset ? null : ImageResourceInitializer;
                }
                case FigmaObjectReactor:
                    return ImageResourceInitializer;
                case ModelReactor:
                    return ModelResourceInitializer;
                case SpaceMaterialReactor r:
                    return GetMaterialInitializer(r, preset);
                case GroupPropertyReactor:
                    return GroupInitializer;
                case ProceduralMeshReactor:
                    return preset switch
                    {
                        IconPreset => IconInitializer,
                        _ => ProceduralObjectInitializer
                    };
                default:
                    return new NullInitializer(reactor.ToString());
            }
        }
        
        private IInitializer GetStrokeInitializer(BasePreset preset)
        {
            if (preset is BaseBrushPreset p)
                return new StrokeInitializer(p.GetParameters());

            return new NullInitializer(preset.ToString());
        }

        private IInitializer GetMaterialInitializer(SpaceMaterialReactor reactor, BasePreset preset)
        {
            List<List<Renderer>>? rendererGroups = null;

            // Get renderers either from group or controller(s)
            var materialGroup = reactor.GetComponent<SpaceMaterialGroup>();
            var materialController = reactor.GetComponent<SpaceMaterialController>();
            var materialControllers = materialGroup == null ?
                materialController == null ? Array.Empty<SpaceMaterialController>() : new[] { materialController } :
                materialGroup.MaterialControllers.ToArray();

            if (!materialControllers.IsNullOrEmpty())
            {
                rendererGroups = new List<List<Renderer>>();

                foreach (var controller in materialControllers)
                {
                    rendererGroups.Add(new List<Renderer>(controller.Renderers));
                    UnityEngine.Object.DestroyImmediate(controller);
                }

                if (materialGroup != null)
                    UnityEngine.Object.DestroyImmediate(materialGroup);
            }
            else
                rendererGroups = new List<List<Renderer>> { new(reactor.GetComponentsInChildren<Renderer>().ToList()) };

            if (preset is ResourcePreset { ResourceType: ResourceType.Image or ResourceType.FigmaObject or ResourceType.Model } resourcePreset)
                return new ResourceMaterialInitializer(resourcePreset.ResourceType, rendererGroups);

            var textures = new List<Texture2D>();
            if (preset is ProceduralObjectPreset pObject && pObject.BaseMap != null)
                textures.Add(pObject.BaseMap!);

            var materialPropertiesPreset = preset switch
            {
                ImagePreset => MaterialPropertiesPreset.ImagePropertiesPreset,
                IconPreset => MaterialPropertiesPreset.IconPropertiesPreset,
                _ => MaterialPropertiesPreset.DefaultPropertiesPreset
            };

            return new ObjectMaterialInitializer(materialPropertiesPreset, rendererGroups, textures);
        }
    }
}
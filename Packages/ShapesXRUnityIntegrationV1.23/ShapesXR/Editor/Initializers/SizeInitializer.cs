using System;
using ShapesXR.Common;

using Ti.Common.Modules.Spaces.Props;
using UnityEngine;

namespace ShapesXR.Import.Initializers
{
    public sealed class SizeInitializer: IInitializer
    {
        public void Initialize(ISpaceDescriptor descriptor, Guid objectId, GameObject reactorObject)
        {
            var size = descriptor.PropertyHub.Get<Vector3>(objectId, Properties.BOUNDS_SIZE_VEC3);
            var stretchers = reactorObject.GetComponentsInChildren<UI3DStretcher>();

            foreach (var stretcher in stretchers)
            {
                stretcher.Size = size;

                if (stretcher.ColliderType == ColliderType.None)
                {
                    stretcher.ColliderType = ColliderType.Mesh;
                }
            }
        }
    }
}
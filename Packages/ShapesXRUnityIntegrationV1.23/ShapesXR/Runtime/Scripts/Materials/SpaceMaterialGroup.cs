using System.Collections.Generic;
using ShapesXR.Import;
using UnityEngine;

namespace Ti.Core.Materials.MaterialsCollection
{
    public class SpaceMaterialGroup : MonoBehaviour, IMaterialGroup
    {
        [SerializeField] private List<SpaceMaterialController> _controllers = new(0);

        public IReadOnlyList<SpaceMaterialController> MaterialControllers => _controllers;

        public void SetRenderers(List<List<Renderer>> renderers)
        {
            foreach (var controller in _controllers) Destroy(controller);
            _controllers.Clear();

            foreach (var group in renderers)
            {
                var controller = gameObject.AddComponent<SpaceMaterialController>();
                controller.Renderers.AddRange(group);
                _controllers.Add(controller);
            }
        }
    }
}
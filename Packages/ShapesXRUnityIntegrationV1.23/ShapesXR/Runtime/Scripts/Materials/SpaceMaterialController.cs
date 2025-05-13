using System.Collections.Generic;
using UnityEngine;

namespace Ti.Core.Materials.MaterialsCollection
{
    // Copy of SpaceMaterialController to reference renderers
    public class SpaceMaterialController : MonoBehaviour
    {
        [SerializeField] private List<Renderer> _renderers = new(0);

        public List<Renderer> Renderers => _renderers;
    }
}

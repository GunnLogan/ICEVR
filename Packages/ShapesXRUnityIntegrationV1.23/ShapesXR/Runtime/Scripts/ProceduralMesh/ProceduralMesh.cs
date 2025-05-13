using System.Collections.Generic;
using ShapesXR.Import;
using UnityEngine;
using UnityEngine.Animations;

namespace ShapesXR.Common.ProceduralMesh
{
    // [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ProceduralMesh : ProceduralMeshBase
    {
        [SerializeField] [NotKeyable] private RefreshMode _refreshMode;
        [SerializeField] [NotKeyable] private AdditionalVertexAttribute _additionalVertexAttributes;
        [SerializeReference] private IProceduralMeshGenerator _generator = new RectGenerator();
        [SerializeReference] private IProceduralMeshColorizer? _colorizer = null!;
        [SerializeReference] private List<IProceduralMeshDeformer> _deformers = new();

        protected override RefreshMode CurrentRefreshMode => _refreshMode;
        protected override AdditionalVertexAttribute AdditionalVertexAttributes => _additionalVertexAttributes;
        protected override IProceduralMeshColorizer? Colorizer => _colorizer;
        public override IProceduralMeshGenerator Generator => _generator;
        public override List<IProceduralMeshDeformer> Deformers => _deformers;
    }
}
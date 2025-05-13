
using Unity.Mathematics;
using UnityEngine;

namespace ShapesXR.Import.Presets.Object
{
    [CreateAssetMenu(menuName = "Assets/Preset Library/Procedural Mesh Preset")]
    public sealed class ProceduralMeshPreset : ProceduralObjectPreset, IProceduralMeshPreset
    {
        [Header("Procedural Mesh")]
        [SerializeField] private float2 _size = new (1f,1f);
        [SerializeField] private float _thickness = 1f;
        [SerializeField] private float _cornerRadius;
        [SerializeField] private bool4 _corners = new(true, true, true, true);
        [SerializeField] private bool _isCornerSharp;
        [SerializeField] private float _chamferRadius;
        [SerializeField] private bool _isChamferSharp;
        [SerializeField] private bool _generateBack = true;
        [SerializeField] private float _horizontalBend;
        [SerializeField] private float _verticalBend;

        public float2 Size => _size;
        public float Thickness => _thickness;
        public float CornerRadius => _cornerRadius;
        public bool4 Corners => _corners;
        public bool IsCornerSharp => _isCornerSharp;
        public float ChamferRadius => _chamferRadius;
        public bool IsChamferSharp => _isChamferSharp;
        public bool GenerateBack => _generateBack;
        public float HorizontalBend => _horizontalBend;
        public float VerticalBend => _verticalBend;
    }
}

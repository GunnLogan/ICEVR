using UnityEngine;

namespace ShapesXR.Import.Presets.Object
{
    public enum FitterType
    {
        BoundsFillFitted,
        UI3DStretcherFitted
    }
    
    public enum AspectLock
    {
        All,
        XY,
        XZ,
        YZ
    }

    public enum CreationSnappingAxis
    {
        X,
        Y,
        Z
    }
    
    public class ProceduralObjectPreset : BasePreset
    {
        [SerializeField] private FitterType _fitterType;
        [SerializeField] private AspectLock _preserveAspect;
        [SerializeField] private Texture2D? _baseMap;
        [SerializeField] private bool _applyToolMaterial;
        [SerializeField] private CreationSnappingAxis _creationSnappingUpAxis = CreationSnappingAxis.Y;
        [SerializeField, TextArea] private string? _license;
        
        public virtual Texture2D? BaseMap { get => _baseMap; set => _baseMap = value; }
        public string? License { get => _license; set => _license = value; }
        public CreationSnappingAxis CreationSnappingUpAxis => _creationSnappingUpAxis;
    }
}

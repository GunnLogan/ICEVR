using UnityEngine;

namespace ShapesXR.Import.Presets.Object
{
    [CreateAssetMenu(menuName = "Assets/Preset Library/Image Preset")]
    public sealed class ImagePreset : ProceduralObjectPreset
    {
        [SerializeField] private Texture2D? _image;

        public Texture2D? Image => _image;
        public override Texture2D? BaseMap => Image;
    }
}

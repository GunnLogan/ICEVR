using UnityEngine;

namespace ShapesXR.Import.Presets.Object
{
    [CreateAssetMenu(menuName = "Assets/Preset Library/Icon Preset")]
    public sealed class IconPreset : ProceduralObjectPreset
    {
        [SerializeField] private Sprite? _icon;

        public Sprite? Icon => _icon;
        public override Texture2D? BaseMap => Icon != null ? Icon.texture : null;
    }
}

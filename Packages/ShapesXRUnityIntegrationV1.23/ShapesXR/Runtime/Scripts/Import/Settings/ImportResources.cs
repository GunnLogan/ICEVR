using ShapesXR.Import.Presets;
using UnityEngine;

namespace ShapesXR.Import.Settings
{
    [CreateAssetMenu(fileName = "ImportResources", menuName = "ShapesXR/Import Resources")]
    public class ImportResources : ScriptableObject
    {
        public const string MtlNameTag = "<name>";
        
        [Header("Editor")]
        [SerializeField] private Texture2D _shapesXrLogo = null!;
        
        [Header("Space")]
        [SerializeField] private GameObject _spaceDescriptorPrefab = null!;

        [Header("Presets")] 
        [SerializeField] private PresetLibrary _presetLibrary = null!;

        [Header("Assets")] 
        [SerializeField] private GameObject _emptyObjectPrefab = null!;
        [SerializeField] private GameObject _imageAssetContainer = null!;
        [SerializeField] private GameObject _iconAssetContainer = null!;

        private static ImportResources _instance = null!;

        private static ImportResources Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = UnityEngine.Resources.Load<ImportResources>("ImportResources");
                }

                return _instance;
            }
        }

        public static Texture2D ShapesXrLogo => Instance._shapesXrLogo;
        
        public static GameObject SpaceDescriptorPrefab => Instance._spaceDescriptorPrefab;
        
        public static PresetLibrary PresetLibrary => Instance._presetLibrary;
        
        public static GameObject EmptyObjectPrefab => Instance._emptyObjectPrefab;
        public static GameObject ImageAssetContainer => Instance._imageAssetContainer;
        public static GameObject IconAssetContainer => Instance._iconAssetContainer;
    }
}
using UnityEngine;

namespace ShapesXR.Import.Presets.Object
{
    public class CharacterPreset : ProceduralObjectPreset
    {
        [SerializeField] private GameObject _pose = null!;

        public GameObject Pose => _pose;

#if UNITY_EDITOR
        public void SetPose(GameObject target)
        {
            _pose = target;
        }
#endif
    }
}
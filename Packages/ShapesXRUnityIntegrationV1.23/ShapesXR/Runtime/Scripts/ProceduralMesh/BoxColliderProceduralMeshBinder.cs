using ShapesXR.Import;

using UnityEngine;

namespace ShapesXR.Common.ProceduralMesh
{
    [ExecuteAlways]
    public sealed class BoxColliderProceduralMeshBinder : MonoBehaviour, IProceduralMeshComponent
    {
        [SerializeField] private ProceduralMesh? _proceduralMesh;
        [SerializeField] private BoxCollider? _boxCollider;
        [SerializeField] private Vector3 _margin = Vector3.zero;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                return;
            }
#endif
            
            if (_proceduralMesh != null)
            {
                _proceduralMesh.GenerationFinished += Refresh;
            }
            
            Refresh();
        }

        private void OnDisable()
        {
            if (_proceduralMesh != null)
                _proceduralMesh.GenerationFinished -= Refresh;
        }
        
#if UNITY_EDITOR
        private void Reset()
        {
            if (_proceduralMesh == null)
            {
                _proceduralMesh = GetComponent<ProceduralMesh>();                
            }

            if (_boxCollider)
            {
                _boxCollider = GetComponent<BoxCollider>();                
            }
            
            Refresh();
        }
        
        private void OnValidate()
        {
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                return;
            }
            
            if (!isActiveAndEnabled)
            {
                return;
            }
            
            UnityEditor.EditorApplication.delayCall += DelayedUpdate;
        }

        private void DelayedUpdate()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedUpdate;

            if (this == null)
            {
                return;
            }

            OnValidateDelayed();
        }

        private void OnValidateDelayed()
        {
            Refresh();
        }
#endif

        private void Refresh()
        {
            if (!isActiveAndEnabled || _boxCollider == null || _proceduralMesh == null)
            {
                return;
            }
            
            _boxCollider.center = _proceduralMesh!.OOBB.Center;
            _boxCollider.size = (Vector3)_proceduralMesh!.OOBB.Size + _margin;
        }
    }
}

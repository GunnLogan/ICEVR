using ShapesXR.Import;

using Unity.Jobs;
using UnityEngine;

namespace ShapesXR.Common.ProceduralMesh
{
    [ExecuteAlways]
    public sealed class MeshColliderProceduralMeshBinder : MonoBehaviour, IProceduralMeshComponent
    {
        [SerializeField] private ProceduralMesh? _proceduralMesh;
        [SerializeField] private MeshCollider? _meshCollider;
        
        private JobHandle? _bakeMeshJobHandle;

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
                Refresh(_proceduralMesh.Mesh);
                _proceduralMesh.GenerationFinished += OnGenerateFinished;
            }
        }

        private void OnDisable()
        {
            if (_proceduralMesh != null)
            {
                _proceduralMesh.GenerationFinished -= OnGenerateFinished;
            }
        }
        
        private void OnGenerateFinished()
        {
            Refresh(null);

            _bakeMeshJobHandle = PhysicsBakeMeshJob.Schedule(_proceduralMesh!.Mesh.GetInstanceID());
        }

        private void Update()
        {
            CompleteBaking();
        }

        private void CompleteBaking()
        {
            if (_bakeMeshJobHandle == null)
                return;
            
            _bakeMeshJobHandle.Value.Complete();
            _bakeMeshJobHandle = null;
            if (_proceduralMesh != null) Refresh(_proceduralMesh.Mesh);
        }

        private void Refresh(Mesh? mesh)
        {
            if (!isActiveAndEnabled || _meshCollider == null || _proceduralMesh == null || mesh?.vertices?.Length <= 0)
                return;

            _meshCollider.sharedMesh = mesh;
        }
    }
}

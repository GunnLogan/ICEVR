using UnityEngine;


[RequireComponent(typeof(MeshRenderer))]
public class FootstepVisualizer : MonoBehaviour
{
    private Material _mat;
    private static readonly int ID_StepCount     = Shader.PropertyToID("_StepCount");
    private static readonly int ID_TeleportStart = Shader.PropertyToID("_TeleportStart");
    private static readonly int ID_TeleportEnd   = Shader.PropertyToID("_TeleportEnd");

    void Awake()
    {
        // Ensure a unique material instance
        var rend = GetComponent<MeshRenderer>();
        _mat = rend.material = new Material(rend.sharedMaterial);

        // Start with zero steps
        _mat.SetFloat(ID_StepCount, 0f);
    }

    void Update()
    {
        // Pull the global step count and feed it into our material
        float globalSteps = Shader.GetGlobalFloat(ID_StepCount);
        _mat.SetFloat(ID_StepCount, globalSteps);

        // Update start/end positions too
        Vector4 startVec = Shader.GetGlobalVector(ID_TeleportStart);
        Vector4 endVec   = Shader.GetGlobalVector(ID_TeleportEnd);
        _mat.SetVector(ID_TeleportStart, startVec);
        _mat.SetVector(ID_TeleportEnd,   endVec);

        // Optional debug:
        // Debug.Log($"[FootstepViz] _StepCount = {globalSteps}");
    }
}

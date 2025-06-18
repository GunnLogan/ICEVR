using UnityEngine;

/// <summary>
/// Draws debug footprint gizmos evenly spaced between two world points.
/// Attach to any GameObject in your scene and set StartPoint and EndPoint.
/// </summary>
public class DebugFootstepTester : MonoBehaviour
{
    [Header("Debug Footstep Tester")]
    public Transform StartPoint;
    public Transform EndPoint;
    [Tooltip("Spacing between debug footprints in world units.")]
    public float StepLength = 1f;
    [Tooltip("Radius of each gizmo footprint.")]
    public float FootSize = 0.2f;

    void OnDrawGizmos()
    {
        if (StartPoint == null || EndPoint == null) return;

        Vector3 a = StartPoint.position;
        Vector3 b = EndPoint.position;
        float total = Vector3.Distance(a, b);
        if (total < 0.01f || StepLength <= 0f) return;

        Vector3 dir = (b - a).normalized;
        int count = Mathf.FloorToInt(total / StepLength);

        Gizmos.color = Color.yellow;
        for (int i = 0; i <= count; i++)
        {
            Vector3 pos = a + dir * StepLength * i;
            Gizmos.DrawWireSphere(pos, FootSize);
        }
    }
}
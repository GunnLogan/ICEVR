using System.Collections;
using UnityEngine;

public class TimedToggle : MonoBehaviour
{
    [Tooltip("The GameObjects to turn on/off")]
    public GameObject[] targets;

    [Tooltip("How long (in seconds) the targets stay on before auto-turning off")]
    public float activeDuration = 20f;

    /// <summary>
    /// Call this from your Button’s OnClick (or any UnityEvent) to start the toggle.
    /// </summary>
    public void OnButtonPressed()
    {
        StartCoroutine(ToggleRoutine());
    }

    private IEnumerator ToggleRoutine()
    {
        // turn on
        foreach (var go in targets)
            go.SetActive(true);

        // wait
        yield return new WaitForSeconds(activeDuration);

        // turn off
        foreach (var go in targets)
            go.SetActive(false);
    }
}
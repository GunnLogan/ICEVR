using UnityEngine;


public class AutoDisableNearFarInteractors : MonoBehaviour
{
    [Tooltip("Optional: Disable the whole GameObject, not just the component.")]
    public bool disableEntireGameObject = true;

    void Start()
    {
        // Find all XRRayInteractors in the scene (typical for Near-Far interaction)
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] rayInteractors = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(includeInactive: true);

        foreach (var interactor in rayInteractors)
        {
            // Optional: You can refine this with tags or names if needed
            string goName = interactor.gameObject.name.ToLower();
            if (goName.Contains("nearfar") || goName.Contains("ray") || goName.Contains("interactor"))
            {
                if (disableEntireGameObject)
                {
                    interactor.gameObject.SetActive(false);
                    Debug.Log($"Disabled Near-Far Interactor GameObject: {interactor.gameObject.name}");
                }
                else
                {
                    interactor.enabled = false;
                    Debug.Log($"Disabled XRRayInteractor component on: {interactor.gameObject.name}");
                }
            }
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;                    // ← for XRSimpleInteractable
using UnityEngine.XR.Interaction.Toolkit.Interactables;     // ← for the XRSimpleInteractable namespace

[RequireComponent(typeof(XRSimpleInteractable))]
public class SceneTouchTransition : MonoBehaviour
{
    [Header("References (drag & drop in Inspector)")]
    [Tooltip("A Quad or Plane with a white, Unlit/Color material. Should start inactive or alpha=0.")]
    public GameObject whitePlane;

    [Tooltip("OPTIONAL: Your VR Main Camera’s Transform. If left empty, this script will use Camera.main.")]
    public Transform xrCameraTransform;

    [Tooltip("Parent whose children you want to hide when the cube is pressed.")]
    public GameObject listARoot;

    [Tooltip("Parent whose children you want to show when the cube is pressed.")]
    public GameObject listBRoot;

    [Header("Fade Durations")]
    public float fadeInDuration  = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Plane Distance")]
    [Tooltip("How far (in meters) in front of the camera to place the white plane.")]
    public float planeDistance = 0.5f;

    // Internals
    private XRSimpleInteractable simpleInteractable;
    private Material            planeMaterial;
    private bool                hasFired = false;

    private void Awake()
    {
        // 1) If the user didn't assign a camera, try to find Camera.main
        if (xrCameraTransform == null)
        {
            if (Camera.main != null)
            {
                xrCameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogError(
                    $"[{nameof(SceneTouchTransition)}] → No xrCameraTransform assigned " +
                    "and Camera.main is null. Make sure your VR camera is tagged “MainCamera.”"
                );
            }
        }

        // 2) Cache the XRSimpleInteractable
        simpleInteractable = GetComponent<XRSimpleInteractable>();

        // 3) Subscribe to hoverEntered (or selectEntered if you prefer pinch-grab)
        simpleInteractable.hoverEntered.AddListener(OnHoverEntered);
        // If you want a full pinch rather than hover, comment out the above line and uncomment below:
        // simpleInteractable.selectEntered.AddListener(OnSelectEntered);

        // 4) Prepare the white plane’s material so we can fade its alpha
        if (whitePlane != null)
        {
            Renderer r = whitePlane.GetComponent<Renderer>();
            if (r != null)
            {
                // Clone the material so we don’t modify the original asset at runtime
                planeMaterial = Instantiate(r.material);
                r.material     = planeMaterial;

                // Start fully transparent
                Color c         = planeMaterial.color;
                c.a             = 0f;
                planeMaterial.color = c;
            }
            else
            {
                Debug.LogError($"[{nameof(SceneTouchTransition)}] → WhitePlane has no Renderer!");
            }

            // Make the plane inactive so it doesn’t block the view initially
            whitePlane.SetActive(false);
        }
        else
        {
            Debug.LogError($"[{nameof(SceneTouchTransition)}] → WhitePlane is not assigned in the Inspector!");
        }

        // 5) Ensure this object has a BoxCollider (IsTrigger = true) and a kinematic Rigidbody
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning(
                $"[{nameof(SceneTouchTransition)}] → No Collider found on '{gameObject.name}'. " +
                "Please add a BoxCollider (Is Trigger = true)."
            );
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning(
                $"[{nameof(SceneTouchTransition)}] → Collider on '{gameObject.name}' is not set to 'Is Trigger = true'. " +
                "Please enable 'Is Trigger' so hover/select works properly."
            );
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // Add a kinematic Rigidbody if missing
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (simpleInteractable != null)
        {
            simpleInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            // If using selectEntered, remove that as well:
            // simpleInteractable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    // Called when a hand interactor “hovers” (touches) this cube
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (hasFired) 
            return;

        hasFired = true;
        StartCoroutine(DoSceneTransition());
    }

    // If you prefer requiring a full pinch-grab instead of hover, use this:
    /*
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (hasFired) 
            return;
        hasFired = true;
        StartCoroutine(DoSceneTransition());
    }
    */

    private IEnumerator DoSceneTransition()
    {
        // Safety check
        if (whitePlane == null || xrCameraTransform == null)
        {
            Debug.LogError($"[{nameof(SceneTouchTransition)}] → Missing references! Assign WhitePlane and XRCameraTransform (or ensure Camera.main is tagged).");
            yield break;
        }

        // ─── Reset the plane’s alpha to 0 each time we start ───
        {
            // Ensure material is fully transparent at the very start
            Color resetColor       = planeMaterial.color;
            resetColor.a           = 0f;
            planeMaterial.color    = resetColor;

            // Also ensure the plane itself is not active before we begin
            whitePlane.SetActive(false);
        }

        // STEP 1: Activate + parent white plane in front of the camera
        whitePlane.SetActive(true);
        whitePlane.transform.SetParent(xrCameraTransform, worldPositionStays: false);
        whitePlane.transform.localPosition = new Vector3(0f, 0f, planeDistance);
        whitePlane.transform.localRotation = Quaternion.identity;
        whitePlane.transform.localScale = Vector3.one;

        // STEP 2: Fade INTO white (alpha: 0 → 1)
        float t = 0f;
        Color startColor = planeMaterial.color;  // alpha = 0 after we just reset it
        Color endColor   = startColor;
        endColor.a       = 1f;                   // target: fully opaque

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float lerpValue = Mathf.Clamp01(t / fadeInDuration);
            planeMaterial.color = Color.Lerp(startColor, endColor, lerpValue);
            yield return null;
        }
        planeMaterial.color = endColor; // ensure fully white

        // STEP 3: Swap object lists (deactivate A, activate B)
        if (listARoot != null)
        {
            foreach (Transform child in listARoot.transform)
                child.gameObject.SetActive(false);
        }
        if (listBRoot != null)
        {
            foreach (Transform child in listBRoot.transform)
                child.gameObject.SetActive(true);
        }

        // STEP 4: Fade OUT of white (alpha: 1 → 0)
        t = 0f;
        Color fullWhite        = planeMaterial.color; // alpha = 1
        Color fullyTransparent = fullWhite;
        fullyTransparent.a     = 0f;

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float lerpValue = Mathf.Clamp01(t / fadeOutDuration);
            planeMaterial.color = Color.Lerp(fullWhite, fullyTransparent, lerpValue);
            yield return null;
        }
        planeMaterial.color = fullyTransparent; // ensure fully transparent

        // STEP 5: Detach & deactivate the white plane
        whitePlane.transform.SetParent(null, worldPositionStays: false);
        whitePlane.SetActive(false);

        // ─── Reset hasFired so we can run this transition again ───
        yield return null;           // wait one frame (optional, but can help prevent accidental retriggers)
        hasFired = false;
    }
}

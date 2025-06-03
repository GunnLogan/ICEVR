using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.Events;

[RequireComponent(typeof(XRBaseInteractable))]
[RequireComponent(typeof(XRPokeFilter))]
public class ButtonFollowVisualTest : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onButtonPressed;
    public UnityEvent onButtonReset;

    [Header("Visual Settings")]
    [Tooltip("Renderer whose material has an _EmissionColor.")]
    public Renderer targetRenderer;
    public Color emissiveOffColor        = new Color(1f, 0f, 0f, 1f) * 0.2f;   // faint red
    public Color emissiveHoverColor      = Color.green * 0.5f;               // mid-green on hover
    public Color emissiveHoverExitColor  = new Color(1f, 0f, 0f, 1f) * 0.4f;   // brighter red on hover exit
    public Color emissiveOnColor         = Color.green;                      // full green on press

    [Header("Movement Settings")]
    public Transform visualTarget;
    public Vector3   localAxis        = Vector3.down;
    public float     maxTravel        = 0.02f;
    public float     resetSpeed       = 5f;

    // internal state
    Vector3                  _initialLocalPos;
    bool                     _isHovering, _isPressed;
    Transform                _pokeTransform;
    Vector3                  _pokeOffset;

    void Start()
    {
        _initialLocalPos = visualTarget.localPosition;

        // ensure emission is enabled up front
        if (targetRenderer != null)
        {
            targetRenderer.material.EnableKeyword("_EMISSION");
            SetEmission(emissiveOffColor);
        }

        var interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited  .AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited  .AddListener(OnSelectExit);
    }

    void Update()
    {
        if (_isHovering && _pokeTransform != null)
        {
            // follow finger along localAxis, clamped to maxTravel
            Vector3 localPos    = visualTarget.InverseTransformPoint(_pokeTransform.position + _pokeOffset);
            Vector3 projected  = Vector3.Project(localPos, localAxis.normalized);
            projected            = Vector3.ClampMagnitude(projected, maxTravel);
            visualTarget.position = visualTarget.TransformPoint(projected);
        }
        else if (!_isPressed)
        {
            // smoothly reset when not pressed
            visualTarget.localPosition = Vector3.Lerp(
                visualTarget.localPosition,
                _initialLocalPos,
                Time.deltaTime * resetSpeed
            );
        }
    }

    // Called the moment the poke collider enters your button collider
    void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (args.interactorObject is XRPokeInteractor poke)
        {
            _isHovering    = true;
            _pokeTransform = poke.transform;
            _pokeOffset    = visualTarget.position - poke.transform.position;
            SetEmission(emissiveHoverColor);
        }
    }

    // Called when the poke collider leaves your button collider
    void OnHoverExit(HoverExitEventArgs args)
    {
        _isHovering    = false;
        _pokeTransform = null;
        SetEmission(emissiveHoverExitColor);
    }

    // Called once you cross the pokeDepth threshold
    void OnSelectEnter(SelectEnterEventArgs args)
    {
        _isPressed = true;
        onButtonPressed?.Invoke();
        SetEmission(emissiveOnColor);
    }

    // Called when you pull your finger back past pokeDepth
    void OnSelectExit(SelectExitEventArgs args)
    {
        _isPressed = false;
        onButtonReset?.Invoke();
        SetEmission(emissiveOffColor);
        // snap instantly back home
        visualTarget.localPosition = _initialLocalPos;
    }

    void SetEmission(Color c)
    {
        if (targetRenderer != null)
            targetRenderer.material.SetColor("_EmissionColor", c);
    }
}

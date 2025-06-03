using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.Events;

public class ButtonFollowVisual : MonoBehaviour
{
    [Header("Press Settings")]
    public float pressThreshold = 0.01f;
    public UnityEvent onButtonPressed;
    
    [Header("Visual Settings")]
    [Tooltip("Renderer whose material has an _EmissionColor.")]
    public Renderer targetRenderer;
    public Color emissiveOnColor = Color.green;
    public Color emissiveOffColor = new Color(1f, 0f, 0f, 1f) * 0.2f; // faint red

    private bool _hasFired = false;
    
    [Header("Movement Settings")]
    public Transform visualTarget;
    public Vector3 localAxis;
    public float resetSpeed = 5;
    public float followAngleThreshold = 45;
    
    private bool _freezeButton = false;
    private Vector3 _initialLocalPosition;
    private Vector3 _offset;
    private Transform _pokeAttachTransform;
    private XRBaseInteractable _interactable;
    private bool _isFollowing = false;

    void Start()
    {
        if (targetRenderer != null)
        {
            // Make sure the material instance has emission enabled
            targetRenderer.material.EnableKeyword("_EMISSION");
            // Initialize to "off" color
            targetRenderer.material.SetColor("_EmissionColor", emissiveOffColor);
        }

        _initialLocalPosition = visualTarget.localPosition;
        _interactable = GetComponent<XRBaseInteractable>();
        _interactable.hoverEntered.AddListener(FollowButton);
        _interactable.hoverExited.AddListener(ResetButton);
        _interactable.selectEntered.AddListener(FreezeButton);
        _interactable.selectExited.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed(BaseInteractionEventArgs args)
    {
        onButtonPressed?.Invoke();
        // also update emission when the button fires
        SetEmissionColor(true);
    }

    void Update()
    {
        if (_freezeButton)
            return;
        
        if (_isFollowing)
        {
            Vector3 localTargetPosition = visualTarget.InverseTransformPoint(_pokeAttachTransform.position + _offset);
            Vector3 constrainedLocalTargetPosition = Vector3.Project(localTargetPosition, localAxis);
            visualTarget.position = visualTarget.TransformPoint(constrainedLocalTargetPosition);
        }
        else
        {
            visualTarget.localPosition = Vector3.Lerp(
                visualTarget.localPosition, 
                _initialLocalPosition, 
                Time.deltaTime * resetSpeed
            );
        }
        
        // Compute how far the button has traveled along the axis
        float travel = Vector3.Dot(
            (visualTarget.localPosition - _initialLocalPosition), 
            localAxis.normalized
        );

        if (!_hasFired && travel <= -pressThreshold)
        {
            _hasFired = true;
            onButtonPressed?.Invoke();
            SetEmissionColor(true);
        }
        else if (_hasFired && travel > -pressThreshold)
        {
            // Reset so it can fire again when pressed next
            _hasFired = false;
            SetEmissionColor(false);
        }
    }

    private void SetEmissionColor(bool isOn)
    {
        if (targetRenderer == null) return;
        Color c = isOn ? emissiveOnColor : emissiveOffColor;
        targetRenderer.material.SetColor("_EmissionColor", c);
    }

    public void FollowButton(BaseInteractionEventArgs hover)
    {
        if (hover.interactorObject is XRPokeInteractor poke)
        {
            _pokeAttachTransform = poke.transform;
            _offset = visualTarget.position - _pokeAttachTransform.position;
            
            float pokeAngle = Vector3.Angle(
                _offset, 
                visualTarget.TransformDirection(localAxis)
            );

            if (pokeAngle < followAngleThreshold)
            {
                _isFollowing = true;
                _freezeButton = false; 
            }
        }
    }

    public void ResetButton(BaseInteractionEventArgs hover)
    {
        if (hover.interactorObject is XRPokeInteractor)
        {
            _isFollowing = false;
            _freezeButton = false;
        }
    }

    public void FreezeButton(BaseInteractionEventArgs hover)
    {
        _freezeButton = true;
        /* 
        if (hover.interactorObject is XRPokeInteractor)
        {
            _freezeButton = true;
        }
        */
    }
    
    // new: zero-arg overload — Unity will show this in the Event dropdown
    public void ResetButton()
    {
        
        DoReset();
    }

    // centralize your reset logic here
    private void DoReset()
    {
        _isFollowing = false;
        _freezeButton = false;
        visualTarget.localPosition = _initialLocalPosition;
        _hasFired = false;
        SetEmissionColor(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Hands;
using UnityEngine.SubsystemsImplementation;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GazePointTeleporter : MonoBehaviour
{
    private static readonly int s_IDTeleportStart = Shader.PropertyToID("_TeleportStart");
    private static readonly int s_IDTeleportEnd = Shader.PropertyToID("_TeleportEnd");
    private static readonly int s_IDStepCount = Shader.PropertyToID("_StepCount");
    private static readonly int s_IDAlpha = Shader.PropertyToID("_Alpha");

    [Header("Rig References (auto-assigned)")]
    public TeleportationProvider teleportProvider;
    public Camera gazeCamera;

    [Header("Teleport Settings")]
    public LayerMask teleportLayerMask;
    public float dwellTime = 2f;

    [Header("Fade Plane")]
    public GameObject fadePlane;
    public float planeDistance = 0.5f;
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 1.5f;

    [Header("Highlight Fade")]
    public float highlightFadeDuration = 0.5f;

    [Header("Footstep Settings")]
    public float stepLength = 1f;

    [Header("Hand Tracking")]
    public XRHandSubsystem _handSubsystem;
    public XRNode leftHandNode = XRNode.LeftHand;
    public XRNode rightHandNode = XRNode.RightHand;

    private float _gazeTimer;
    private Vector3 _gazeStartPos;
    private Vector3 _gazeTargetPos;
    private Transform _currentTarget;
    private RaycastHit _hitInfo;
    private Material _planeMaterial;
    private bool _isFading;
    private MeshRenderer _lastRenderer;
    private Coroutine _highlightCoroutine;
    private MaterialPropertyBlock _mpb;
    private GameObject _lastDisabledTeleportable;

#if UNITY_EDITOR
    void Reset()
    {
        gazeCamera = Camera.main;
        if (gazeCamera != null)
            teleportProvider = gazeCamera.GetComponentInParent<TeleportationProvider>();
        fadePlane = GameObject.Find("FadePlane");
        EditorUtility.SetDirty(this);
    }

    void OnValidate()
    {
        if (gazeCamera == null)
            gazeCamera = Camera.main;
        if (teleportProvider == null && gazeCamera != null)
            teleportProvider = gazeCamera.GetComponentInParent<TeleportationProvider>();
    }
#endif

    void Awake()
    {
        gazeCamera ??= Camera.main ?? Object.FindFirstObjectByType<Camera>();
        teleportProvider ??= gazeCamera.GetComponentInParent<TeleportationProvider>()
                             ?? Object.FindFirstObjectByType<TeleportationProvider>();

        if (teleportProvider == null)
        {
            var root = gazeCamera.transform.root.gameObject;
            teleportProvider = root.AddComponent<TeleportationProvider>();
            Debug.Log("GazeTeleporter: Added TeleportationProvider at runtime.");
        }

        if (fadePlane != null)
        {
            var rend = fadePlane.GetComponent<Renderer>();
            if (rend != null)
            {
                _planeMaterial = Instantiate(rend.sharedMaterial);
                rend.material = _planeMaterial;
                var c = _planeMaterial.color;
                c.a = 0f;
                _planeMaterial.color = c;
                fadePlane.SetActive(false);
            }
            else Debug.LogError("GazeTeleporter: fadePlane missing Renderer.");
        }
        else Debug.LogError("GazeTeleporter: fadePlane not assigned.");

        _mpb = new MaterialPropertyBlock();

        if (_handSubsystem == null)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
                _handSubsystem = subsystems[0];
        }
    }

    void Update()
    {
        if (_isFading || gazeCamera == null || teleportProvider == null)
            return;

        HandleGazeTeleport();
    }

    private void HandleGazeTeleport()
    {
        var origin = gazeCamera.transform.position;
        var dir = gazeCamera.transform.forward;

        if (Physics.Raycast(origin, dir, out _hitInfo, 100f, teleportLayerMask, QueryTriggerInteraction.Collide))
        {
            var mr = _hitInfo.collider.GetComponent<MeshRenderer>();
            if (mr != null && _lastRenderer != mr)
            {
                if (_lastRenderer != null) StartHighlight(_lastRenderer, false);
                _lastRenderer = mr;
                mr.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_UseGlobalTime", 0f);
                _mpb.SetFloat("_Alpha", 0f);
                mr.SetPropertyBlock(_mpb);
                StartHighlight(mr, true);
            }

            if (_currentTarget != _hitInfo.transform)
            {
                _currentTarget = _hitInfo.transform;
                _gazeTimer = 0f;
                _gazeStartPos = origin;
                _gazeTargetPos = _hitInfo.transform.Find("TeleportPoint")?.position ?? _hitInfo.point;
                Shader.SetGlobalVector(s_IDTeleportStart, _gazeStartPos);
                Shader.SetGlobalVector(s_IDTeleportEnd, _gazeTargetPos);
            }
            else
            {
                _gazeTimer += Time.deltaTime;
            }

            float gazePct = Mathf.Clamp01(_gazeTimer / dwellTime);
            float totalDist = Vector3.Distance(_gazeStartPos, _gazeTargetPos);
            int stepCount = Mathf.FloorToInt((totalDist * gazePct) / stepLength);
            Shader.SetGlobalFloat(s_IDStepCount, stepCount);

            if (_gazeTimer >= dwellTime)
            {
                bool isLeftPointing = IsHandPointing(leftHandNode);
                bool isRightPointing = IsHandPointing(rightHandNode);

                if (isLeftPointing || isRightPointing)
                {
                    StartCoroutine(FadeAndTeleport(_gazeTargetPos, _hitInfo.collider.gameObject));
                }
            }
        }
        else
        {
            if (_lastRenderer != null) StartHighlight(_lastRenderer, false);
            _lastRenderer = null;
            _currentTarget = null;
            _gazeTimer = 0f;
            Shader.SetGlobalFloat(s_IDStepCount, 0f);
        }
    }

    private bool IsHandPointing(XRNode handNode)
{
    if (_handSubsystem == null || !_handSubsystem.running)
        return false;

    XRHand hand = handNode == XRNode.LeftHand ? _handSubsystem.leftHand : _handSubsystem.rightHand;
    if (!hand.isTracked)
        return false;

    var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
    if (!indexTip.TryGetPose(out Pose tipPose))
        return false;

    Vector3 rayOrigin = tipPose.position;
    Vector3 rayDirection = tipPose.rotation * Vector3.forward;

    Debug.DrawRay(rayOrigin, rayDirection * 10f, Color.cyan);

    if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, 10f, teleportLayerMask))
    {
        return _currentTarget != null && hit.transform == _currentTarget;
    }

    return false;
}

    private IEnumerator FadeAndTeleport(Vector3 destination, GameObject targetObj)
    {
        _isFading = true;
        Vector3 startPos = gazeCamera.transform.position;
        float totalDistance = Vector3.Distance(startPos, destination);
        Shader.SetGlobalVector(s_IDTeleportStart, startPos);
        Shader.SetGlobalVector(s_IDTeleportEnd, destination);

        fadePlane.SetActive(true);
        fadePlane.transform.SetParent(gazeCamera.transform, false);
        fadePlane.transform.localPosition = Vector3.forward * planeDistance;
        fadePlane.transform.localRotation = Quaternion.identity;
        fadePlane.transform.localScale = Vector3.one;

        Color baseCol = _planeMaterial.color;
        Color whiteCol = Color.white;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);
            _planeMaterial.color = Color.Lerp(baseCol, whiteCol, p);
            int step = Mathf.FloorToInt((totalDistance * p) / stepLength);
            Shader.SetGlobalFloat(s_IDStepCount, step);
            yield return null;
        }
        Shader.SetGlobalFloat(s_IDStepCount, Mathf.FloorToInt(totalDistance / stepLength));
        _planeMaterial.color = whiteCol;

        teleportProvider.QueueTeleportRequest(new TeleportRequest { destinationPosition = destination, matchOrientation = MatchOrientation.TargetUp });
        if (_lastDisabledTeleportable != null) _lastDisabledTeleportable.SetActive(true);
        targetObj.SetActive(false);
        _lastDisabledTeleportable = targetObj;

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float p = 1f - Mathf.Clamp01(t / fadeOutDuration);
            _planeMaterial.color = Color.Lerp(baseCol, whiteCol, p);
            yield return null;
        }

        _planeMaterial.color = baseCol;
        fadePlane.SetActive(false);
        Shader.SetGlobalFloat(s_IDStepCount, 0f);
        _gazeTimer = 0f;
        _isFading = false;
    }

    private void StartHighlight(MeshRenderer mr, bool fadeIn)
    {
        if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
        _highlightCoroutine = StartCoroutine(FadeShaderAlpha(mr, fadeIn));
    }

    private IEnumerator FadeShaderAlpha(MeshRenderer mr, bool fadeIn)
    {
        float startA = fadeIn ? 0f : 1f;
        float endA = fadeIn ? 1f : 0f;
        float t = 0f;
        while (t < highlightFadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, endA, t / highlightFadeDuration);
            _mpb.SetFloat(s_IDAlpha, a);
            mr.SetPropertyBlock(_mpb);
            yield return null;
        }
        _mpb.SetFloat(s_IDAlpha, endA);
        mr.SetPropertyBlock(_mpb);
    }
}

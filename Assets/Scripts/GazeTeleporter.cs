using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gaze-to-teleport behaviour with:
///  • auto-fade on a camera-attached quad
///  • disables the last teleported target when you teleport, re-enables the previous one
///  • footsteps begin as soon as gaze locks, then continue through the fade
/// </summary>
public class GazeTeleporter : MonoBehaviour
{
    // Shader global property IDs
    private static readonly int ID_TeleportStart = Shader.PropertyToID("_TeleportStart");
    private static readonly int ID_TeleportEnd   = Shader.PropertyToID("_TeleportEnd");
    private static readonly int ID_StepCount     = Shader.PropertyToID("_StepCount");
    private static readonly int ID_UseGlobalTime = Shader.PropertyToID("_UseGlobalTime");
    private static readonly int ID_Alpha         = Shader.PropertyToID("_Alpha");

    [Header("Rig References (auto-assigned)")]
    public TeleportationProvider teleportProvider;
    public Camera gazeCamera;

    [Header("Teleport Settings")]
    public LayerMask teleportLayerMask;
    public float dwellTime = 2f;

    [Header("Fade Plane (Quad with white Unlit/Transparent material)")]
    public GameObject fadePlane;
    public float planeDistance   = 0.5f;
    public float fadeInDuration  = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Highlight Fade for Teleportable Objects")]
    public float highlightFadeDuration = 0.5f;

    [Header("Footstep Settings")]
    [Tooltip("Spacing between footprints (world units). Must match shader’s _StepLength")]
    public float stepLength = 1f;

    // Internal state
    private float   _gazeTimer;
    private Vector3 _gazeStartPos;
    private Vector3 _gazeTargetPos;
    private Transform _currentTarget;
    private RaycastHit _hitInfo;
    private Material   _planeMaterial;
    private bool       _isFading;
    private MeshRenderer _lastRenderer;
    private Coroutine    _highlightCoroutine;
    private MaterialPropertyBlock _mpb;
    private GameObject   _lastDisabledTeleportable;

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
        // Auto-assign camera & provider
        gazeCamera ??= Camera.main ?? Object.FindFirstObjectByType<Camera>();
        teleportProvider ??= gazeCamera.GetComponentInParent<TeleportationProvider>()
                             ?? Object.FindFirstObjectByType<TeleportationProvider>();
        if (teleportProvider == null)
        {
            var root = gazeCamera.transform.root.gameObject;
            teleportProvider = root.AddComponent<TeleportationProvider>();
            Debug.Log("GazeTeleporter: Added TeleportationProvider at runtime.");
        }

        // Prepare fadePlane
        if (fadePlane != null)
        {
            var rend = fadePlane.GetComponent<Renderer>();
            if (rend != null)
            {
                _planeMaterial = Instantiate(rend.sharedMaterial);
                rend.material  = _planeMaterial;
                var c = _planeMaterial.color;
                c.a = 0f;
                _planeMaterial.color = c;
                fadePlane.SetActive(false);
            }
            else Debug.LogError("GazeTeleporter: fadePlane missing Renderer.");
        }
        else Debug.LogError("GazeTeleporter: fadePlane not assigned.");

        _mpb = new MaterialPropertyBlock();
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
        var dir    = gazeCamera.transform.forward;

        if (Physics.Raycast(origin, dir, out _hitInfo, 100f, teleportLayerMask, QueryTriggerInteraction.Collide))
        {
            // Highlight as before
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

            // On new target, capture start/end
            if (_currentTarget != _hitInfo.transform)
            {
                _currentTarget  = _hitInfo.transform;
                _gazeTimer      = 0f;
                _gazeStartPos   = origin;
                _gazeTargetPos  = _hitInfo.point;
                Shader.SetGlobalVector(ID_TeleportStart, new Vector4(_gazeStartPos.x, _gazeStartPos.y, _gazeStartPos.z, 0f));
                Shader.SetGlobalVector(ID_TeleportEnd,   new Vector4(_gazeTargetPos.x, _gazeTargetPos.y, _gazeTargetPos.z, 0f));
            }
            else
            {
                _gazeTimer += Time.deltaTime;
            }

            // Pre-fade footsteps
            float gazePct   = Mathf.Clamp01(_gazeTimer / dwellTime);
            float totalDist = Vector3.Distance(_gazeStartPos, _gazeTargetPos);
            int   stepCount = Mathf.FloorToInt((totalDist * gazePct) / stepLength);
            Shader.SetGlobalFloat(ID_StepCount, stepCount);
            Debug.Log($"[GazeTeleporter] stepCount={stepCount}, gazePct={gazePct:F2}");

            // Kick off fade+teleport
            if (_gazeTimer >= dwellTime)
                StartCoroutine(FadeAndTeleport(_gazeTargetPos, _hitInfo.collider.gameObject));
        }
        else
        {
            if (_lastRenderer != null) StartHighlight(_lastRenderer, false);
            _lastRenderer = null;
            _currentTarget = null;
            _gazeTimer = 0f;
            Shader.SetGlobalFloat(ID_StepCount, 0f);
        }
    }

    private IEnumerator FadeAndTeleport(Vector3 destination, GameObject targetObj)
    {
        Debug.Log(">> FadeAndTeleport START");
        _isFading = true;

        Vector3 startPos = gazeCamera.transform.position;
        float totalDistance = Vector3.Distance(startPos, destination);
        Shader.SetGlobalVector(ID_TeleportStart, new Vector4(startPos.x, startPos.y, startPos.z, 0f));
        Shader.SetGlobalVector(ID_TeleportEnd,   new Vector4(destination.x, destination.y, destination.z, 0f));

        // Fade in
        fadePlane.SetActive(true);
        fadePlane.transform.SetParent(gazeCamera.transform, false);
        fadePlane.transform.localPosition = Vector3.forward * planeDistance;
        fadePlane.transform.localRotation = Quaternion.identity;
        fadePlane.transform.localScale    = Vector3.one;

        Color baseCol  = _planeMaterial.color;
        Color whiteCol = Color.white;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);
            _planeMaterial.color = Color.Lerp(baseCol, whiteCol, p);
            int step = Mathf.FloorToInt((totalDistance * p) / stepLength);
            Shader.SetGlobalFloat(ID_StepCount, step);
            yield return null;
        }
        Shader.SetGlobalFloat(ID_StepCount, Mathf.FloorToInt(totalDistance / stepLength));
        _planeMaterial.color = whiteCol;

        // Teleport
        teleportProvider.QueueTeleportRequest(new TeleportRequest { destinationPosition = destination, matchOrientation = MatchOrientation.TargetUp });
        if (_lastDisabledTeleportable != null) _lastDisabledTeleportable.SetActive(true);
        targetObj.SetActive(false);
        _lastDisabledTeleportable = targetObj;

        // Fade out
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
        Shader.SetGlobalFloat(ID_StepCount, 0f);
        _gazeTimer = 0f;
        _isFading = false;
        Debug.Log(">> FadeAndTeleport END");
    }

    private void StartHighlight(MeshRenderer mr, bool fadeIn)
    {
        if (_highlightCoroutine != null) StopCoroutine(_highlightCoroutine);
        _highlightCoroutine = StartCoroutine(FadeShaderAlpha(mr, fadeIn));
    }

    private IEnumerator FadeShaderAlpha(MeshRenderer mr, bool fadeIn)
    {
        float startA = fadeIn ? 0f : 1f;
        float endA   = fadeIn ? 1f : 0f;
        float t      = 0f;
        while (t < highlightFadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, endA, t / highlightFadeDuration);
            _mpb.SetFloat(ID_Alpha, a);
            mr.SetPropertyBlock(_mpb);
            yield return null;
        }
        _mpb.SetFloat(ID_Alpha, endA);
        mr.SetPropertyBlock(_mpb);
    }
}
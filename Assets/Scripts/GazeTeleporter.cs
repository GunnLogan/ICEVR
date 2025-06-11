using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gaze-to-teleport behaviour that auto-finds or adds TeleportationProvider and Camera on Awake,
/// shows a fade plane in front of the camera to fade to white, teleport, then fade out,
/// and drives a pulsing-alpha shader on teleportable objects to fade them in/out on gaze.
/// </summary>
public class GazeTeleporter : MonoBehaviour
{
    [Header("Rig References (auto-assigned)")]
    public TeleportationProvider teleportProvider;
    public Camera gazeCamera;

    [Header("Teleport Settings")]
    public LayerMask teleportLayerMask;
    public float dwellTime = 2f;

    [Header("Fade Plane (Quad with white Unlit/Transparent material)")]
    public GameObject fadePlane;
    public float planeDistance = 0.5f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Highlight Fade for Teleportable Objects")]
    public float highlightFadeDuration = 0.5f;

    // internal state
    private float _gazeTimer;
    private Transform _currentTarget;
    private RaycastHit _hitInfo;
    private Material _planeMaterial;
    private bool _isFading;
    private MeshRenderer _lastRenderer;
    private Coroutine _highlightCoroutine;
    private MaterialPropertyBlock _mpb;

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
        // auto-assign camera
        gazeCamera ??= Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (gazeCamera == null)
            Debug.LogError("GazeTeleporter: No Camera found.");

        // auto-assign or add TeleportationProvider
        teleportProvider ??= gazeCamera.GetComponentInParent<TeleportationProvider>()
                             ?? Object.FindFirstObjectByType<TeleportationProvider>();
        if (teleportProvider == null)
        {
            var root = gazeCamera.transform.root.gameObject;
            teleportProvider = root.AddComponent<TeleportationProvider>();
            Debug.Log("GazeTeleporter: Added TeleportationProvider at runtime.");
        }

        // prepare fadePlane
        if (fadePlane != null)
        {
            var rend = fadePlane.GetComponent<Renderer>();
            if (rend != null)
            {
                _planeMaterial = Instantiate(rend.sharedMaterial);
                rend.material = _planeMaterial;
                var c = _planeMaterial.color; c.a = 0f;
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

        var origin = gazeCamera.transform.position;
        var dir = gazeCamera.transform.forward;
        Debug.DrawRay(origin, dir * 100f,
            Physics.Raycast(origin, dir, out _hitInfo, 100f, teleportLayerMask) ? Color.green : Color.red);

        if (Physics.Raycast(origin, dir, out _hitInfo, 100f, teleportLayerMask))
        {
            var mr = _hitInfo.collider.GetComponent<MeshRenderer>();
            if (mr != null && _lastRenderer != mr)
            {
                if (_lastRenderer != null)
                    StartHighlight(_lastRenderer, false);
                _lastRenderer = mr;

                // initialize shader block for new target
                mr.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_UseGlobalTime", 0f);
                _mpb.SetFloat("_Alpha", 0f);
                mr.SetPropertyBlock(_mpb);

                StartHighlight(mr, true);
            }

            // gaze dwell
            if (_currentTarget == _hitInfo.transform)
                _gazeTimer += Time.deltaTime;
            else
            {
                _currentTarget = _hitInfo.transform;
                _gazeTimer = Time.deltaTime;
            }
            if (_gazeTimer >= dwellTime)
                StartCoroutine(FadeAndTeleport(_hitInfo.point));
        }
        else
        {
            if (_lastRenderer != null)
                StartHighlight(_lastRenderer, false);
            _lastRenderer = null;
            _currentTarget = null;
            _gazeTimer = 0f;
        }
    }

    private void StartHighlight(MeshRenderer mr, bool fadeIn)
    {
        if (_highlightCoroutine != null)
            StopCoroutine(_highlightCoroutine);
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
            _mpb.SetFloat("_Alpha", a);
            mr.SetPropertyBlock(_mpb);
            yield return null;
        }
        _mpb.SetFloat("_Alpha", endA);
        mr.SetPropertyBlock(_mpb);
    }

    private IEnumerator FadeAndTeleport(Vector3 destination)
    {
        _isFading = true;
        fadePlane.SetActive(true);
        fadePlane.transform.SetParent(gazeCamera.transform, false);
        fadePlane.transform.localPosition = new Vector3(0, 0, planeDistance);
        fadePlane.transform.localRotation = Quaternion.identity;
        fadePlane.transform.localScale = Vector3.one;

        float t = 0f;
        var start = _planeMaterial.color;
        var white = start; white.a = 1f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            _planeMaterial.color = Color.Lerp(start, white, t / fadeInDuration);
            yield return null;
        }
        _planeMaterial.color = white;

        teleportProvider.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = destination,
            matchOrientation    = MatchOrientation.TargetUp
        });

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            _planeMaterial.color = Color.Lerp(white, start, t / fadeOutDuration);
            yield return null;
        }
        _planeMaterial.color = start;
        fadePlane.SetActive(false);

        _gazeTimer = 0f;
        _isFading = false;
    }
}

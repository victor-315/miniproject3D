using System.Collections;
using UnityEngine;

public class HitscanWeapon : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform muzzlePoint;

    [Header("Fire Settings")]
    public float fireRate = 600f;
    public bool fullAuto = true;
    public float maxRange = 200f;
    public LayerMask hitMask = ~0;

    [Header("Tracer Settings")]
    public bool showTracers = true;
    public float tracerSpeed = 800f;
    public float tracerFadeTime = 0.06f;
    public float tracerWidth = 0.02f;
    public Color tracerColor = new Color(1f, 0.85f, 0.3f, 1f);
    public Material tracerMaterial;

    public Vector3 tracerFallbackOffset = new Vector3(0.3f, -0.2f, 0.8f);

    private float _nextFireTime;
    private float _fireInterval;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        _fireInterval = 60f / Mathf.Max(fireRate, 1f);
    }

    void Update()
    {
        HandleFiring();
    }

    void HandleFiring()
    {
        bool trigger = fullAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (!trigger || Time.time < _nextFireTime) return;

        Fire();
    }

    void Fire()
    {
        _nextFireTime = Time.time + _fireInterval;

        Transform origin = muzzlePoint != null ? muzzlePoint : playerCamera.transform;

        // Ray from camera center
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = ray.origin + ray.direction * maxRange;
        }

        // Tracer start
        Vector3 tracerStart;
        if (muzzlePoint != null)
        {
            tracerStart = muzzlePoint.position;
        }
        else
        {
            Transform cam = playerCamera.transform;
            tracerStart = cam.position
                        + cam.right   * tracerFallbackOffset.x
                        + cam.up      * -tracerFallbackOffset.y
                        + cam.forward * tracerFallbackOffset.z;
        }

        if (showTracers)
            StartCoroutine(AnimateTracer(tracerStart, endPoint));
    }

    IEnumerator AnimateTracer(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("Tracer");
        LineRenderer lr = go.AddComponent<LineRenderer>();

        lr.material = tracerMaterial != null ? tracerMaterial : CreateTracerMaterial();

        lr.startWidth = tracerWidth;
        lr.endWidth = tracerWidth * 0.3f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        float distance = Vector3.Distance(start, end);
        float travelled = 0f;

        while (travelled < distance)
        {
            travelled += tracerSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(travelled / distance);
            Vector3 tip = Vector3.Lerp(start, end, t);

            lr.SetPosition(0, start);
            lr.SetPosition(1, tip);
            yield return null;
        }

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        float elapsed = 0f;
        while (elapsed < tracerFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / tracerFadeTime;

            Color col = new Color(tracerColor.r, tracerColor.g, tracerColor.b, Mathf.Lerp(1f, 0f, t));
            lr.startColor = col;
            lr.endColor = new Color(col.r, col.g, col.b, 0f);

            yield return null;
        }

        Destroy(go);
    }

    Material CreateTracerMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = tracerColor;
        return mat;
    }
}
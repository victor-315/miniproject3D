


using System.Collections;
using UnityEngine;
 

public class HitscanWeapon : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector Settings
    // -------------------------------------------------------------------------
 
    [Header("References")]
    [Tooltip("The player's camera. Auto-assigned to Camera.main if left empty.")]
    public Camera playerCamera;
 
    [Tooltip("Optional muzzle transform for tracer + VFX origin.")]
    public Transform muzzlePoint;
 
    [Header("Fire Settings")]
    public float fireRate  = 600f;
    public bool  fullAuto  = true;
    public float maxRange  = 200f;
    public LayerMask hitMask = ~0;
 
    [Header("Damage")]
    public float baseDamage          = 25f;
    public float headshotMultiplier  = 2f;
    public float falloffStartDistance = 50f;
    [Range(0f, 1f)]
    public float minDamageFraction   = 0.25f;
 
    [Header("Ammo")]
    public int   magazineSize = 30;
    public int   reserveAmmo  = 90;
    public float reloadTime   = 2f;
 
    [Header("Spread")]
    public float baseSpread     = 0.5f;
    public float movementSpread = 1.5f;
 
    [Header("Tracers")]
    [Tooltip("Show bullet tracers (no prefab needed).")]
    public bool showTracers = true;
 
    [Tooltip("How fast the tracer travels in metres per second. Set very high for instant.")]
    public float tracerSpeed = 800f;
 
    [Tooltip("How long the tracer line lingers after reaching its target (seconds).")]
    public float tracerFadeTime = 0.06f;
 
    [Tooltip("Width of the tracer line in world units.")]
    public float tracerWidth = 0.02f;
 
    [Tooltip("Tracer colour — set alpha < 1 for translucency.")]
    public Color tracerColor = new Color(1f, 0.85f, 0.3f, 1f); // warm yellow
 
    [Tooltip("Optional: assign an Unlit/Transparent shader material for glow effect. " +
             "Leave empty to use a plain color.")]
    public Material tracerMaterial;
 
    [Tooltip("Fallback tracer origin offset from the camera when no Muzzle Point is assigned. " +
             "X = right, Y = down, Z = forward. Tweak to match where your gun barrel sits.\n" +
             "Typical FPS values: (0.3, -0.2, 0.8)")]
    public Vector3 tracerFallbackOffset = new Vector3(0.3f, -0.2f, 0.8f);
 
    [Header("VFX (optional prefabs)")]
    public GameObject muzzleFlashPrefab;
    public float      muzzleFlashDuration  = 0.05f;
    public GameObject impactEffectPrefab;
    public float      impactEffectDuration = 1f;
    public GameObject impactDecalPrefab;
    public float      impactDecalDuration  = 10f;
 
    // -------------------------------------------------------------------------
    // Public State
    // -------------------------------------------------------------------------
 
    public int  CurrentAmmo { get; private set; }
    public int  ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }
 
    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------
 
    private float _nextFireTime;
    private float _fireInterval;
    private CharacterController _charController;
 
    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------
 
    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
 
        if (playerCamera == null)
            Debug.LogError("[HitscanWeapon] No camera found! Assign 'playerCamera' in the Inspector.", this);
 
        CurrentAmmo   = magazineSize;
        ReserveAmmo   = reserveAmmo;
        _fireInterval = 60f / Mathf.Max(fireRate, 1f);
        _charController = GetComponentInParent<CharacterController>();
    }
 
    private void Update()
    {
        if (playerCamera == null) return;
        HandleFiring();
        HandleReload();
    }
 
    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------
 
    private void HandleFiring()
    {
        if (IsReloading) return;
        bool trigger = fullAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (!trigger || Time.time < _nextFireTime) return;
 
        if (CurrentAmmo > 0)
            Fire();
        else
            StartCoroutine(Reload());
    }
 
    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R) && !IsReloading)
            StartCoroutine(Reload());
    }
 
    // -------------------------------------------------------------------------
    // Fire
    // -------------------------------------------------------------------------
 
    private void Fire()
    {
        CurrentAmmo--;
        _nextFireTime = Time.time + _fireInterval;
 
        // Determine muzzle origin
        Transform origin  = muzzlePoint != null ? muzzlePoint : playerCamera.transform;
        Vector3 muzzlePos = origin.position;
 
        // Spread
        float   spread    = CalculateSpread();
        Vector3 direction = ApplySpread(playerCamera.transform.forward, spread);
        Ray     ray       = new Ray(playerCamera.transform.position, direction);
 
        // Muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePos, origin.rotation, origin);
            Destroy(flash, muzzleFlashDuration);
        }
 
        // Raycast (ignore triggers)
        Vector3 endPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            ProcessHit(hit);
            SpawnImpactEffects(hit);
        }
        else
        {
            endPoint = ray.origin + ray.direction * maxRange;
        }
 
        // Tracer start: use muzzle point if assigned, otherwise offset from camera
        // in local space so it looks like it comes from the gun barrel, not the eye.
        Vector3 tracerStart;
        if (muzzlePoint != null)
        {
            tracerStart = muzzlePoint.position;
        }
        else
        {
            Transform cam = playerCamera.transform;
            tracerStart = cam.position
                        + cam.right   *  tracerFallbackOffset.x
                        + cam.up      * -tracerFallbackOffset.y   // positive Y = down
                        + cam.forward *  tracerFallbackOffset.z;
        }
 
        // Tracer
        if (showTracers)
            StartCoroutine(AnimateTracer(tracerStart, endPoint));
 
        Debug.Log($"[HitscanWeapon] Fired. Ammo: {CurrentAmmo}/{magazineSize}");
    }
 
    // -------------------------------------------------------------------------
    // Hit Processing
    // -------------------------------------------------------------------------
 
    private void ProcessHit(RaycastHit hit)
    {
        float damage  = CalculateDamage(hit);
        bool headshot = hit.collider.CompareTag("Head");
        if (headshot) damage *= headshotMultiplier;
 
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage, hit.point, hit.normal, headshot);
            Debug.Log($"[HitscanWeapon] Hit '{hit.collider.name}' for {damage:F1} dmg. Headshot: {headshot}");
        }
        else
        {
            Debug.Log($"[HitscanWeapon] Hit '{hit.collider.name}' — no IDamageable found.");
        }
    }
 
    // -------------------------------------------------------------------------
    // Tracer Coroutine
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// Finds the best unlit shader available in the current render pipeline.
    /// Tries URP, HDRP, then Built-in fallbacks so the tracer always renders.
    /// </summary>
    private Material CreateTracerMaterial()
    {
        // Ordered by pipeline preference
        string[] candidates = new string[]
        {
            "Universal Render Pipeline/Particles/Unlit",   // URP
            "Universal Render Pipeline/Unlit",             // URP simple
            "HDRP/Unlit",                                  // HDRP
            "Unlit/Color",                                 // Built-in
            "Sprites/Default",                             // Built-in fallback
            "Hidden/Internal-Colored",                     // last resort
        };
 
        foreach (string name in candidates)
        {
            Shader s = Shader.Find(name);
            if (s != null)
            {
                Material mat = new Material(s);
                // Make sure the color property exists and is set
                if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  tracerColor);
                if (mat.HasProperty("_Color"))      mat.SetColor("_Color",      tracerColor);
                if (mat.HasProperty("_UnlitColor")) mat.SetColor("_UnlitColor", tracerColor);
                Debug.Log($"[HitscanWeapon] Tracer using shader: {name}");
                return mat;
            }
        }
 
        // Absolute fallback – default material (will be pink/magenta but visible)
        Debug.LogWarning("[HitscanWeapon] Could not find an unlit shader. Tracer will use default material.");
        return new Material(Shader.Find("Standard"));
    }
 
    private IEnumerator AnimateTracer(Vector3 start, Vector3 end)
    {
        // Create a fresh LineRenderer each shot
        GameObject go = new GameObject("Tracer");
        LineRenderer lr = go.AddComponent<LineRenderer>();
 
        // Material
        lr.material = tracerMaterial != null ? tracerMaterial : CreateTracerMaterial();
 
        // Colour — must also be set via SetColors, not just startColor/endColor,
        // as some shaders ignore vertex colour without the right keyword enabled.
        lr.colorGradient = MakeTracerGradient(tracerColor);
        lr.startColor    = tracerColor;
        lr.endColor      = new Color(tracerColor.r, tracerColor.g, tracerColor.b, 0f);
        lr.startWidth    = tracerWidth;
        lr.endWidth      = tracerWidth * 0.3f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
 
        float distance  = Vector3.Distance(start, end);
        float travelled = 0f;
 
        // --- Travel phase: tip moves from muzzle to target ---
        while (travelled < distance)
        {
            travelled += tracerSpeed * Time.deltaTime;
            float    t   = Mathf.Clamp01(travelled / distance);
            Vector3  tip = Vector3.Lerp(start, end, t);
 
            lr.SetPosition(0, start);
            lr.SetPosition(1, tip);
            yield return null;
        }
 
        // Snap to full length
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
 
        // --- Fade phase: fade out alpha over tracerFadeTime ---
        float elapsed = 0f;
        while (elapsed < tracerFadeTime)
        {
            elapsed += Time.deltaTime;
            float t   = elapsed / tracerFadeTime;
            Color col = new Color(tracerColor.r, tracerColor.g, tracerColor.b, Mathf.Lerp(1f, 0f, t));
            lr.startColor    = col;
            lr.endColor      = new Color(col.r, col.g, col.b, 0f);
            lr.colorGradient = MakeTracerGradient(col);
            yield return null;
        }
 
        Destroy(go);
    }
 
    /// <summary>Builds a simple start→transparent gradient for the LineRenderer.</summary>
    private Gradient MakeTracerGradient(Color col)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(col, 0f),  new GradientColorKey(col, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(col.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        return g;
    }
 
    // -------------------------------------------------------------------------
    // Damage Falloff
    // -------------------------------------------------------------------------
 
    private float CalculateDamage(RaycastHit hit)
    {
        if (falloffStartDistance <= 0f || hit.distance <= falloffStartDistance)
            return baseDamage;
        float t = Mathf.InverseLerp(falloffStartDistance, maxRange, hit.distance);
        return Mathf.Lerp(baseDamage, baseDamage * minDamageFraction, t);
    }
 
    // -------------------------------------------------------------------------
    // Spread
    // -------------------------------------------------------------------------
 
    private float CalculateSpread()
    {
        float spread = baseSpread;
        if (_charController != null && _charController.velocity.magnitude > 0.1f)
            spread += movementSpread;
        return spread;
    }
 
    private Vector3 ApplySpread(Vector3 direction, float halfAngle)
    {
        if (halfAngle <= 0f) return direction;
        float angle = Random.Range(0f, halfAngle);
        float roll  = Random.Range(0f, 360f);
        Quaternion rot = Quaternion.AngleAxis(roll, direction)
                       * Quaternion.AngleAxis(angle, playerCamera.transform.right);
        return rot * direction;
    }
 
    // -------------------------------------------------------------------------
    // Reload
    // -------------------------------------------------------------------------
 
    private IEnumerator Reload()
    {
        if (IsReloading || ReserveAmmo <= 0 || CurrentAmmo == magazineSize) yield break;
        IsReloading = true;
        Debug.Log("[HitscanWeapon] Reloading...");
        yield return new WaitForSeconds(reloadTime);
        int drawn   = Mathf.Min(magazineSize - CurrentAmmo, ReserveAmmo);
        CurrentAmmo += drawn;
        ReserveAmmo -= drawn;
        IsReloading  = false;
        Debug.Log($"[HitscanWeapon] Reloaded. Ammo: {CurrentAmmo}/{magazineSize} | Reserve: {ReserveAmmo}");
    }
 
    // -------------------------------------------------------------------------
    // VFX
    // -------------------------------------------------------------------------
 
    private void SpawnImpactEffects(RaycastHit hit)
    {
        Quaternion rot = Quaternion.LookRotation(hit.normal);
        if (impactEffectPrefab != null)
        {
            GameObject fx = Instantiate(impactEffectPrefab, hit.point, rot);
            Destroy(fx, impactEffectDuration);
        }
        if (impactDecalPrefab != null)
        {
            GameObject decal = Instantiate(impactDecalPrefab, hit.point, rot, hit.transform);
            Destroy(decal, impactDecalDuration);
        }
    }
 
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------
 
    public void ForceReload()       => StartCoroutine(Reload());
    public void AddAmmo(int amount) => ReserveAmmo += amount;
}
 
// =============================================================================
// IDamageable Interface
// =============================================================================
 
/// <summary>
/// Implement on any MonoBehaviour that should receive weapon damage.
///
/// Example:
///     public class Enemy : MonoBehaviour, IDamageable
///     {
///         float hp = 100f;
///         public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, bool headshot)
///         {
///             hp -= damage;
///             if (hp <= 0) Destroy(gameObject);
///         }
///     }
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, bool headshot);
}
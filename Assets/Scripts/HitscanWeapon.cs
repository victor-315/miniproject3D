
using UnityEngine;

/// <summary>
/// HitscanWeapon - Attach to your weapon GameObject.
///
/// Features:
///   - Instant hitscan raycast from camera centre (or weapon muzzle)
///   - Semi-auto and full-auto fire modes
///   - Ammo + reload system
///   - Damage with optional falloff over distance
///   - Bullet spread / recoil
///   - Muzzle flash, impact VFX + decals
///   - Hit detection via IDamageable interface
///
/// Setup:
///   1. Assign 'playerCamera' (your main Camera).
///   2. (Optional) assign 'muzzlePoint' Transform for VFX origin.
///   3. (Optional) assign 'muzzleFlashPrefab', 'impactEffectPrefab', 'impactDecalPrefab'.
///   4. Implement IDamageable on any GameObject you want to receive damage.
/// </summary>
public class HitscanWeapon : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Public Settings
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The player's camera – ray is cast from its centre.")]
    public Camera playerCamera;

    [Tooltip("Muzzle point for VFX. Falls back to camera if unassigned.")]
    public Transform muzzlePoint;

    [Header("Fire Settings")]
    [Tooltip("Rounds per minute.")]
    public float fireRate = 600f;

    [Tooltip("Full-auto when true; semi-auto (click per shot) when false.")]
    public bool fullAuto = true;

    [Tooltip("Maximum effective range in metres.")]
    public float maxRange = 200f;

    [Tooltip("Layers that can be hit by the weapon.")]
    public LayerMask hitMask = ~0;   // everything by default

    [Header("Damage")]
    [Tooltip("Base damage per hit.")]
    public float baseDamage = 25f;

    [Tooltip("Damage multiplier on headshot (tag 'Head' on collider).")]
    public float headshotMultiplier = 2.0f;

    [Tooltip("Apply damage falloff beyond this distance (0 = disabled).")]
    public float falloffStartDistance = 50f;

    [Tooltip("Minimum damage at max range (as fraction of baseDamage, 0–1).")]
    [Range(0f, 1f)]
    public float minDamageFraction = 0.25f;

    [Header("Ammo")]
    public int magazineSize    = 30;
    public int reserveAmmo     = 90;
    public float reloadTime    = 2.0f;

    [Header("Spread")]
    [Tooltip("Base cone half-angle in degrees (0 = perfectly accurate).")]
    public float baseSpread    = 0.5f;

    [Tooltip("Extra spread added per shot while moving.")]
    public float movementSpread = 1.5f;

    [Header("VFX")]
    public GameObject muzzleFlashPrefab;
    public float      muzzleFlashDuration = 0.05f;

    public GameObject impactEffectPrefab;
    public float      impactEffectDuration = 1.0f;

    public GameObject impactDecalPrefab;
    public float      impactDecalDuration  = 10.0f;

    [Header("Tracers (optional)")]
    [Tooltip("Line renderer prefab to visualise the bullet path.")]
    public LineRenderer tracerPrefab;
    public float        tracerDuration = 0.04f;

    // -------------------------------------------------------------------------
    // Runtime State
    // -------------------------------------------------------------------------

    public int  CurrentAmmo   { get; private set; }
    public int  ReserveAmmo   { get; private set; }
    public bool IsReloading   { get; private set; }

    private float _nextFireTime;
    private float _fireInterval;

    private CharacterController _charController; // used for movement spread check

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        CurrentAmmo  = magazineSize;
        ReserveAmmo  = reserveAmmo;
        _fireInterval = 60f / Mathf.Max(fireRate, 1f);

        // Try to find a CharacterController on the root for movement detection
        _charController = GetComponentInParent<CharacterController>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        HandleInput();

        if (Input.GetKeyDown(KeyCode.R) && !IsReloading)
            StartCoroutine(Reload());
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    private void HandleInput()
    {
        if (IsReloading) return;

        bool trigger = fullAuto ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (trigger && Time.time >= _nextFireTime)
        {
            if (CurrentAmmo > 0)
            {
                Fire();
            }
            else
            {
                // Auto-reload when magazine is empty
                if (!IsReloading)
                    StartCoroutine(Reload());
            }
        }
    }

    // -------------------------------------------------------------------------
    // Fire
    // -------------------------------------------------------------------------

    private void Fire()
    {
        CurrentAmmo--;
        _nextFireTime = Time.time + _fireInterval;

        // --- Build ray with spread ---
        float spread = CalculateSpread();
        Vector3 direction = ApplySpread(playerCamera.transform.forward, spread);
        Ray ray = new Ray(playerCamera.transform.position, direction);

        // --- Muzzle flash ---
        if (muzzleFlashPrefab != null)
        {
            Transform origin = muzzlePoint != null ? muzzlePoint : playerCamera.transform;
            GameObject flash = Instantiate(muzzleFlashPrefab, origin.position, origin.rotation, origin);
            Destroy(flash, muzzleFlashDuration);
        }

        // --- Raycast ---
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitMask))
        {
            ProcessHit(hit, ray);
        }
        else
        {
            // Tracer to max range even on miss
            SpawnTracer(ray.origin, ray.origin + ray.direction * maxRange);
        }
    }

    // -------------------------------------------------------------------------
    // Hit Processing
    // -------------------------------------------------------------------------

    private void ProcessHit(RaycastHit hit, Ray ray)
    {
        // --- Calculate damage ---
        float damage = CalculateDamage(hit);

        // --- Apply to IDamageable ---
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            bool headshot = hit.collider.CompareTag("Head");
            if (headshot) damage *= headshotMultiplier;
            target.TakeDamage(damage, hit.point, hit.normal, headshot);
        }

        // --- Impact VFX ---
        SpawnImpactEffects(hit);

        // --- Tracer ---
        SpawnTracer(ray.origin, hit.point);
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

        Quaternion spreadRot = Quaternion.AngleAxis(roll, direction)
                             * Quaternion.AngleAxis(angle, playerCamera.transform.right);

        return spreadRot * direction;
    }

    // -------------------------------------------------------------------------
    // Reload
    // -------------------------------------------------------------------------

    private System.Collections.IEnumerator Reload()
    {
        if (ReserveAmmo <= 0 || CurrentAmmo == magazineSize) yield break;

        IsReloading = true;
        yield return new WaitForSeconds(reloadTime);

        int needed = magazineSize - CurrentAmmo;
        int drawn  = Mathf.Min(needed, ReserveAmmo);
        CurrentAmmo  += drawn;
        ReserveAmmo  -= drawn;
        IsReloading   = false;
    }

    // -------------------------------------------------------------------------
    // VFX Helpers
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
            // Parent decal to the hit object so it moves with it
            GameObject decal = Instantiate(impactDecalPrefab, hit.point, rot, hit.transform);
            Destroy(decal, impactDecalDuration);
        }
    }

    private void SpawnTracer(Vector3 from, Vector3 to)
    {
        if (tracerPrefab == null) return;

        LineRenderer lr = Instantiate(tracerPrefab);
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        Destroy(lr.gameObject, tracerDuration);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Force a reload from external code (e.g. pickup systems).</summary>
    public void ForceReload() => StartCoroutine(Reload());

    /// <summary>Add ammo to reserves (e.g. ammo pickup).</summary>
    public void AddAmmo(int amount) => ReserveAmmo = Mathf.Min(ReserveAmmo + amount, reserveAmmo * 2);
}

// =============================================================================
// IDamageable Interface
// Implement this on any GameObject that should receive damage (enemies, players)
// =============================================================================
public interface IDamageable
{
    /// <summary>
    /// Called when this object is hit.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    /// <param name="hitPoint">World-space point of impact.</param>
    /// <param name="hitNormal">Surface normal at impact point.</param>
    /// <param name="headshot">True if the hit collider is tagged "Head".</param>
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, bool headshot);
}
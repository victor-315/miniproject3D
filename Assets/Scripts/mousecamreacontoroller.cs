using UnityEngine;

/// <summary>
/// MouseCameraController - Attach to your Camera GameObject.
///
/// Controls:
///   - Move Mouse                               → Rotate / look around (cursor always locked)
///   - Middle Mouse Button (hold) + Move Mouse  → Pan (strafe left/right, up/down)
///   - Scroll Wheel                             → Zoom (move forward/back)
///   - Hold Shift                               → Speed boost
///   - Escape                                   → Release cursor (click window to re-lock)
/// </summary>
public class MouseCameraController : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Mouse sensitivity for look rotation.")]
    public float lookSensitivity = 2.0f;

    [Tooltip("Invert the vertical (pitch) axis.")]
    public bool invertY = false;

    [Header("Panning")]
    [Tooltip("Speed at which the camera pans with middle-mouse drag.")]
    public float panSpeed = 0.5f;

    [Header("Zoom")]
    [Tooltip("Speed at which the camera zooms with the scroll wheel.")]
    public float zoomSpeed = 10.0f;

    [Header("Speed Boost")]
    [Tooltip("Multiplier applied when Left Shift is held.")]
    public float shiftMultiplier = 3.0f;

    [Header("Smoothing")]
    [Tooltip("Smooth out rotation movement (0 = no smoothing).")]
    [Range(0f, 20f)]
    public float smoothing = 10.0f;

    // Internal state
    private float _yaw;
    private float _pitch;
    private float _targetYaw;
    private float _targetPitch;

    private void Start()
    {
        // Initialise from current rotation so the camera doesn't snap on play
        Vector3 euler = transform.eulerAngles;
        _yaw         = euler.y;
        _pitch       = euler.x;
        _targetYaw   = _yaw;
        _targetPitch = _pitch;

        LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Update()
    {
        // Escape releases the cursor; clicking the game window re-locks it
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }

        // Only drive the camera while the cursor is captured
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float speedMult = Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1.0f;

        HandleRotation(speedMult);
        HandlePan(speedMult);
        HandleZoom(speedMult);
    }

    // -------------------------------------------------------------------------
    // Rotation - always active while cursor is locked
    // -------------------------------------------------------------------------
    private void HandleRotation(float speedMult)
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float yDelta = invertY ? mouseY : -mouseY;

        _targetYaw   += mouseX * lookSensitivity * speedMult;
        _targetPitch += yDelta * lookSensitivity * speedMult;

        // Clamp pitch so the camera can't flip upside-down
        _targetPitch = Mathf.Clamp(_targetPitch, -89f, 89f);

        if (smoothing > 0f)
        {
            _yaw   = Mathf.LerpAngle(_yaw,   _targetYaw,   Time.deltaTime * smoothing);
            _pitch = Mathf.LerpAngle(_pitch, _targetPitch, Time.deltaTime * smoothing);
        }
        else
        {
            _yaw   = _targetYaw;
            _pitch = _targetPitch;
        }

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    // -------------------------------------------------------------------------
    // Pan - Middle Mouse Button drag
    // -------------------------------------------------------------------------
    private void HandlePan(float speedMult)
    {
        if (!Input.GetMouseButton(2)) return;   // 2 = middle mouse button

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 pan = transform.right * (-mouseX * panSpeed * speedMult)
                    + transform.up    * (-mouseY * panSpeed * speedMult);

        transform.position += pan;
    }

    // -------------------------------------------------------------------------
    // Zoom - Scroll Wheel
    // -------------------------------------------------------------------------
    private void HandleZoom(float speedMult)
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        transform.position += transform.forward * scroll * zoomSpeed * speedMult;
    }
}
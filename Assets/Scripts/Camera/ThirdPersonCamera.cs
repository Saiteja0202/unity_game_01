using UnityEngine;

/// PUBG-style over-the-shoulder camera.
/// Accepts mouse input on desktop AND touch-drag delta from MobileInputProvider.
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3   shoulderOffset = new Vector3(0.55f, 1.65f, 0f);

    [Header("Rotation")]
    public float mouseSensitivity  = 2.8f;
    public float touchSensitivity  = 0.12f;  // pixels → degrees for mobile
    public float minPitch          = -25f;
    public float maxPitch          =  65f;

    [Header("Zoom")]
    public float distance    = 3.5f;
    public float minDistance = 1.2f;
    public float maxDistance = 6f;
    public float zoomSpeed   = 3f;
    public float zoomSmooth  = 8f;

    [Header("Collision")]
    public float      collisionRadius = 0.18f;
    public LayerMask  collisionMask   = ~0;

    private float yaw;
    private float pitch = 10f;
    private float currentDistance;

    void Start()
    {
        currentDistance     = distance;
        Cursor.lockState    = CursorLockMode.Locked;
        Cursor.visible      = false;
    }

    void Update()
    {
        // Cursor lock toggle (desktop)
#if ENABLE_LEGACY_INPUT_MANAGER
        bool _escPressed = Input.GetKeyDown(KeyCode.Escape);
#else
        bool _escPressed = UnityEngine.InputSystem.Keyboard.current != null
                        && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
        if (_escPressed)
        {
            bool locked         = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState    = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible      = !locked;
        }

        // ── Look input: mobile drag OR mouse ────────────────────────────────
        var mobile = MobileInputProvider.Instance;
        if (mobile != null && mobile.HasTouchLook)
        {
            // Raw pixel delta from TouchLookArea
            yaw   += mobile.LookDelta.x * touchSensitivity;
            pitch -= mobile.LookDelta.y * touchSensitivity;
        }
        else if (Cursor.lockState == CursorLockMode.Locked)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
#else
            var _md = UnityEngine.InputSystem.Mouse.current?.delta.ReadValue() ?? Vector2.zero;
            yaw   += _md.x * mouseSensitivity * 0.05f;
            pitch -= _md.y * mouseSensitivity * 0.05f;
#endif
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ── Scroll zoom (desktop) ────────────────────────────────────────────
#if ENABLE_LEGACY_INPUT_MANAGER
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
#else
        distance -= (UnityEngine.InputSystem.Mouse.current?.scroll.ReadValue().y ?? 0f) * zoomSpeed * 0.01f;
#endif
        distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * zoomSmooth);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion rot        = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    focusPoint = target.position + shoulderOffset;
        Vector3    desiredPos = focusPoint - rot * Vector3.forward * currentDistance;

        // Camera collision
        if (Physics.SphereCast(focusPoint, collisionRadius,
                (desiredPos - focusPoint).normalized,
                out RaycastHit hit, currentDistance, collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            desiredPos = hit.point + hit.normal * collisionRadius;
        }

        transform.position = desiredPos;
        transform.LookAt(focusPoint);
    }

    public float Yaw   => yaw;
    public float Pitch => pitch;
}

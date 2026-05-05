using UnityEngine;

/// Singleton bridge between mobile UI widgets and game systems.
/// PlayerController and ThirdPersonCamera read from this.
public class MobileInputProvider : MonoBehaviour
{
    public static MobileInputProvider Instance { get; private set; }

    [Header("References (auto-assigned by GameSceneSetup)")]
    public VirtualJoystick moveJoystick;
    public TouchLookArea   lookArea;

    // ── Public input getters ────────────────────────────────────────────────

    /// Normalised move direction from joystick [-1..1] on each axis.
    public Vector2 MoveInput => moveJoystick != null ? moveJoystick.Direction : Vector2.zero;

    /// Raw pixel delta from the look drag area this frame.
    public Vector2 LookDelta => lookArea != null ? lookArea.Delta : Vector2.zero;

    public bool HasTouchLook => lookArea != null && lookArea.IsDragging;

    /// Sprint is automatic when joystick is pushed beyond 85 %.
    public bool SprintHeld =>
        moveJoystick != null && moveJoystick.Direction.magnitude > 0.85f;

    public bool CrouchHeld   { get; private set; }

    // Jump is a one-shot: set true by button press, consumed once by PlayerController.
    private bool jumpPending;
    public bool ConsumeJump()
    {
        bool j      = jumpPending;
        jumpPending = false;
        return j;
    }

    // ── UI button callbacks (hook these up to Button OnClick / EventTrigger) ─

    public void OnJumpPressed()   => jumpPending = true;
    public void OnCrouchDown()    => CrouchHeld  = true;
    public void OnCrouchUp()      => CrouchHeld  = false;

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed   = 3.5f;
    public float runSpeed    = 7.5f;
    public float crouchSpeed = 1.8f;
    public float jumpForce   = 6f;
    public float gravity     = -22f;

    [Header("Crouch Settings")]
    public float standHeight  = 1.8f;
    public float crouchHeight = 1.0f;

    [Header("State (Read Only)")]
    public PlayerState CurrentState = PlayerState.Idle;
    public float HorizontalSpeed;
    public bool  IsGrounded;
    public bool  IsCrouching;

    public enum PlayerState { Idle, Walking, Running, Jumping, Crouching, CrouchWalking }

    private CharacterController cc;
    private Vector3 velocity;
    private Transform cam;
    private float smoothTurnVelocity;

    void Awake()
    {
        cc  = GetComponent<CharacterController>();
        cam = Camera.main?.transform;
    }

    void Update()
    {
        IsGrounded = cc.isGrounded;
        if (IsGrounded && velocity.y < 0f) velocity.y = -3f;

        // ── Gather input: mobile takes priority, keyboard is fallback ───────
        float  h          = Input.GetAxisRaw("Horizontal");
        float  v          = Input.GetAxisRaw("Vertical");
        bool   sprint     = Input.GetKey(KeyCode.LeftShift);
        bool   crouchHeld = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        bool   jumped     = Input.GetKeyDown(KeyCode.Space);

        var mobile = MobileInputProvider.Instance;
        if (mobile != null)
        {
            Vector2 joyDir = mobile.MoveInput;
            if (joyDir.sqrMagnitude > 0.01f) { h = joyDir.x; v = joyDir.y; }
            if (mobile.CrouchHeld) crouchHeld = true;
            if (mobile.SprintHeld) sprint      = true;
            jumped |= mobile.ConsumeJump();
        }

        // ── Crouch height transition ─────────────────────────────────────────
        IsCrouching = crouchHeld;
        float targetHeight = IsCrouching ? crouchHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, targetHeight, Time.deltaTime * 14f);
        cc.center = Vector3.up * (cc.height * 0.5f);

        // ── Movement direction (camera-relative) ─────────────────────────────
        Vector2 moveInput = new Vector2(h, v);
        Vector3 moveDir   = Vector3.zero;

        if (moveInput.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg
                                + (cam ? cam.eulerAngles.y : 0f);
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                              ref smoothTurnVelocity, 0.07f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        float speed = IsCrouching ? crouchSpeed : (sprint ? runSpeed : walkSpeed);
        cc.Move(moveDir.normalized * speed * Time.deltaTime);

        // ── Jump & gravity ───────────────────────────────────────────────────
        if (jumped && IsGrounded) velocity.y = jumpForce;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        // ── State machine ─────────────────────────────────────────────────────
        HorizontalSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;

        if (!IsGrounded)
            CurrentState = PlayerState.Jumping;
        else if (IsCrouching && HorizontalSpeed > 0.1f)
            CurrentState = PlayerState.CrouchWalking;
        else if (IsCrouching)
            CurrentState = PlayerState.Crouching;
        else if (HorizontalSpeed > 5.5f)
            CurrentState = PlayerState.Running;
        else if (HorizontalSpeed > 0.1f)
            CurrentState = PlayerState.Walking;
        else
            CurrentState = PlayerState.Idle;
    }
}

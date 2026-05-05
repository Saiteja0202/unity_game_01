using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Fixed on-screen joystick. Works with both touch and mouse.
/// Drop on the joystick background Image; assign the handle child RectTransform.
[RequireComponent(typeof(Image))]
public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Handle")]
    public RectTransform handle;

    [Range(0f, 1f)]
    public float deadzone = 0.08f;

    // Normalised direction [-1..1] on each axis. Zero when not touched.
    public Vector2 Direction { get; private set; }
    public bool    IsPressed { get; private set; }

    // Convenience
    public float Horizontal => Direction.x;
    public float Vertical   => Direction.y;

    private RectTransform bgRect;
    private Canvas        rootCanvas;

    void Awake()
    {
        bgRect      = GetComponent<RectTransform>();
        rootCanvas  = GetComponentInParent<Canvas>();
    }

    // ── Pointer events ─────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        IsPressed = true;
        MoveHandle(e);
    }

    public void OnDrag(PointerEventData e) => MoveHandle(e);

    public void OnPointerUp(PointerEventData e)
    {
        IsPressed  = false;
        Direction  = Vector2.zero;
        if (handle) handle.anchoredPosition = Vector2.zero;
    }

    // ── Internal ───────────────────────────────────────────────────────────

    void MoveHandle(PointerEventData e)
    {
        if (handle == null) return;

        // Camera for world-space canvases; null for overlay
        Camera uiCam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgRect, e.position, uiCam, out Vector2 localPos);

        float maxRadius = bgRect.sizeDelta.x * 0.5f;
        Vector2 clamped = Vector2.ClampMagnitude(localPos, maxRadius);

        handle.anchoredPosition = clamped;

        Vector2 normalised = clamped / maxRadius;
        Direction = normalised.magnitude < deadzone ? Vector2.zero : normalised;
    }
}

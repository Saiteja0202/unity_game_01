using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Transparent drag area on the right side of the screen.
/// Outputs per-frame pixel delta so ThirdPersonCamera can rotate.
[RequireComponent(typeof(Image))]
public class TouchLookArea : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // Raw pixel delta for this frame. Reset every LateUpdate.
    public Vector2 Delta     { get; private set; }
    public bool    IsDragging { get; private set; }

    private Vector2 prevPos;

    public void OnPointerDown(PointerEventData e)
    {
        IsDragging = true;
        prevPos    = e.position;
        Delta      = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        Delta   = e.position - prevPos;
        prevPos = e.position;
    }

    public void OnPointerUp(PointerEventData e)
    {
        IsDragging = false;
        Delta      = Vector2.zero;
    }

    void LateUpdate()
    {
        // Clear delta at end of frame so camera only rotates when actively dragging
        if (!IsDragging) Delta = Vector2.zero;
    }
}

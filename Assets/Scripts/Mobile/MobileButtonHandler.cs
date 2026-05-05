using UnityEngine;
using UnityEngine.EventSystems;

/// Attached to each mobile button by GameSceneSetup.
/// Uses IPointer interfaces instead of Button.onClick lambdas,
/// so it works correctly after scene save / reload.
public class MobileButtonHandler : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public enum ButtonAction { Jump, CrouchHold }
    public ButtonAction action;

    // Jump fires on pointer-down for maximum responsiveness.
    public void OnPointerDown(PointerEventData e)
    {
        if (action == ButtonAction.Jump)
            MobileInputProvider.Instance?.OnJumpPressed();
        else if (action == ButtonAction.CrouchHold)
            MobileInputProvider.Instance?.OnCrouchDown();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (action == ButtonAction.CrouchHold)
            MobileInputProvider.Instance?.OnCrouchUp();
    }

    // Suppress the default Button click sound / animation without needing a Button component.
    public void OnPointerClick(PointerEventData e) { }
}

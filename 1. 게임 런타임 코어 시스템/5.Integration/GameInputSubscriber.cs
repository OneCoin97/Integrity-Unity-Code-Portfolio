using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameInputSubscriber : MonoBehaviour
{
    private MainInputAction inputAction;

    private void Start()
    {
        inputAction = new MainInputAction();
        inputAction.Enable();
        inputAction.Map.TurnEnd.performed += onTurnEndPressed;
        inputAction.Map.SelectUnit1.performed += onSelectUnit1Pressed;
        inputAction.Map.SelectUnit2.performed += onSelectUnit2Pressed;
        inputAction.Map.SelectUnit3.performed += onSelectUnit3Pressed;
        inputAction.Map.SelectUnit4.performed += onSelectUnit4Pressed;
    }

    private void OnDestroy()
    {
        if (inputAction == null) return;

        inputAction.Map.TurnEnd.performed -= onTurnEndPressed;
        inputAction.Map.SelectUnit1.performed -= onSelectUnit1Pressed;
        inputAction.Map.SelectUnit2.performed -= onSelectUnit2Pressed;
        inputAction.Map.SelectUnit3.performed -= onSelectUnit3Pressed;
        inputAction.Map.SelectUnit4.performed -= onSelectUnit4Pressed;
        inputAction.Disable();
        inputAction.Dispose();
        inputAction = null;
    }

    private void onTurnEndPressed(InputAction.CallbackContext context)
    {
        if (InputManager.isModifierAction) return;

        GameSessionManager.GetInst.requestTurnEnd();
    }

    private void onSelectUnit1Pressed(InputAction.CallbackContext context)
    {
        if (!InputManager.isModifierAction) UnitSelectionManager.GetInst.selectUnitByInputNumber(0);
    }

    private void onSelectUnit2Pressed(InputAction.CallbackContext context)
    {
        if (!InputManager.isModifierAction) UnitSelectionManager.GetInst.selectUnitByInputNumber(1);
    }

    private void onSelectUnit3Pressed(InputAction.CallbackContext context)
    {
        if (!InputManager.isModifierAction) UnitSelectionManager.GetInst.selectUnitByInputNumber(2);
    }

    private void onSelectUnit4Pressed(InputAction.CallbackContext context)
    {
        if (!InputManager.isModifierAction) UnitSelectionManager.GetInst.selectUnitByInputNumber(3);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    PlayerInputActions inputActions;
    public float move;
    public Vector2 look;
    public bool boost = false;
    public bool pickup = false;
    public bool analogMovement;
    public float altitude;

    public bool cursorLocked = true;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        inputActions.Player.Boost.performed += Boost;
        inputActions.Player.Boost.canceled += Boost;
        inputActions.Player.Move.performed += Move;
        inputActions.Player.Move.canceled += MoveCanceled;
        inputActions.Player.Pickup.performed += Pickup;
        //inputActions.Player.Pickup.canceled += Pickup;
    }

    private void Update()
    {
        look = inputActions.Player.Look.ReadValue<Vector2>();
        altitude = inputActions.Player.Altitude.ReadValue<float>();
    }

    void Move(InputAction.CallbackContext context)
    {
        move = context.ReadValue<float>();
    }

    void MoveCanceled(InputAction.CallbackContext context)
    {
        move = 0;
    }

    void Boost(InputAction.CallbackContext context)
    {
        boost = !boost;
    }

    void Pickup(InputAction.CallbackContext context) 
    {
        //pickup = !pickup;
        pickup = true;
    }

    private void OnApplicationFocus(bool focus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}

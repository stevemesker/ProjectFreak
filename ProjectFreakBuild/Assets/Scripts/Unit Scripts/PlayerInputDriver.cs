using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class PlayerInputDriver : MonoBehaviour
{
    //Reads the player's controls and tells CharacterMovement where to move and face.
    //Turning this component on or off is how control moves between the player and a shade (see ShadeManager).

    [Header("Settings")]
    [Tooltip("How long after you stop aiming before the unit goes back to facing the way it moves, in seconds")]
    [SerializeField, Min(0f)] float _TurnIdleTime = 1f;

    [Header("References")]
    [Tooltip("The movement engine this driver steers. Grabbed from this object if left empty")]
    [SerializeField] CharacterMovement _Movement;

    [Header("Runtime Data")]
    [Tooltip("True while the unit faces the way it's moving, because nothing has aimed recently (read only)")]
    [SerializeField, ReadOnly] bool _IsTurnSnapped = true;

    [Tooltip("True if the last aim came from the mouse rather than the right stick (read only)")]
    [SerializeField, ReadOnly] bool _IsUsingMouse;

    //local variables
    PlayerInput _input;
    Coroutine _idleTimer;
    Vector3 _moveDirection; //the current move direction in world space

    private void Awake()
    {
        if (_Movement == null) _Movement = GetComponent<CharacterMovement>();
        if (_Movement == null) { Debug.LogError($"Error! No CharacterMovement found for the PlayerInputDriver on {gameObject.name}", this); enabled = false; return; }
        _input = new PlayerInput();
    }

    private void OnEnable()
    {
        //turns the controls on and listens for input
        if (_input == null) return;
        _input.Enable();

        _input.Player.Move.performed += MoveInput;
        _input.Player.Move.canceled += MoveInput;

        _input.Player.Look.performed += StickTurn;
        _input.Player.Look.canceled += EndStickTurn;

        _input.Player.Point.performed += MouseInput;
        _input.Player.Point.canceled += MouseStopInput;

        _input.Player.Dash.performed += DashInput;
    }

    private void OnDisable()
    {
        //stops listening for input when control moves somewhere else
        if (_input == null) return;
        _input.Player.Move.performed -= MoveInput;
        _input.Player.Move.canceled -= MoveInput;

        _input.Player.Look.performed -= StickTurn;
        _input.Player.Look.canceled -= EndStickTurn;

        _input.Player.Point.performed -= MouseInput;
        _input.Player.Point.canceled -= MouseStopInput;

        _input.Player.Dash.performed -= DashInput;

        _input.Disable();
        StopIdleTimer();

        //clear the move direction so the unit stops instead of walking on with nobody steering it
        //(the "stick released" event never arrives once we stop listening). Facing is left alone
        _moveDirection = Vector3.zero;
        _Movement.SetMoveDirection(Vector3.zero);
    }

    private void OnDestroy()
    {
        //PlayerInput holds onto input system resources, Dispose frees them
        if (_input != null) _input.Dispose();
    }

    #region Inputs
    void MoveInput(InputAction.CallbackContext context)
    {
        //function that turns the move stick/keys into a camera-relative move direction
        Vector2 stickInput = context.ReadValue<Vector2>();
        _moveDirection = CameraRelative(new Vector3(stickInput.x, 0f, stickInput.y));
        _Movement.SetMoveDirection(_moveDirection);

        //face the way we're moving unless we've aimed recently
        if (_IsTurnSnapped) _Movement.SetLookDirection(_moveDirection);
        else if (_IsUsingMouse) _Movement.SetLookDirection(GetMouseAimDirection());
    }

    void StickTurn(InputAction.CallbackContext context)
    {
        //function for aiming with the gamepad's right stick
        Vector2 stickInput = context.ReadValue<Vector2>();
        _Movement.SetLookDirection(CameraRelative(new Vector3(stickInput.x, 0f, stickInput.y)));
        _IsUsingMouse = false;
        _IsTurnSnapped = false;
        StopIdleTimer();
    }

    void EndStickTurn(InputAction.CallbackContext context)
    {
        //right stick let go, start counting down to facing the move direction again
        StartIdleTimer();
    }

    void MouseInput(InputAction.CallbackContext context)
    {
        //function for aiming with the mouse
        StopIdleTimer();
        _IsUsingMouse = true;
        _IsTurnSnapped = false;
        _Movement.SetLookDirection(GetMouseAimDirection());
    }

    void MouseStopInput(InputAction.CallbackContext context)
    {
        //mouse aim stopped (like clicking outside the window), start counting down to facing the move direction again
        StartIdleTimer();
    }

    void DashInput(InputAction.CallbackContext context)
    {
        //dash in whatever direction we're moving
        _Movement.Dash();
    }
    #endregion

    #region Idle Timer
    void StartIdleTimer()
    {
        //function that restarts the countdown back to facing the move direction
        StopIdleTimer();
        _idleTimer = StartCoroutine(IdleTimerRoutine());
    }

    void StopIdleTimer()
    {
        //function that cancels the countdown
        if (_idleTimer != null) StopCoroutine(_idleTimer);
        _idleTimer = null;
    }

    IEnumerator IdleTimerRoutine()
    {
        //waits, then goes back to facing the way we're moving
        yield return new WaitForSeconds(_TurnIdleTime);
        _IsTurnSnapped = true;
        _idleTimer = null;

        //turn to face the move direction right away, instead of waiting for the next move input
        _Movement.SetLookDirection(_moveDirection);
    }
    #endregion

    #region Tools
    Vector3 CameraRelative(Vector3 input)
    {
        //function that rotates stick input so "up" means "toward the top of the screen"
        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform == null) return input; //no camera to line up with, use the input as-is

        Quaternion cameraYaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f); //only the camera's left/right turn matters
        return cameraYaw * input;
    }

    Transform GetCameraTransform()
    {
        //function that finds the gameplay camera, asking the Camera Manager first
        if (CameraManager._CamManager != null && CameraManager._CamManager._currentGameplayCamera != null) return CameraManager._CamManager._currentGameplayCamera.transform;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    Vector3 GetMouseAimDirection()
    {
        //function that works out which way to face to look at the mouse cursor
        if (Camera.main == null || Mouse.current == null) return transform.forward; //no camera or no mouse plugged in

        Ray mouseRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        //an invisible flat plane at the unit's height, so aiming works the same over any terrain
        Plane unitPlane = new Plane(Vector3.up, transform.position);

        if (unitPlane.Raycast(mouseRay, out float distance))
        {
            Vector3 direction = mouseRay.GetPoint(distance) - transform.position;
            direction.y = 0f;
            return direction.normalized;
        }

        return transform.forward;
    }
    #endregion
}

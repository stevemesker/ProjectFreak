using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    [Header("Stats")]
    public Rigidbody _RB;
    public GameObject _MainCamera;
    private PlayerInput pInput;
    Vector3 DownDir;
    RaycastHit _rayHit;
    [Tooltip("True if floor was detected under gameobject")]public bool _rayDidHit;

    [Header("Standing")]
    [SerializeField, Tooltip("How far to cast the ray to find the standing upright position")] float RayLength;
    [SerializeField, Tooltip("The desired height of the character")] float RideHeight;
    [SerializeField, Tooltip("Strength of the spring force")] float RideSpringStrength;
    [SerializeField, Tooltip("Resistance strength of the spring force")] float RideSpringDamper;

    [Header("Locomotion")]
    [SerializeField] float maxSpeed = 8;
    [SerializeField] float acceleration = 200;
    [SerializeField] float maxAccelForce = 150;
    [SerializeField] float speedFactor = 1;
    [SerializeField] public Vector3 m_UnitGoal;
    Vector3 m_GoalVel;

    [Header("Turning")]
    [SerializeField, Tooltip("When true, the unit's rotation will match their movement vector")] bool isTurnSnapped = true;
    [SerializeField] float TurnSpeed;
    [SerializeField] public Vector3 m_turnGoal;
    [SerializeField] float turnIdleTime = 1;
    Coroutine IdleTimer;

    [Header("Mouse Settings")]
    [SerializeField] bool isUsingMouse;

    [Header("Debug")]
    [SerializeField] bool OnDebugDrawLines;
    [SerializeField] float lineLength = 2;

    #region Initialize
    private void OnEnable()
    {
        pInput = new PlayerInput();
        pInput.Enable();

        pInput.Player.Move.performed += MovementInput;
        pInput.Player.Move.canceled += MovementInput;

        pInput.Player.Look.performed += StickTurn;
        pInput.Player.Look.canceled += StickTurn;
        pInput.Player.Look.canceled += EndStickTurn;

        pInput.Player.Point.performed += MouseInput;
        pInput.Player.Point.canceled += MouseStopInput;
    }

    private void OnDisable()
    {
        pInput.Disable();
    }
    private void Awake()
    {
        _RB = GetComponent<Rigidbody>();
        DownDir = Vector3.down;
    }
    #endregion

    private void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out _rayHit, RayLength)) _rayDidHit = true;
        else _rayDidHit = false;
        StandingForce();
        MovementForce();

        if (OnDebugDrawLines) debugLineDraw();
    }


    private void StandingForce()
    {
        if (_rayDidHit)
        {
            Vector3 vel = _RB.velocity;
            Vector3 rayDir = transform.TransformDirection(DownDir);

            Vector3 otherVel = Vector3.zero;
            Rigidbody hitBody = _rayHit.rigidbody;
            if (hitBody != null)
            {
                otherVel = hitBody.velocity;
            }

            float rayDirVel = Vector3.Dot(rayDir, vel);
            float otherDirVel = Vector3.Dot(rayDir, otherVel);

            float relVel = rayDirVel - otherDirVel;

            float x = _rayHit.distance - RideHeight;

            float springForce = (x * RideSpringStrength) - (relVel * RideSpringDamper);

            Debug.DrawLine(transform.position, transform.position + (rayDir * springForce), Color.yellow);

            _RB.AddForce(rayDir * springForce);

            if (hitBody != null)
            {
                hitBody.AddForceAtPosition(rayDir * -springForce, _rayHit.point);
            }
        }
    }

    void MovementForce()
    {
        Vector3 desiredVelocity = m_UnitGoal * maxSpeed;

        Vector3 currentVelocity = _RB.velocity;
        currentVelocity.y = 0f;

        Vector3 velocityDelta =
            desiredVelocity - currentVelocity;

        Vector3 desiredAccel =
            velocityDelta * acceleration;

        Vector3 accelForce =
            Vector3.ClampMagnitude(
                desiredAccel * _RB.mass,
                maxAccelForce);

        _RB.AddForce(accelForce);
    }

    #region Inputs
    void MovementInput(InputAction.CallbackContext context)
    {
        Vector2 stickInput = context.ReadValue<Vector2>();
        Vector3 move = new Vector3(stickInput.x,0, stickInput.y);
        m_UnitGoal = ConvertMovementScreenSpace(move);
        if (isTurnSnapped) m_turnGoal = m_UnitGoal;
    }
    
    void StickTurn(InputAction.CallbackContext context)
    {
        //function for tracking gamepad stick turning input
        Vector2 stickInput = context.ReadValue<Vector2>();
        Vector3 roate = new Vector3(stickInput.x, 0, stickInput.y);
        m_turnGoal = ConvertMovementScreenSpace(roate);
        isUsingMouse = false;
        isTurnSnapped = false;
        IdleTimer = null;
    }

    void EndStickTurn(InputAction.CallbackContext context)
    {
        IdleTimer = StartCoroutine(IdleTimerCoroutine());
    }

    void MouseInput(InputAction.CallbackContext context)
    {
        if (IdleTimer != null) StopCoroutine(IdleTimer);
        IdleTimer = null;
        isUsingMouse = true;
        isTurnSnapped = false;
        m_turnGoal = GetMouseAimDirection();
    }

    void MouseStopInput(InputAction.CallbackContext context)
    {
        //when the mouse clicks outside of the window apparently
        IdleTimer = StartCoroutine(IdleTimerCoroutine());
    }

    #endregion

    #region Tools
    Vector3 ConvertMovementScreenSpace(Vector3 input)
    {
        Quaternion cameraRotation = Quaternion.Euler(
        0f,
        _MainCamera.transform.eulerAngles.y,
        0f);

        return (cameraRotation * input);
    }

    Vector3 GetMouseAimDirection()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Plane at player's height
        Plane playerPlane = new Plane(
            Vector3.up,
            transform.position);

        if (playerPlane.Raycast(mouseRay, out float distance))
        {
            Vector3 hitPoint = mouseRay.GetPoint(distance);

            Vector3 direction = hitPoint - transform.position;
            direction.y = 0f;

            return direction.normalized;
        }

        return transform.forward;
    }

    private IEnumerator IdleTimerCoroutine()
    {
        print("Starting coroutine...");
        yield return new WaitForSeconds(turnIdleTime);
        if (IdleTimer != null) isTurnSnapped = true;
        m_turnGoal = m_UnitGoal;
    }

    void debugLineDraw()
    {
        //forward vector
        Debug.DrawLine(transform.position, transform.forward * lineLength + transform.position, Color.blue);

        //movement direction vector
        Debug.DrawLine(transform.position, transform.position + m_UnitGoal * lineLength, Color.green);

        //rotation direction vector
        if (isTurnSnapped) Debug.DrawLine(transform.position, m_turnGoal * lineLength + transform.position, Color.black);
        else
        {
            if (isUsingMouse)
                Debug.DrawLine(transform.position, transform.position + m_turnGoal * lineLength, Color.red);
            else
                Debug.DrawLine(transform.position, transform.position + m_turnGoal * lineLength, Color.yellow);
        }
    }
    #endregion
}

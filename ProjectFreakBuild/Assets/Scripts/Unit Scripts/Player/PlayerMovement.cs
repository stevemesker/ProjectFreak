using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class PlayerMovement : MonoBehaviour
{
    #region Variables
    /////////////////////////////////////////////////
    [Header("MOVEMENT Variables")]
    /////////////////////////////////////////////////
    //--

    [SerializeField, Range(0f, 1f), Tooltip("controls how far the player has to move the sticks before forward movement will occure")]
    float stickSensitivity; 

    [SerializeField, Range(0f, 1f), Tooltip("How far from centered the sticks must be to be considered moving")]
    float lookSensitivity;

    [SerializeField, Range(0f, 100f), Tooltip("Max Running Speed")]
    float Speed = 10f;

    [SerializeField, Range(0f, 100f), Tooltip("How fast the player accelerates")]
    float acceleration = 20f;

    [SerializeField, Range(0f, 100f), Tooltip("How fast the player decelerates")]
    float deceleration = 20f;

    [SerializeField, Range(0f, 25f), Tooltip("Speed at which the player turns")]
    float lookSpeed = 10f;

    [SerializeField, Range(0f,1f), Tooltip("How far the sticks must move to not hit a dead zone")] 
    private float stickDeadzone = 0.2f;

    /////////////////////////////////////////////////
    [Header("Rotation Code")]
    /////////////////////////////////////////////////
    //--
    
    private PlayerInput pInput;
    [SerializeField] private Vector2 lStickInput;//controller input movement
    [SerializeField] private Vector2 rStickInput;//controller input aim
    [SerializeField] private Vector3 moveVector; //handles the direction the player should move
    [SerializeField] private Vector3 lookVector; //handles the direction the player should face when moving

    [SerializeField] private bool isIdleAim = true; //determines if the system should rotate the player based on aim rotation or movement direction
    [SerializeField, Range(0f, 10f), Tooltip("handles how long the player unit should wait to use move direction location as the looking direction")]
    public float lookIdleTimer;
    private Coroutine lookIdleCoroutine;

    //--

    /////////////////////////////////////////////////
    [Header("Interaction Code")]
    /////////////////////////////////////////////////
    //--
    [SerializeField] public GameObject pCamera; //stores the player's camera
    [SerializeField] float grabDistance = 1; //how far something can be away from the player to grab it
    [SerializeField] RaycastHit interactRay; //used for picking up objects and interacting

    //--

    /////////////////////////////////////////////////
    [Header("State Variables")]
    /////////////////////////////////////////////////
    //--
    [SerializeField] private bool isGamepad; //lets the system know if gamepad is being used or if mouse and keyboard
    //--

    /////////////////////////////////////////////////
    [Header("Debug Variables")]
    /////////////////////////////////////////////////
    //--
    [SerializeField] private bool OnDebugLines; //toggle for movement debug lines
    [SerializeField, Range(0f, 5f)] float debugLineLength;
    //--

    //private variables
    private Vector3 placerTargetLocation;
    private bool lockPlacer;
    private Rigidbody rb;
    private float currentPlanarSpeed;            // optional, if you prefer speed smoothing instead of velocity move-towards
    private bool rightStickActive;               // tracks aim activity for idle logic


    [Header("_______Temp Variables_______")]
    public Vector3 testVector3; //current rotation of the camera
    public bool boop;

    #endregion

    #region Initializing
    private void Awake()
    {
        pInput = new PlayerInput();
        if (pCamera)
        {
            //pCamera.GetComponent<cameraScript>().enabled = true;
        }
    }

    private void OnEnable()
    {
        pInput.Enable();

        pInput.Player.Move.performed += MoveInput;
        pInput.Player.Move.canceled += MoveInput;
        pInput.Player.Look.performed += LookInput;
        pInput.Player.Look.canceled += LookInput;
        pInput.Player.Look.canceled += LookIdle;



        rb = gameObject.GetComponent<Rigidbody>();
    }


    private void OnDisable()
    {
        pInput.Disable();
    }

    #endregion

    #region Movement
    //MOVEMENT CODE
    private void FixedUpdate()
    {
        if (!rb) return;

        // 1) Build moveVector from left stick (world space top-down: XZ)
        moveVector = new Vector3(lStickInput.x, 0f, lStickInput.y);
        if (moveVector.sqrMagnitude > 0.0001f)
            moveVector.Normalize();

        // 2) Movement via Rigidbody velocity (accelerate/decelerate)
        Vector3 targetPlanarVel = moveVector * Speed;

        Vector3 currentVel = rb.velocity;
        Vector3 currentPlanarVel = new Vector3(currentVel.x, 0f, currentVel.z);

        // Choose accel or decel depending on whether we have input
        float rate = (moveVector.sqrMagnitude > 0.0001f) ? acceleration : deceleration;

        Vector3 newPlanarVel = Vector3.MoveTowards(
            currentPlanarVel,
            targetPlanarVel,
            rate * Time.fixedDeltaTime
        );

        rb.velocity = new Vector3(newPlanarVel.x, currentVel.y, newPlanarVel.z);

        // 3) Facing logic:
        // - If right stick is active (outside deadzone), face that
        // - Else if idle aim (or after timer), face move direction (left stick)
        Vector3 rightDir = new Vector3(rStickInput.x, 0f, rStickInput.y);
        bool aimHasInput = rightDir.sqrMagnitude >= stickDeadzone * stickDeadzone;

        if (aimHasInput)
        {
            // Right stick facing
            lookVector = rightDir.normalized;
            isIdleAim = false; // keep consistent even if callback timing varies
        }
        else
        {
            // Right stick idle -> default facing to movement when allowed
            if (isIdleAim && moveVector.sqrMagnitude > 0.0001f)
            {
                lookVector = moveVector;
            }
            // else: keep last lookVector (don’t snap)
        }

        // 4) Apply rotation (turn speed)
        if (lookVector.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookVector, Vector3.up);

            // Your lookSpeed is currently 0-25, so treat it like degrees/sec multiplier
            Quaternion newRot = Quaternion.RotateTowards(
                rb.rotation,
                targetRot,
                (lookSpeed * 90f) * Time.fixedDeltaTime
            );

            rb.MoveRotation(newRot);
        }
    }




    #endregion

    #region Input
    //INPUT CODE
    private void MoveInput(InputAction.CallbackContext context)
    {
        lStickInput = context.ReadValue<Vector2>();

        // Apply stick sensitivity / deadzone for movement
        if (lStickInput.sqrMagnitude < stickSensitivity * stickSensitivity)
            lStickInput = Vector2.zero;
    }

    private void LookInput(InputAction.CallbackContext context)
    {
        rStickInput = context.ReadValue<Vector2>();

        // Determine if the right stick is "active"
        rightStickActive = rStickInput.sqrMagnitude >= stickDeadzone * stickDeadzone;

        if (rightStickActive)
        {
            // Right stick takes control of facing immediately
            isIdleAim = false;

            // If you were waiting to return to move-facing, cancel that
            if (lookIdleCoroutine != null)
            {
                StopCoroutine(lookIdleCoroutine);
                lookIdleCoroutine = null;
            }
        }

        // Apply look sensitivity / deadzone
        if (rStickInput.sqrMagnitude < lookSensitivity * lookSensitivity)
            rStickInput = Vector2.zero;
    }

    private void LookIdle(InputAction.CallbackContext context)
    {
        // Called when Look is canceled (you already hooked this up)
        // Start a timer to return to movement-facing (prevents snap-back)
        if (lookIdleCoroutine != null)
            StopCoroutine(lookIdleCoroutine);

        lookIdleCoroutine = StartCoroutine(LookIdleCountdown());
    }

    private IEnumerator LookIdleCountdown()
    {
        yield return new WaitForSeconds(lookIdleTimer);
        isIdleAim = true;
        lookIdleCoroutine = null;
    }

    #endregion
}

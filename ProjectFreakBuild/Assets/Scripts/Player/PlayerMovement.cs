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
        if (OnDebugLines) drawlines(); //debug Delete Later

        if (moveVector == Vector3.zero) { print("stopped"); return; }
        Vector3 newHorizontal = Vector3.MoveTowards(transform.position, transform.position + moveVector, (acceleration * Vector2.Distance(Vector2.zero, lStickInput)) * Time.fixedDeltaTime);
        rb.velocity = newHorizontal;
    }



    #endregion

    #region Input
    //INPUT CODE
    public void MoveInput(InputAction.CallbackContext context)
    {
        lStickInput = context.ReadValue<Vector2>();
        float lStickDistance = Vector2.Distance(Vector2.zero, lStickInput); //grab 2d vector of the sticks/keyboard

        
        if (lStickDistance <= stickSensitivity) { moveVector = Vector3.zero; return; }

        if (lStickDistance >= lookSensitivity)
        {
            moveVector = new Vector3(lStickInput.x, 0f, lStickInput.y);
        }
        else moveVector = Vector3.zero;

        if (isIdleAim && moveVector != Vector3.zero) lookVector = moveVector;

    }
    public void LookInput(InputAction.CallbackContext context)
    {
        rStickInput = context.ReadValue<Vector2>();
        float rStickDistance = Vector2.Distance(Vector2.zero, rStickInput); //grab 2d vector of the sticks/keyboard
        if (rStickDistance <= stickSensitivity) { return; }
        if (lookIdleCoroutine != null) { StopCoroutine(lookIdleCoroutine); lookIdleCoroutine = null; } //handles interupting the the idle timer for aiming
        lookVector = new Vector3(rStickInput.x, 0f, rStickInput.y);
        isIdleAim = false;
    }

    public void LookIdle(InputAction.CallbackContext context)
    {
        lookIdleCoroutine = StartCoroutine(AimIdleTimer());
    }

    private bool IsGamepadScheme()
    {
        // Handles custom scheme names like "Keyboard&Mouse" / "Gamepad"
        // If you used different names, adjust these strings.
        //return pInput != null && pInput.currentControlScheme == "Gamepad";
        return false;
    }
    #endregion

    #region Timers
    IEnumerator AimIdleTimer()
    {
        yield return new WaitForSeconds(lookIdleTimer);
        isIdleAim = true;
    }
    #endregion

    #region Debug

    void drawlines()
    {
        Debug.DrawLine(transform.position + moveVector, transform.position + moveVector + transform.up * debugLineLength, Color.red);
        Debug.DrawLine(transform.position + lookVector, transform.position + lookVector + transform.up * debugLineLength, Color.green);
    }


    #endregion
}

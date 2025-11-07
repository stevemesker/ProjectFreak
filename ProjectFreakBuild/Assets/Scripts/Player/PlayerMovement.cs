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

    private RaycastHit slopeHit;

    [SerializeField, Range(0f, 50f), Tooltip("maximum angle the floor can be to move up it")]
    float maxSlopeAngle = 30f;

    /////////////////////////////////////////////////
    [Header("RolL Variables")]
    /////////////////////////////////////////////////

    [SerializeField, Range(0f, 100f), Tooltip("How strong the roll will be")]
    float RollStrength;
    [SerializeField, Range(0f, 3f), Tooltip("how long the player must wait after executing a roll to do it again")]
    float RollCooldown;
    private Coroutine rollCooldownTimer;

    /////////////////////////////////////////////////
    [Header("Idle Variables")]
    /////////////////////////////////////////////////

    [SerializeField, Range(0f, 1f), Tooltip("How long the Player must stand idle to be considered 'Standing Still'")]
    float standTimerLength;
    public bool isStanding;
    private Coroutine idleTimer;

    Rigidbody rb;
    

    //--

    /////////////////////////////////////////////////
    [Header("Rotation Code")]
    /////////////////////////////////////////////////
    //--
    
    private PlayerInput pInput;
    [SerializeField] private Vector2 lStickInput;

    [SerializeField] private Vector3 moveVector; //handles the direction the player should move
    [SerializeField] Vector3 lookVector; //handles the direction the player should be looking at
    [SerializeField] float stickDistance; //holds how far the player has pushed the sticks

    //--

    /////////////////////////////////////////////////
    [Header("Interaction Code")]
    /////////////////////////////////////////////////
    //--
    [SerializeField] float grabDistance = 1; //how far something can be away from the player to grab it
    [SerializeField] public GameObject pCamera; //stores the player's camera
    [SerializeField] RaycastHit interactRay; //used for picking up objects and interacting
    [SerializeField] public GameObject targeter; //used for showing target location to player

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
            pCamera.GetComponent<cameraScript>().enabled = true;
        }
    }

    private void OnEnable()
    {
        pInput.Enable();

        pInput.Player.Move.performed += MoveInput;
        pInput.Player.Move.canceled += MoveInput;
        pInput.Player.Move.canceled += MoveCancel;

        pInput.Player.Interact.performed += RollInput;

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
        if (OnDebugLines) drawlines(); //debud Delete Later
        
        stickDistance = Vector2.Distance(Vector2.zero, lStickInput); //grab 2d vector of the sticks/keyboard

        //turning//
        moveVector = facingDirection(); //finds forward relative to camera angle
        lookVector = new Vector3(moveVector.x, 0f, moveVector.z); //find the direction the character will be facing
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookVector), Time.fixedDeltaTime * lookSpeed*stickDistance); //lerps the player rotation to the desired rotation
                                                                                                                                                       //turning//

        
        //placerTargetLocation = Player.player.FindSnapPoint();

        //Debug.DrawLine(transform.position, moveVector + GetSlopeMoveDirection().normalized + transform.position, Color.blue);
        //Debug.DrawLine(transform.position, GetSlopeMoveDirection().normalized + transform.position, Color.green);
        //Debug.DrawLine(transform.position + transform.forward * grabDistance + transform.up, transform.position + transform.up * -1 + transform.forward, Color.yellow);

        //velocity and displacement//
        Vector3 currentVelocity = rb.velocity; //get current velocity to allow gravity
        Vector3 displacement; //used for uneven surfaces
        //velocity and displacement//

        if (stickDistance >= lookSensitivity)
        {
            Vector3 targetHorizontal;
            if (OnSlope())
            {
                displacement = GetSlopeMoveDirection().normalized;
                targetHorizontal = displacement * Speed;
                rb.velocity = targetHorizontal;
            }
            else
            {
                targetHorizontal = moveVector.normalized * Speed;

                // Extract current horizontal velocity
                Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

                // Blend toward target based on acceleration
                Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontal, (acceleration*stickDistance) * Time.fixedDeltaTime);

                // Combine with preserved vertical (Y) velocity
                rb.velocity = new Vector3(newHorizontal.x, currentVelocity.y, newHorizontal.z);
            }

        }

        else
        {
            Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, Vector3.zero, deceleration * Time.fixedDeltaTime);

            rb.velocity = new Vector3(newHorizontal.x, currentVelocity.y, newHorizontal.z);
            
        }
        
        if (OnDebugLines) drawlines();
        
    }

    public Vector3 facingDirection()
    {
        //find rotation of camera
        float camRot = pCamera.transform.eulerAngles.y;
        //get stick input location
        lStickInput = Vector2.ClampMagnitude(lStickInput, 1f);

        //offset stick location based on camera rotation
        Vector2 adjStick = new Vector2((lStickInput.x * Mathf.Cos(camRot * Mathf.Deg2Rad * -1f)) + (lStickInput.y * Mathf.Sin(camRot * Mathf.Deg2Rad)), (lStickInput.x * Mathf.Sin(camRot * Mathf.Deg2Rad * -1f)) + (lStickInput.y * Mathf.Cos(camRot * Mathf.Deg2Rad)));
        return (new Vector3(adjStick.x, 0f, adjStick.y));
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position + new Vector3(0f, .5f, 0f), Vector3.down, out slopeHit))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private void Roll()
    {
        //gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * RollStrength, ForceMode.Impulse);
        gameObject.GetComponent<Rigidbody>().AddForce(moveVector * RollStrength, ForceMode.Impulse);
        print("Roll Impuled");
        rollCooldownTimer = StartCoroutine( RollCooldownCountdown(RollCooldown));
    }

    private bool isFalling()
    {
        if (Physics.Raycast(transform.position + new Vector3(0f, .5f, 0f), Vector3.down, out slopeHit))
        {
            if (Vector3.Distance(slopeHit.point, transform.position) < .6f) return false;
            else return true;
        }
        return true;
    }
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(transform.forward, slopeHit.normal).normalized;
    }


    #endregion

    #region Input
    //INPUT CODE
    public void MoveInput(InputAction.CallbackContext context)
    {
        idleTimer = null;
        lStickInput = context.ReadValue<Vector2>();
        isStanding = false;
    }
    
    public void MoveCancel(InputAction.CallbackContext context)
    {
        //isStanding = true;
        if (idleTimer != null) return;
        idleTimer = StartCoroutine(Idle(standTimerLength));
    }

    public void RollInput(InputAction.CallbackContext context)
    {
        if (stickDistance > 0 && rollCooldownTimer == null)
        {
            print("Initiating rolling...");
            Roll();
        }
    }

    private IEnumerator RollCooldownCountdown(float time)
    {
        yield return new WaitForSeconds(time);
        rollCooldownTimer = null;
        print("Roll Reset");
    }

   private IEnumerator Idle(float time)
    {
        yield return new WaitForSeconds(time);
        isStanding = true;
        idleTimer = null;
    }


    #endregion

    #region Debug

    void drawlines()
    {
        Debug.DrawLine(transform.position, transform.position + transform.forward * debugLineLength, Color.green);
        Debug.DrawLine(transform.position, transform.position + GetSlopeMoveDirection() * debugLineLength, Color.blue);
    }


    #endregion
}

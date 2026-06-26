using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[SelectionBase]
public class cameraScript : MonoBehaviour
{

    public bool canRotate;
    public float rotationSpeed; //how fast the camera spins around the player
    public float refreshSpeed; //how long until the zoom snapping can move again
    public float stickYSensitivity; //how much the sticks need to move before they are registered
    public int snappingPointNumber; //number of devisions the camera will snap to when zooming
    public float snapPauseTime; //how long until 
    public float zoomSpeed = 10; //how quickly the camera zooms

    private PlayerInput pInput;
    [SerializeField] private Vector2 rStickInput;
    [SerializeField] private float stickDistance;

    [SerializeField] private bool click;

    [SerializeField] private float targetZoom;
    [SerializeField] private CinemachineFreeLook cam;
    [SerializeField] private GameObject camBrain;
    [SerializeField] private int zoomCounter;

    private void Awake()
    {
        pInput = new PlayerInput();
        camBrain = transform.GetChild(0).gameObject;
        cam = camBrain.GetComponent<CinemachineFreeLook>();
        targetZoom = 1f / snappingPointNumber;

        if (cam.Follow == null)
        {
            cam.Follow = Player.player.gameObject.transform;
            cam.LookAt = Player.player.camTarget.transform;
        }
    }

    private void OnEnable()
    {
        pInput.Enable();

        pInput.Player.Look.performed += ZoomInput;
        pInput.Player.Look.canceled += ZoomCanceled;

    }

    void Update()
    {
        cam.m_YAxis.Value = Mathf.Lerp(cam.m_YAxis.Value, zoomCounter * targetZoom, zoomSpeed * Time.deltaTime); //sets the progress of the zooming
        zoomCamera();

        if (canRotate)
        {
            cam.m_XAxis.Value += rotationSpeed * rStickInput.x *Time.deltaTime;
        }
    }

    private void zoomCamera()
    {
        //gameObject.transform.rotation = new Quaternion(transform.rotation.w, transform.rotation.x + rStickInput.x*cameraSpeed, 0,0);
        if (Mathf.Abs(rStickInput.y) >= stickYSensitivity)
        {
            if (click == false)
            {
                click = true;
                zoomSnap(rStickInput.y);
                StartCoroutine(snapTimer());
            }
        }
        else
        {
            click = false;
        }
    }

    public void ZoomInput(InputAction.CallbackContext context)
    {
        rStickInput = context.ReadValue<Vector2>();
        stickDistance = Vector2.Distance(Vector2.zero, rStickInput);

        
    }

    public void ZoomCanceled(InputAction.CallbackContext context)
    {
        rStickInput = context.ReadValue<Vector2>();
        stickDistance = Vector2.Distance(Vector2.zero, rStickInput);

        if (Mathf.Abs(rStickInput.y) <= stickYSensitivity)
        {
            click = false;
            StopCoroutine(snapTimer());
        }
    }

    private IEnumerator snapTimer()
    {
        yield return new WaitForSeconds(snapPauseTime);
        click = false;
    }

    public void zoomSnap(float direction)
    {
        if (direction < 0)
        {
            zoomCounter += 1;
            if (zoomCounter >= snappingPointNumber)
            {
                zoomCounter = snappingPointNumber-1;
            }
        }
        else if (direction > 0)
        {
            zoomCounter -= 1;
            if (zoomCounter < 0)
            {
                zoomCounter = 0;
            }
        }
    }
}

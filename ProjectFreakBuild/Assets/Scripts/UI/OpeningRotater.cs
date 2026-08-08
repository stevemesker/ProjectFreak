using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OpeningRotater : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float maxRotateDistance;
    [SerializeField] float RotateSpeed;

    [Header("Current State")]
    [SerializeField] float rotationGoal;
    private float currentRotation;

    [SerializeField] float smoothTime = 0.08f;
    private float rotationVelocity;

    private PlayerInput pInput;

    private void Awake()
    {
        pInput = new PlayerInput();
        currentRotation = transform.localEulerAngles.y;
    }

    private void OnEnable()
    {
        pInput.Enable();

        pInput.Player.Move.performed += UpdateRotation;
        pInput.Player.Move.canceled += UpdateRotation;
        pInput.Player.Look.performed += UpdateRotation;
        pInput.Player.Look.canceled += UpdateRotation;
    }

    // Update is called once per frame
    void Update()
    {
        currentRotation = Mathf.SmoothDampAngle(
        currentRotation,
        rotationGoal,
        ref rotationVelocity,
        smoothTime);

        transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
    }

    void UpdateRotation(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        rotationGoal = input.x * maxRotateDistance;
    }
}

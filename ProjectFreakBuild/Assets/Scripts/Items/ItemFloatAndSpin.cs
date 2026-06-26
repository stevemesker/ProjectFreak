using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemFloatAndSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 40f;      // Degrees per second

    [Header("Float Settings")]
    [SerializeField] private float floatSpeed = 3.25f;      // Wave speed
    [SerializeField] private float floatHeight = 0.25f;  // Amplitude

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        HandleSpin();
        HandleFloat();
    }

    private void HandleSpin()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void HandleFloat()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        Vector3 pos = transform.position;
        pos.y = newY;

        transform.position = pos;
    }
}

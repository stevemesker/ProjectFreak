using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHover : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far down to look for the floor, in meters. Should be longer than the Ride Height or the unit won't find the floor")]
    [SerializeField, Min(0f)] float _RayLength = 3f;

    [Tooltip("How high above the floor the unit floats, in meters")]
    [SerializeField, Min(0f)] float _RideHeight = 1.5f;

    [Tooltip("How hard the spring pushes the unit back to its ride height. Higher = stiffer and snappier")]
    [SerializeField, Min(0f)] float _RideSpringStrength = 100f;

    [Tooltip("How much the spring resists bouncing. Higher = settles faster with less bob")]
    [SerializeField, Min(0f)] float _RideSpringDamper = 6f;

    [Header("References")]
    [Tooltip("The unit's rigidbody. Grabbed automatically from this object if left empty")]
    [SerializeField] Rigidbody _RB;

    [Header("Debug")]
    [Tooltip("Draws the spring force as a yellow line in the scene view")]
    [SerializeField] bool _OnDebugDrawLines;

    [Header("Runtime Data")]
    [Tooltip("True if the floor was found under the unit this physics step (read only)")]
    [SerializeField] bool _IsGrounded;

    //local variables
    RaycastHit _rayHit;

    private void Awake()
    {
        //grabs the rigidbody if one wasn't assigned in the inspector
        if (_RB == null) _RB = GetComponent<Rigidbody>();
        if (_RB == null) { Debug.LogError($"Error! No Rigidbody found on {gameObject.name}, UnitHover can't float it", this); enabled = false; }
    }

    private void OnValidate()
    {
        //editor-only warning: if the ray is shorter than the ride height the unit can never find the floor it's trying to float above
        if (_RayLength <= _RideHeight) Debug.LogWarning($"Warning! Ray Length on {gameObject.name} should be longer than Ride Height or the unit won't float", this);
    }

    private void FixedUpdate()
    {
        //checks for the floor and pushes the unit to its ride height every physics step
        _IsGrounded = FindFloor();
        StandingForce();
    }

    #region Standing
    bool FindFloor()
    {
        //function that casts down from the unit and saves what it hit into _rayHit
        //QueryTriggerInteraction.Ignore means trigger volumes don't count as floor
        Vector3 downDir = transform.TransformDirection(Vector3.down);
        return Physics.Raycast(transform.position, downDir, out _rayHit, _RayLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    void StandingForce()
    {
        //function that acts like a spring holding the unit at its ride height above the floor
        if (_IsGrounded == false) return; //nothing under us, let gravity do its thing

        Vector3 rayDir = transform.TransformDirection(Vector3.down);

        //if we're standing on something that moves (like a platform), match its speed so we don't bounce off it
        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = _rayHit.rigidbody;
        if (hitBody != null) otherVel = hitBody.velocity;

        //how fast we're moving up/down compared to the thing we're standing on
        float rayDirVel = Vector3.Dot(rayDir, _RB.velocity);
        float otherDirVel = Vector3.Dot(rayDir, otherVel);
        float relVel = rayDirVel - otherDirVel;

        //spring math: push based on how far we are from the ride height, minus a damper so it doesn't wobble forever
        float heightOffset = _rayHit.distance - _RideHeight;
        float springForce = (heightOffset * _RideSpringStrength) - (relVel * _RideSpringDamper);

        if (_OnDebugDrawLines) Debug.DrawLine(transform.position, transform.position + (rayDir * springForce), Color.yellow);

        _RB.AddForce(rayDir * springForce);

        //push the thing we're standing on the opposite way (Newton's third law) so platforms react to our weight
        if (hitBody != null) hitBody.AddForceAtPosition(rayDir * -springForce, _rayHit.point);
    }
    #endregion

    #region Tools
    public bool IsGrounded()
    {
        //function other scripts (AI, animation, abilities) can use to check if the unit is over the floor
        return _IsGrounded;
    }
    #endregion
}

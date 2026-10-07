using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))] //Unity adds a LineRenderer automatically when this script is added
public class ShadeTail : MonoBehaviour
{
    //Placeholder tether tail: a line from the floor under the player, along the floor, then up into the shade's body.
    //The real tail mesh waits until the art style is locked in (see Shade Forms Plan)

    [Header("Settings")]
    [Tooltip("How thick the placeholder line is, in meters")]
    [SerializeField, Min(0.001f)] float _LineWidth = 0.08f;

    [Tooltip("How high above the floor the flat part of the line sits, in meters, so it doesn't flicker into the ground")]
    [SerializeField, Min(0f)] float _FloorOffset = 0.02f;

    [Header("References")]
    [Tooltip("Material for the placeholder line. Uses a plain default material if left empty")]
    [SerializeField] Material _LineMaterial;

    //local variables
    LineRenderer _line;
    UnitHover _playerHover; //the player's hover, its floor ray gives the tail's start point
    Transform _tailEnd; //where the tail meets the body (the art rig's tail connection point)
    Vector3[] _points = new Vector3[3]; //start on the floor, corner under the body, end in the body

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>(); //the prefab was made without one, add it now
        _line.positionCount = _points.Length;
        _line.useWorldSpace = true; //the points are world positions, not relative to the shade
        _line.startWidth = _LineWidth;
        _line.endWidth = _LineWidth;

        //Sprites/Default is a simple built-in shader that's always included in builds, so the line isn't pink without a material
        if (_LineMaterial != null) _line.material = _LineMaterial;
        else _line.material = new Material(Shader.Find("Sprites/Default"));

        _line.enabled = false; //hidden until Setup gives it something to connect
    }

    private void LateUpdate()
    {
        //LateUpdate runs after everything has moved this frame, so the line never lags a frame behind
        UpdateLine();
    }

    #region Setup
    public void Setup(UnitHover playerHover, Transform tailEnd)
    {
        //function TetheredShade calls once its art is spawned
        _playerHover = playerHover;
        _tailEnd = tailEnd;
        if (_playerHover == null) Debug.LogWarning($"Warning! No UnitHover on the player for {gameObject.name}'s tail, hiding the tail...", this);
        if (_tailEnd == null) _tailEnd = transform;
    }
    #endregion

    #region Line
    void UpdateLine()
    {
        //function that draws the L: along the floor from under the player, then straight up into the body
        //hidden while the player is in the air, since there's no floor point to start from
        if (_playerHover == null || _tailEnd == null || _playerHover.IsGrounded() == false)
        {
            _line.enabled = false;
            return;
        }

        Vector3 start = _playerHover.GetFloorHit().point + Vector3.up * _FloorOffset;
        Vector3 end = _tailEnd.position;
        Vector3 corner = new Vector3(end.x, start.y, end.z); //on the floor, right under the body

        _points[0] = start;
        _points[1] = corner;
        _points[2] = end;
        _line.SetPositions(_points);
        _line.enabled = true;
    }
    #endregion
}

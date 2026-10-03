using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    //The movement "engine". Turns a move direction and a look direction into physics forces.
    //It doesn't read any controls: drivers (PlayerInputDriver, and later the shade's AI driver) call SetMoveDirection / SetLookDirection.
    //Floating is handled separately by UnitHover.

    [Header("Debug Stuff for now")]
    [SerializeField] Vector3 goalVel;

    [Header("Stats")]
    public Rigidbody _RB;

    [Header("State Variables")]
    [SerializeField, Tooltip("When true, turn off the ability to move and turn the character")] bool isMovePaused;

    [Header("Locomotion")]
    [SerializeField] float maxSpeed = 8;
    [SerializeField, Tooltip("How fast the target speed ramps up toward max speed, in m/s per second")] float acceleration = 200;
    [SerializeField, Tooltip("Multiplies acceleration based on how the new direction compares to the current one. X axis: -1 = full reverse, 0 = 90 degree turn, 1 = same direction. Values above 1 make turning/reversing snappier")]
    AnimationCurve AccelerationFactorFromDot = new AnimationCurve(new Keyframe(-1, 2), new Keyframe(0, 1), new Keyframe(1, 1));
    [SerializeField, Tooltip("Cap on how hard the rigidbody can be pushed toward the target speed, in m/s per second")] float maxAccelForce = 150;
    [SerializeField, Tooltip("Multiplies the max force cap using the same -1 to 1 direction comparison as above. Higher at -1 lets the unit stop and turn around faster")]
    AnimationCurve MaxAccelerationForceFactorFromDot = new AnimationCurve(new Keyframe(-1, 2), new Keyframe(0, 1), new Keyframe(1, 1));
    [SerializeField] Vector3 forceScale;
    [SerializeField] float maxAccelForceFactor = 1;
    [SerializeField] float speedFactor = 1;
    [SerializeField, Sirenix.OdinInspector.ReadOnly, Tooltip("Temporary slowdown from actions like charging a weapon. 1 = normal speed, 0.5 = half speed. Set by SetMoveSpeedMultiplier (read only)")]
    float _MoveSpeedMultiplier = 1;
    [SerializeField, Tooltip("The direction the unit is trying to move, set by its driver (read only)")] public Vector3 m_UnitGoal;
    Vector3 m_GoalVel;
    Vector3 savedVel; //used when stopping the unit and restarting at the same speed is necessary, saved to this variable

    [Header("Turning")]
    [SerializeField, Tooltip("How fast the unit turns to face its look direction, in degrees per second")] float TurnSpeed;
    [SerializeField, Tooltip("The direction the unit is trying to face, set by its driver (read only)")] public Vector3 m_turnGoal;

    [Header("Dash")]
    [SerializeField] UnitDash _Dash;

    [Header("Debug")]
    [SerializeField] bool OnDebugDrawLines;
    [SerializeField] float lineLength = 2;

    [Header("State Machine Variables")]
    [SerializeField] bool _CanTurn = true;

    #region Initialize
    public void SetTurning(bool Active)
    {
        //function that disables turning without disabling other controls
        _CanTurn = Active;
    }

    private void Awake()
    {
        _RB = GetComponent<Rigidbody>();
    }
    #endregion

    private void FixedUpdate()
    {
        //the floating/standing spring force now lives in UnitHover so it keeps running no matter who is steering this unit
        MovementForce();
        Rotationforce();

        if (OnDebugDrawLines) DebugLineDraw();
    }


    void MovementForce()
    {
        if (isMovePaused) return;
        Vector3 unitVel = m_GoalVel.normalized;
        float velDot = Vector3.Dot(m_UnitGoal, unitVel);
        float accel = acceleration * AccelerationFactorFromDot.Evaluate(velDot);
        goalVel = m_UnitGoal * GetMaxSpeed();

        m_GoalVel = Vector3.MoveTowards(m_GoalVel, goalVel, accel*Time.fixedDeltaTime);

        //turn the speed difference into an acceleration (m/s per second) FIRST, then cap it
        //(this matches Toyful's controller. Capping before dividing put the cap in the wrong units)
        Vector3 neededAccel = (m_GoalVel - _RB.velocity) / Time.fixedDeltaTime;

        float maxAccel = maxAccelForce * MaxAccelerationForceFactorFromDot.Evaluate(velDot) * maxAccelForceFactor;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);
        _RB.AddForce(Vector3.Scale(neededAccel * _RB.mass, forceScale));
    }

    void Rotationforce()
    {
        if (isMovePaused) return;
        if (_CanTurn == false) return;
        if (m_turnGoal.sqrMagnitude < 0.0001f) return; //no look direction yet (LookRotation can't use a zero direction)
        Quaternion targetRotation =
        Quaternion.LookRotation(m_turnGoal);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                TurnSpeed * Time.fixedDeltaTime);
    }

    #region Driver Controls
    public void SetMoveDirection(Vector3 direction)
    {
        //function drivers (player input, AI) use to say which way to move. Length 0 to 1, where 1 is full speed
        direction.y = 0f; //movement is only ever sideways, UnitHover and gravity handle up and down
        m_UnitGoal = Vector3.ClampMagnitude(direction, 1f);
    }

    public void SetLookDirection(Vector3 direction)
    {
        //function drivers use to say which way to face. A zero direction is ignored so the unit keeps facing the way it was
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        m_turnGoal = direction.normalized;
    }

    public Vector3 GetMoveDirection()
    {
        //function for checking which way the unit is currently trying to move
        return m_UnitGoal;
    }

    public float GetMaxSpeed()
    {
        //function for checking the unit's top speed in meters per second (used by the AI driver so its steering matches)
        return maxSpeed * speedFactor * _MoveSpeedMultiplier;
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        //function for slowing the unit down for a while (like charging a weapon). 1 = normal speed. Whoever sets it must call ClearMoveSpeedMultiplier when done
        _MoveSpeedMultiplier = Mathf.Max(0f, multiplier); //no negative speeds
    }

    public void ClearMoveSpeedMultiplier()
    {
        //function that puts the unit back to normal speed
        _MoveSpeedMultiplier = 1f;
    }

    public void Dash()
    {
        //function drivers use to dash in the current move direction
        if (_Dash == null) { Debug.LogWarning($"Warning! No UnitDash assigned on {gameObject.name}, can't dash...", this); return; }
        _Dash.DashCharacter(m_UnitGoal);
    }
    #endregion

    #region Tools
    public void DeactivateMovement()
    {
        //function that turns off movement/rotation of the character for pausing purposes
        isMovePaused = true;
        savedVel = _RB.velocity;
        _RB.velocity = Vector3.zero;
    }
    public void ReactivateMovement()
    {
        //function that turns back on the movement/rotation of the character after it had been paused
        isMovePaused = false;
        _RB.velocity = savedVel;
    }

    void DebugLineDraw()
    {
        //forward vector
        Debug.DrawLine(transform.position, transform.forward * lineLength + transform.position, Color.blue);

        //movement direction vector
        Debug.DrawLine(transform.position, transform.position + m_UnitGoal * lineLength, Color.green);

        //rotation direction vector
        Debug.DrawLine(transform.position, transform.position + m_turnGoal * lineLength, Color.yellow);
    }
    #endregion
}

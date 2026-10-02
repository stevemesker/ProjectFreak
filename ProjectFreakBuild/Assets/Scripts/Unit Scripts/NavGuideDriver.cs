using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(NavMeshAgent))]
public class NavGuideDriver : MonoBehaviour, IUnitMover
{
    //AI driver for physics units (like the shade). A NavMeshAgent plans the route and steers around other agents,
    //but it never moves the unit: we read the direction it wants to go and feed it into CharacterMovement, so physics still does the moving.
    //Something else (a brain/AI script) decides where to go and calls SetDestination.

    [Header("Settings")]
    [Tooltip("How close to the destination the unit stops, in meters")]
    [SerializeField, Min(0f)] float _StoppingDistance = 1.5f;

    [Tooltip("How far below/around the unit to look for the NavMesh, in meters. Must be more than the unit's float height (UnitHover ride height)")]
    [SerializeField, Min(0.1f)] float _NavMeshSnapDistance = 3f;

    [Tooltip("Below this fraction of full speed, the unit stops turning to face where it's going (stops jitter when nearly stopped). 0 to 1")]
    [SerializeField, Range(0f, 1f)] float _FaceMoveThreshold = 0.1f;

    [Header("References")]
    [Tooltip("The movement engine this driver steers. Grabbed from this object if left empty")]
    [SerializeField] CharacterMovement _Movement;

    [InfoBox("Turn off the NavMeshAgent component on this prefab. This driver turns it on once there's a NavMesh, otherwise Unity warns that the agent can't find one", InfoMessageType.Warning, "IsAgentOnInPrefab")]
    [Header("Runtime Data")]
    [Tooltip("True while the agent is on the NavMesh and guiding (read only)")]
    [SerializeField, ReadOnly] bool _IsGuiding;

    [Tooltip("True while the unit is too far from any NavMesh to follow a path, like mid-air or knocked off the edge (read only)")]
    [SerializeField, ReadOnly] bool _IsOffNavMesh;

    //local variables
    NavMeshAgent _agent;
    DungeonFloorObject _waitingOnFloor; //the floor we're waiting on to finish loading, if any
    Vector3 _savedDestination; //remembered so the unit can carry on once it's able to
    bool _hasDestination;
    bool _hasStarted; //false until Start() runs, see OnEnable

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>(); //safety net, RequireComponent normally adds it in the editor
        _agent.enabled = false; //stay off until there's a NavMesh to stand on

        if (_Movement == null) _Movement = GetComponent<CharacterMovement>();
        if (_Movement == null) { Debug.LogError($"Error! No CharacterMovement found for the NavGuideDriver on {gameObject.name}", this); enabled = false; return; }

        //the agent only plans and steers, it must never move or turn the unit itself (physics does that)
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.stoppingDistance = _StoppingDistance;
    }

    private void Start()
    {
        //the first time we start, wait until Start() so the dungeon floor (if any) has set itself up in its Awake()
        _hasStarted = true;
        BeginGuidingOrWait();
    }

    private void OnEnable()
    {
        //this driver just got control (like switching the shade back to AI). Before the first Start() it waits for Start instead
        if (_hasStarted) BeginGuidingOrWait();
    }

    private void OnDisable()
    {
        //control moved somewhere else: stop listening, turn the agent off and stop the unit
        if (_waitingOnFloor != null) _waitingOnFloor.FloorReady -= OnFloorReady;
        _waitingOnFloor = null;

        if (_agent != null) _agent.enabled = false;
        _IsGuiding = false;
        if (_Movement != null) _Movement.SetMoveDirection(Vector3.zero); //facing is left alone
    }

    private void Update()
    {
        //every frame: keep the agent where the unit really is, then pass the agent's wanted direction to the movement engine
        if (_IsGuiding == false) return;

        //find the NavMesh under the floating unit. If there isn't one nearby (mid-air, knocked off an edge), wait until there is
        if (NavMeshTools.TryGetNavMeshPoint(transform.position, _NavMeshSnapDistance, out Vector3 groundPoint) == false)
        {
            _IsOffNavMesh = true;
            _Movement.SetMoveDirection(Vector3.zero);
            return;
        }

        if (_IsOffNavMesh)
        {
            //back on the NavMesh after being off it, teleport the agent here so it doesn't try to path from the old spot
            _IsOffNavMesh = false;
            _agent.Warp(groundPoint);
            if (_hasDestination) _agent.SetDestination(_savedDestination);
        }
        else _agent.nextPosition = groundPoint; //nextPosition is the agent's own idea of where it is. We keep it matched to the unit

        SteerMovement();
    }

    #region Initialize
    void BeginGuidingOrWait()
    {
        //function that starts guiding now, or waits for the dungeon floor's NavMesh if it's still being built
        if (_Movement == null) return;
        if (NavMeshTools.IsWaitingOnFloor(gameObject, out DungeonFloorObject floor))
        {
            _waitingOnFloor = floor;
            _waitingOnFloor.FloorReady += OnFloorReady; //"+=" subscribes our function to the floor's event
            return;
        }
        StartGuiding();
    }

    void OnFloorReady()
    {
        //called by the dungeon floor once its NavMesh is built
        _waitingOnFloor.FloorReady -= OnFloorReady;
        _waitingOnFloor = null;
        StartGuiding();
    }

    void StartGuiding()
    {
        //function that puts the agent on the NavMesh under the unit and starts guiding
        if (NavMeshTools.TryGetNavMeshPoint(transform.position, _NavMeshSnapDistance, out Vector3 groundPoint) == false)
        {
            Debug.LogWarning($"Warning! No NavMesh found within {_NavMeshSnapDistance}m of {gameObject.name}, its AI can't move it. If this scene was built by hand, it needs a baked NavMesh", this);
            return;
        }

        _agent.enabled = true;
        _agent.Warp(groundPoint); //Warp teleports the agent without it trying to walk there
        _agent.speed = _Movement.GetMaxSpeed(); //match the engine's top speed so the agent's steering and slowdown line up with how the unit really moves
        _IsGuiding = true;
        _IsOffNavMesh = false;

        if (_hasDestination) _agent.SetDestination(_savedDestination);
    }
    #endregion

    #region Steering
    void SteerMovement()
    {
        //function that turns the agent's wanted velocity into a move and look direction for the engine
        //desiredVelocity already includes the path's corners, slowing down near the end, and stepping around other agents
        Vector3 desired = _agent.desiredVelocity;
        desired.y = 0f;

        float speedFraction = 0f;
        if (_agent.speed > 0f) speedFraction = Mathf.Clamp01(desired.magnitude / _agent.speed); //1 = full speed, smaller near the destination

        if (speedFraction <= 0f)
        {
            _Movement.SetMoveDirection(Vector3.zero);
            return;
        }

        Vector3 direction = desired.normalized;
        _Movement.SetMoveDirection(direction * speedFraction);
        if (speedFraction >= _FaceMoveThreshold) _Movement.SetLookDirection(direction);
    }
    #endregion

    #region Driver Controls
    public void SetDestination(Vector3 destination)
    {
        //function a brain/AI script calls to tell the unit where to go
        _savedDestination = destination;
        _hasDestination = true;

        if (_IsGuiding == false || _IsOffNavMesh) return; //remembered, and used once guiding starts
        _agent.SetDestination(destination);
    }

    public void StopMoving()
    {
        //function that makes the unit stop and forget its destination
        _hasDestination = false;
        if (_IsGuiding == false) return; //we don't have control right now (like the player steering the shade), so leave the movement alone
        if (_IsOffNavMesh == false) _agent.ResetPath();
        _Movement.SetMoveDirection(Vector3.zero);
    }

    public bool HasArrived()
    {
        //function for checking if the unit made it to its destination (or has nowhere to go)
        if (_hasDestination == false) return true;
        if (_IsGuiding == false || _agent.pathPending) return false; //pathPending = the agent is still working out the route
        return _agent.remainingDistance <= _StoppingDistance;
    }

    public bool IsGuiding()
    {
        //function for checking if the agent is on the NavMesh and guiding the unit
        return _IsGuiding;
    }

    public bool IsActive()
    {
        //function for checking if the shade can take AI orders right now: guiding (so the player isn't driving it) and on the NavMesh
        return _IsGuiding && _IsOffNavMesh == false;
    }
    #endregion

    #region Tools
    bool IsAgentOnInPrefab()
    {
        //used by the inspector warning above. Only matters outside play mode, since this script turns the agent on itself during play
        if (Application.isPlaying) return false;
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        return agent != null && agent.enabled;
    }
    #endregion
}

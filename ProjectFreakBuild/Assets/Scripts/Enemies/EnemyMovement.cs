using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour, IKnockbackable, IUnitMover
{
    //Moves an enemy with a NavMeshAgent. Something else (a brain/AI script) decides WHERE to go and calls SetDestination.
    //Waits for the dungeon floor's NavMesh before moving, and handles knockback by sliding the agent along the NavMesh.

    [Header("Data")]
    [Tooltip("The enemy template this enemy reads its movement and knockback settings from")]
    [SerializeField] EnemySO _EnemyData;

    [Header("Settings")]
    [Tooltip("How far to look for a NavMesh to stand on when the enemy starts, in meters")]
    [SerializeField, Min(0.1f)] float _NavMeshSnapDistance = 3f;

    [InfoBox("Turn off the NavMeshAgent component on this prefab. This script turns it on once the floor's NavMesh is built, otherwise Unity warns that the agent can't find a NavMesh", InfoMessageType.Warning, "IsAgentOnInPrefab")]
    [Header("Runtime Data")]
    [Tooltip("True once the enemy is standing on a NavMesh and can move (read only)")]
    [SerializeField, ReadOnly] bool _IsActive;

    [Tooltip("True while a knockback slide is happening (read only)")]
    [SerializeField, ReadOnly] bool _IsKnockedBack;

    //local variables
    NavMeshAgent _agent;
    Coroutine _knockbackRoutine;
    DungeonFloorObject _waitingOnFloor; //the floor we're waiting on to finish loading, if any
    Vector3 _savedDestination; //remembered so the enemy can carry on after a knockback or once it activates
    bool _hasDestination;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>(); //safety net, RequireComponent normally adds it in the editor
        _agent.enabled = false; //stay off until there's a NavMesh to stand on

        if (_EnemyData == null) { Debug.LogError($"Error! No EnemySO assigned to EnemyMovement on {gameObject.name}", this); enabled = false; return; }
        ApplyMovementSettings();
    }

    private void Start()
    {
        //if this scene is a dungeon floor that's still loading, wait for its NavMesh before moving
        if (NavMeshTools.IsWaitingOnFloor(gameObject, out DungeonFloorObject floor))
        {
            _waitingOnFloor = floor;
            _waitingOnFloor.FloorReady += OnFloorReady; //"+=" subscribes our function to the floor's event
            return;
        }

        //no floor (hand-built scene with a pre-made NavMesh) or it's already done, start now
        ActivateMovement();
    }

    private void OnDisable()
    {
        //stop a knockback slide partway through so it doesn't resume oddly later
        if (_knockbackRoutine != null) StopCoroutine(_knockbackRoutine);
        _knockbackRoutine = null;
        _IsKnockedBack = false;
    }

    private void OnDestroy()
    {
        //"-=" unsubscribes, so the floor doesn't try to call us after we're gone
        if (_waitingOnFloor != null) _waitingOnFloor.FloorReady -= OnFloorReady;
    }

    #region Initialize
    void OnFloorReady()
    {
        //called by the dungeon floor once its NavMesh is built
        _waitingOnFloor.FloorReady -= OnFloorReady;
        _waitingOnFloor = null;
        ActivateMovement();
    }

    void ActivateMovement()
    {
        //function that puts the enemy onto the NavMesh and turns the agent on
        //finds the closest point on the NavMesh, so an enemy placed a little above the floor still snaps down
        if (NavMeshTools.TryGetNavMeshPoint(transform.position, _NavMeshSnapDistance, out Vector3 groundPoint) == false)
        {
            Debug.LogWarning($"Warning! No NavMesh found within {_NavMeshSnapDistance}m of {gameObject.name}, it won't move. If this scene was built by hand, it needs a baked NavMesh", this);
            return;
        }

        _agent.enabled = true;
        _agent.Warp(groundPoint); //Warp teleports the agent without it trying to walk there
        _IsActive = true;

        if (_hasDestination) _agent.SetDestination(_savedDestination);
    }

    void ApplyMovementSettings()
    {
        //copies the movement settings from the enemy template onto the agent. The template itself never changes
        _agent.speed = _EnemyData._MoveSpeed;
        _agent.acceleration = _EnemyData._Acceleration;
        _agent.angularSpeed = _EnemyData._TurnSpeed;
        _agent.stoppingDistance = _EnemyData._StoppingDistance;
    }
    #endregion

    #region Movement
    public void SetDestination(Vector3 destination)
    {
        //function a brain/AI script calls to tell the enemy where to go
        _savedDestination = destination;
        _hasDestination = true;

        if (_IsActive == false || _IsKnockedBack) return; //remembered, and used once it can move again
        _agent.SetDestination(destination);
    }

    public void StopMoving()
    {
        //function that makes the enemy stop where it is and forget its destination
        _hasDestination = false;
        if (_IsActive == false || _IsKnockedBack) return;
        _agent.ResetPath();
    }

    public bool HasArrived()
    {
        //function for checking if the enemy made it to its destination (or has nowhere to go)
        if (_hasDestination == false) return true;
        if (IsActive() == false || _agent.pathPending) return false; //pathPending = the agent is still working out the route
        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    public EnemySO GetEnemyData()
    {
        //function that gives this enemy's template (other scripts like the UnitBrain read their settings from it)
        return _EnemyData;
    }

    public bool IsActive()
    {
        //function for checking if the enemy can take orders right now: on a NavMesh and not mid-knockback
        return _IsActive && _IsKnockedBack == false;
    }
    #endregion

    #region Knockback Interface
    public void TakeKnockback(DamagePackage dmgPackage)
    {
        //called when this enemy gets hit (by EnemyDamagable). Pushes it away from whatever hit it
        if (dmgPackage == null || dmgPackage._KnockbackDistance <= 0f) return; //hit has no knockback
        if (dmgPackage._Source == null) { Debug.LogWarning($"Warning! Knockback on {gameObject.name} has no source to push away from, skipping knockback...", this); return; }

        Vector3 direction = transform.position - dmgPackage._Source.transform.position;
        direction.y = 0f; //only push sideways, never up or down
        if (direction.sqrMagnitude < 0.0001f) direction = -transform.forward; //source is right on top of us, push straight back

        ApplyKnockback(direction.normalized, dmgPackage._KnockbackDistance, dmgPackage._Entries);
    }

    void ApplyKnockback(Vector3 direction, float distance, List<DamageEntry> damageEntries)
    {
        //function that works out how far this enemy really gets pushed and starts the slide
        if (_IsActive == false || _agent.isOnNavMesh == false) return; //can't slide if we're not on a NavMesh yet
        if (_EnemyData._KnockbackImmune) return;

        //the size class decides how much of the knockback we take (Large takes half, Huge takes none, etc.)
        float multiplier = 1f;
        SizeClassRulesSO rules = GetSizeClassRules();
        if (rules != null) multiplier = rules.GetKnockbackMultiplier(_EnemyData._SizeClass, damageEntries);

        float finalDistance = distance * multiplier;
        if (finalDistance <= 0f) return;

        //nextPosition is where the agent is on the NavMesh, which is what NavMesh.Raycast needs
        Vector3 start = _agent.nextPosition;
        Vector3 end = start + direction * finalDistance;

        //NavMesh.Raycast stops at the edge of the NavMesh (walls, ledges), so enemies never get pushed into walls or off the map
        //todo: let enemies get knocked over edges into hazards like lava pits once hazards exist (see AI plan)
        if (NavMesh.Raycast(start, end, out NavMeshHit edgeHit, NavMesh.AllAreas)) end = edgeHit.position;

        if (_knockbackRoutine != null) StopCoroutine(_knockbackRoutine); //a new hit replaces a slide that's still going
        _knockbackRoutine = StartCoroutine(KnockbackRoutine(start, end));
    }

    IEnumerator KnockbackRoutine(Vector3 start, Vector3 end)
    {
        //function that slides the enemy from start to end following the knockback curve, then lets it carry on
        _IsKnockedBack = true;
        _agent.isStopped = true; //stop following its path while it's being pushed
        _agent.velocity = Vector3.zero;

        float elapsed = 0f;
        Vector3 lastPosition = start;

        while (elapsed < _EnemyData._KnockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _EnemyData._KnockbackDuration);
            Vector3 nextPosition = Vector3.Lerp(start, end, _EnemyData._KnockbackCurve.Evaluate(t));

            _agent.Move(nextPosition - lastPosition); //Move shifts the agent but keeps it on the NavMesh
            lastPosition = nextPosition;
            yield return null;
        }

        _agent.isStopped = false;
        _IsKnockedBack = false;
        _knockbackRoutine = null;

        if (_hasDestination) _agent.SetDestination(_savedDestination); //carry on to wherever we were going
    }
    #endregion

    #region Tools
    SizeClassRulesSO GetSizeClassRules()
    {
        //function that gets the shared size class rules from the Game Manager
        if (GameManager._GameManager == null) { Debug.LogWarning($"Warning! No Game Manager found for {gameObject.name}, using full knockback...", this); return null; }
        return GameManager._GameManager.GetSizeClassRules();
    }

    bool IsAgentOnInPrefab()
    {
        //used by the inspector warning above. Only matters outside play mode, since this script turns the agent on itself during play
        if (Application.isPlaying) return false;
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        return agent != null && agent.enabled;
    }
    #endregion

    #region Test Tools
    [Button("Test Knockback")]
    void TestKnockback(float distance = 3f)
    {
        //editor button (play mode only) that knocks this enemy away from the player
        if (Application.isPlaying == false || Player.player == null) return;
        Vector3 direction = transform.position - Player.player.transform.position;
        direction.y = 0f;
        ApplyKnockback(direction.normalized, distance, null);
    }
    #endregion
}

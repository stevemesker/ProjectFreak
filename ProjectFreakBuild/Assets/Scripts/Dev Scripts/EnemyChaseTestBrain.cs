using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseTestBrain : MonoBehaviour
{
    //temp: a stand-in "brain" that chases the player so enemy movement can be tested.
    //Gets replaced by the real AI decision layer (step 7 of the AI plan)

    [Header("Settings")]
    [Tooltip("How often the enemy updates where the player is, in seconds")]
    [SerializeField, Min(0.05f)] float _RepathInterval = 0.25f;

    [Tooltip("How close the player has to be before the enemy chases, in meters. Further than this and it stops")]
    [SerializeField, Min(0f)] float _ChaseRange = 20f;

    [Header("References")]
    [Tooltip("The movement script to drive. Grabbed from this object if left empty")]
    [SerializeField] EnemyMovement _Movement;

    //local variables
    Coroutine _chaseRoutine;

    private void Awake()
    {
        if (_Movement == null) _Movement = GetComponent<EnemyMovement>();
        if (_Movement == null) { Debug.LogError($"Error! No EnemyMovement found for the chase test brain on {gameObject.name}", this); enabled = false; }
    }

    private void OnEnable()
    {
        //starts the chase loop when the brain turns on
        _chaseRoutine = StartCoroutine(ChaseLoop());
    }

    private void OnDisable()
    {
        //stops the chase loop when the brain turns off
        if (_chaseRoutine != null) StopCoroutine(_chaseRoutine);
        _chaseRoutine = null;
    }

    #region Chasing
    IEnumerator ChaseLoop()
    {
        //function that keeps pointing the enemy at the player while they're in range
        WaitForSeconds wait = new WaitForSeconds(_RepathInterval); //made once and reused, instead of a new one every loop

        while (true)
        {
            if (Player.player != null)
            {
                float distance = Vector3.Distance(transform.position, Player.player.transform.position);
                if (distance <= _ChaseRange) _Movement.SetDestination(Player.player.transform.position);
                else _Movement.StopMoving();
            }
            yield return wait;
        }
    }
    #endregion
}

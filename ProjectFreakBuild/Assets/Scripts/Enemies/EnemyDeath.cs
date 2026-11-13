using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Bare-bones enemy death. At 0 health (or when something calls Kill, like a self-destruct) it turns the enemy's
/// AI, movement, abilities and colliders off, takes it off the unit list so nothing keeps targeting the corpse,
/// then despawns it after the EnemySO's Death delay. Loot and XP come with the death and loot pass.
/// See Enemy Movement > EnemyDeath in the GDD.
///
////////////////////////////////////////////////
public class EnemyDeath : MonoBehaviour, IKillable
{
    [Header("Events")]
    [Tooltip("Runs the moment this enemy dies. Hook up death effects or sounds here until death animations exist")]
    public UnityEvent _OnDied;

    [Header("Runtime Data")]
    [Tooltip("True once this enemy has died (read only)")]
    [SerializeField, ReadOnly] bool _IsDead;

    [Tooltip("True if another unit made this one during play, like a scarab hatched by a Living Hive. Spawned units won't drop loot (read only)")]
    [SerializeField, ReadOnly] bool _WasSpawned;

    [Tooltip("The unit that spawned this one, if any (read only)")]
    [SerializeField, ReadOnly] GameObject _SpawnedBy;

    //local variables
    EnemySO _enemyData;
    Coroutine _despawnRoutine;

    public event System.Action<EnemyDeath> Died; //C# event for code that needs to know (like a hive counting its scarabs). "+=" to listen

    private void Awake()
    {
        if (TryGetComponent(out EnemyMovement movement)) _enemyData = movement.GetEnemyData();
    }

    #region Death
    public void Kill()
    {
        //function that kills this enemy. Called by EnemyStats at 0 health, or directly by things like a self-destruct
        if (_IsDead) return; //can only die once, even if several hits land on the same frame
        _IsDead = true;

        TurnOffUnit();
        _OnDied?.Invoke();
        Died?.Invoke(this);

        _despawnRoutine = StartCoroutine(DespawnRoutine());
    }

    void TurnOffUnit()
    {
        //function that switches off everything that makes the enemy act or get hit, so the corpse just sits there
        //the brain goes first so it can't give one last order
        if (TryGetComponent(out UnitBrain brain)) brain.enabled = false;
        if (TryGetComponent(out UnitTargeting targeting)) targeting.enabled = false;
        if (TryGetComponent(out EnemyStagger stagger)) stagger.enabled = false;

        if (TryGetComponent(out AbilityInterpreter abilities))
        {
            abilities.InterruptAbility(); //stops an ability partway through
            abilities.enabled = false;
        }

        if (TryGetComponent(out EnemyMovement movement))
        {
            movement.StopMoving();
            movement.enabled = false; //its OnDisable stops any knockback or stun still going
        }
        if (TryGetComponent(out NavMeshAgent agent)) agent.enabled = false; //so living units don't steer around the corpse

        //every collider on the enemy and its children, so weapons and projectiles pass through the corpse
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

        //todo: turn off status effects here once UnitStatus exists (Status effect system card)

        //last: turning UnitTeam off takes it out of the UnitRegistry, and UnitTargeting drops targets that are turned off
        if (TryGetComponent(out UnitTeam team)) team.enabled = false;
    }

    IEnumerator DespawnRoutine()
    {
        //function that waits the EnemySO's despawn delay, shrinking the body if it's set to (stand-in until death animations), then removes it
        float delay = _enemyData != null ? _enemyData._DespawnDelay : 0f;
        bool shrink = _enemyData != null && _enemyData._ShrinkOnDeath && delay > 0f;
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            if (shrink) transform.localScale = startScale * _enemyData._ShrinkCurve.Evaluate(Mathf.Clamp01(elapsed / delay));
            yield return null; //wait one frame
        }

        _despawnRoutine = null;
        Destroy(gameObject); //todo: pooled units (hatched scarabs) go back to their pool instead, once pooling is built (Living Hive card)
    }
    #endregion

    #region Spawning
    public void MarkAsSpawned(GameObject spawnedBy)
    {
        //function a spawner unit calls on the units it makes during play, so the loot pass knows to skip them
        _WasSpawned = true;
        _SpawnedBy = spawnedBy;
    }

    public bool WasSpawned()
    {
        //function for checking if another unit made this one during play
        return _WasSpawned;
    }
    #endregion

    #region Killable Interface
    public bool IsDead()
    {
        //function for checking if this enemy has died
        return _IsDead;
    }
    #endregion

    #region Test Tools
    [Button("Test Kill (Play Mode)")]
    void TestKill()
    {
        //editor button that kills this enemy right away, to check the death and despawn
        if (Application.isPlaying == false) return;
        Kill();
    }
    #endregion
}

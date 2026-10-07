using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(UnitTeam))]
public class UnitTargeting : MonoBehaviour, IAggroReceiver
{
    //Picks which hostile unit this unit goes after. It never moves anything, brains just ask it for GetTarget().
    //Target rules (from the AI plan), in order. A higher group always wins, no matter the distance:
    //  1) Attacker    - anything that hit this unit recently (like the shade jumping in)
    //  2) Normal      - hostile units it can see
    //  3) Vacant Body - the player's empty body while the player controls a shade. Only if nothing else is around
    //Inside a group, closer and higher priority units score better.

    [Header("Settings")]
    [Tooltip("How far away this unit notices hostile units, in meters")]
    [SerializeField, Min(0f)] float _DetectionRange = 15f;

    [Tooltip("How far its current target (or an attacker) can get before it gives up on them, in meters. Bigger than the detection range so it doesn't drop a target right at the edge")]
    [SerializeField, Min(0f)] float _LoseTargetRange = 25f;

    [Tooltip("How long a hit keeps the attacker as its top priority, in seconds. Each new hit restarts the timer")]
    [SerializeField, Min(0f)] float _AggroDuration = 5f;

    [Tooltip("How often it rechecks who to target, in seconds. Getting hit always rechecks right away")]
    [SerializeField, Min(0.05f)] float _RetargetInterval = 0.25f;

    [Tooltip("How much better a new target in the same group has to score before it switches (scores run about 0 to 1). Stops it flip-flopping between two targets")]
    [SerializeField, Range(0f, 1f)] float _SwitchMargin = 0.2f;

    [Tooltip("Does it have to see a unit (nothing solid in the way) to notice it? Its current target and anything that hit it are tracked either way")]
    [SerializeField] bool _NeedsLineOfSight = true;

    [ShowIf("_NeedsLineOfSight")]
    [Tooltip("Height of the eyes above the unit's position, in meters. Sight checks go from these eyes to the same height on the target")]
    [SerializeField] float _EyeHeight = 1f;

    [ShowIf("_NeedsLineOfSight")]
    [Tooltip("Layers that block sight. Leave out Units, Projectile and Item so other units and bullets don't block the view")]
    [SerializeField] LayerMask _SightBlockers = ~((1 << 6) | (1 << 7) | (1 << 8)); //everything except Projectile (6), Item (7) and Units (8). "~" flips the bits, so this means "all layers except these"

    [Header("Runtime Data")]
    [Tooltip("Who it's going after right now (read only)")]
    [SerializeField, ReadOnly] UnitTeam _CurrentTarget;

    [Tooltip("Which priority group the current target is in (read only)")]
    [SerializeField, ReadOnly] UnitType.TargetTier _CurrentTier;

    [Tooltip("Units that hit it recently, and when (read only)")]
    [SerializeField, ReadOnly] List<AggroEntry> _Attackers = new List<AggroEntry>();

    //Lets other scripts (like a brain) react the moment the target changes. Sends the new target, which can be null
    public event System.Action<UnitTeam> TargetChanged;

    //local variables
    UnitTeam _myTeam;
    Coroutine _retargetRoutine;

    private void Awake()
    {
        _myTeam = GetComponent<UnitTeam>();
        if (_myTeam == null) { Debug.LogError($"Error! No UnitTeam found for the UnitTargeting on {gameObject.name}", this); enabled = false; }
    }

    private void OnEnable()
    {
        //starts checking for targets
        _retargetRoutine = StartCoroutine(RetargetLoop());
    }

    private void OnDisable()
    {
        //stops checking and forgets everything, so it starts fresh if it turns back on
        if (_retargetRoutine != null) StopCoroutine(_retargetRoutine);
        _retargetRoutine = null;
        _Attackers.Clear();
        SetTarget(null, UnitType.TargetTier.None);
    }

    private void OnValidate()
    {
        //keeps the lose range from being smaller than the detection range, which would make it drop targets the moment it finds them
        if (_LoseTargetRange < _DetectionRange) _LoseTargetRange = _DetectionRange;
    }

    #region Targeting
    IEnumerator RetargetLoop()
    {
        //function that rechecks the target on a timer
        WaitForSeconds wait = new WaitForSeconds(_RetargetInterval); //made once and reused, instead of a new one every loop
        while (true)
        {
            UpdateTarget();
            yield return wait;
        }
    }

    void UpdateTarget()
    {
        //function that scores every hostile unit and picks the best one, following the target rules at the top
        ClearOldAggro();

        UnitTeam bestTarget = null;
        UnitType.TargetTier bestTier = UnitType.TargetTier.None;
        float bestScore = 0f;

        //how the current target scores right now, used for the stickiness check below
        UnitType.TargetTier currentTier = UnitType.TargetTier.None;
        float currentScore = 0f;

        List<UnitTeam> units = UnitRegistry.GetUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam unit = units[i];
            if (unit == null || _myTeam.IsHostileTo(unit) == false) continue;

            UnitType.TargetTier tier = GetTier(unit);
            float score = ScoreTarget(unit, tier);
            if (score <= 0f) continue; //out of range or can't be seen

            if (unit == _CurrentTarget) { currentTier = tier; currentScore = score; }

            //a higher group always wins. In the same group, the higher score wins
            bool betterGroup = tier > bestTier; //enums can be compared like numbers, in the order they're written
            bool sameGroupBetterScore = tier == bestTier && score > bestScore;
            if (betterGroup || sameGroupBetterScore)
            {
                bestTarget = unit;
                bestTier = tier;
                bestScore = score;
            }
        }

        //stickiness: keep the current target unless the new one is in a higher group or clearly better
        bool currentStillValid = _CurrentTarget != null && currentScore > 0f;
        if (currentStillValid && bestTarget != _CurrentTarget && currentTier == bestTier && bestScore < currentScore + _SwitchMargin)
        {
            bestTarget = _CurrentTarget;
            bestTier = currentTier;
        }

        SetTarget(bestTarget, bestTier);
    }

    UnitType.TargetTier GetTier(UnitTeam unit)
    {
        //function that works out which priority group a unit falls in
        if (HasAggroFrom(unit)) return UnitType.TargetTier.Attacker;
        if (unit.IsVacantBody()) return UnitType.TargetTier.VacantBody;
        return UnitType.TargetTier.Normal;
    }

    float ScoreTarget(UnitTeam unit, UnitType.TargetTier tier)
    {
        //function that scores a unit from about 0 to 1 (more with a high target priority). 0 means it can't be targeted right now
        bool isKnown = unit == _CurrentTarget || tier == UnitType.TargetTier.Attacker; //units we're already tracking

        //known units are kept out to the lose range, new ones have to come within the detection range
        float range = _DetectionRange;
        if (isKnown) range = _LoseTargetRange;
        if (range <= 0f) return 0f;

        float distance = Vector3.Distance(transform.position, unit.transform.position);
        if (distance > range) return 0f;

        //new units have to be seen first. Known ones don't, so stepping behind a pillar doesn't make it forget you
        if (isKnown == false && _NeedsLineOfSight && CanSee(unit) == false) return 0f;

        float closeness = 1f - (distance / range); //1 = right next to us, 0 = at the edge of the range
        closeness = Mathf.Max(closeness, 0.01f); //something right at the edge still counts as a valid target
        return closeness * unit.GetTargetPriority();
    }

    bool CanSee(UnitTeam unit)
    {
        //function that checks for anything solid between our eyes and the target
        Vector3 eyes = transform.position + Vector3.up * _EyeHeight;
        Vector3 targetEyes = unit.transform.position + Vector3.up * _EyeHeight;

        //Linecast is a raycast between two points. True means something on the blocker layers is in the way
        return Physics.Linecast(eyes, targetEyes, _SightBlockers, QueryTriggerInteraction.Ignore) == false;
    }

    void SetTarget(UnitTeam newTarget, UnitType.TargetTier tier)
    {
        //function that changes the target and lets listeners know if it's a different one
        _CurrentTier = tier;
        if (newTarget == _CurrentTarget) return;
        _CurrentTarget = newTarget;
        TargetChanged?.Invoke(_CurrentTarget); //"?." only calls it if something is listening
    }
    #endregion

    #region Aggro
    bool HasAggroFrom(UnitTeam unit)
    {
        //function for checking if this unit hit us recently
        for (int i = 0; i < _Attackers.Count; i++)
        {
            if (_Attackers[i]._Attacker == unit) return true;
        }
        return false;
    }

    void ClearOldAggro()
    {
        //function that forgets attackers whose aggro ran out, or who are gone
        //loops backwards so removing an entry doesn't skip the next one
        for (int i = _Attackers.Count - 1; i >= 0; i--)
        {
            AggroEntry entry = _Attackers[i];
            bool isGone = entry._Attacker == null || entry._Attacker.isActiveAndEnabled == false;
            bool timedOut = Time.time - entry._LastHitTime > _AggroDuration;
            if (isGone || timedOut) _Attackers.RemoveAt(i);
        }
    }

    void AddAttacker(UnitTeam attacker)
    {
        //function that remembers who hit us (or restarts their timer) and rechecks the target right away
        if (_myTeam == null || attacker == null) return;
        if (_myTeam.IsHostileTo(attacker) == false) return; //no aggro from friendly fire

        bool found = false;
        for (int i = 0; i < _Attackers.Count; i++)
        {
            if (_Attackers[i]._Attacker != attacker) continue;
            _Attackers[i]._LastHitTime = Time.time;
            found = true;
        }
        if (found == false) _Attackers.Add(new AggroEntry { _Attacker = attacker, _LastHitTime = Time.time }); //the { } fills in the new entry's fields as it's made

        UpdateTarget(); //react on the hit instead of waiting for the next timer check
    }
    #endregion

    #region Aggro Interface
    public void AddAggro(DamagePackage dmgPackage)
    {
        //called when this unit gets hit (by EnemyDamagable). Remembers whoever caused the hit
        if (dmgPackage == null || dmgPackage._Source == null) return; //hit has no source to blame
        if (isActiveAndEnabled == false) return;

        //GetComponentInParent also checks the object itself, then its parents, in case the source is a child object like a weapon
        UnitTeam attacker = dmgPackage._Source.GetComponentInParent<UnitTeam>();
        if (attacker == null) return; //hit came from something that isn't a unit, like a trap
        AddAttacker(attacker);
    }
    #endregion

    #region Driver Controls
    public UnitTeam GetTarget()
    {
        //function a brain calls to find out who to go after. Can be null when there's no one
        if (_CurrentTarget != null && _CurrentTarget.isActiveAndEnabled == false) return null; //target was turned off since the last check
        return _CurrentTarget;
    }

    public bool HasTarget()
    {
        //function for checking if it has someone to go after
        return GetTarget() != null;
    }

    public UnitType.TargetTier GetTargetTier()
    {
        //function that says which priority group the current target is in (like Vacant Body, so cowards can flee from it instead)
        return _CurrentTier;
    }
    #endregion

    #region Debugging
    private void OnDrawGizmosSelected()
    {
        //shows the detection range (yellow), the lose range (gray) and a line to the current target (red) when this object is selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _DetectionRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, _LoseTargetRange);

        if (_CurrentTarget == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * _EyeHeight, _CurrentTarget.transform.position + Vector3.up * _EyeHeight);
    }
    #endregion

    #region Test Tools
    [Button("Test Aggro From Shade")]
    void TestAggroFromShade()
    {
        //editor button (play mode only) that acts like the shade just hit this unit
        if (Application.isPlaying == false) return;
        if (ReleasedShade._Released == null) { Debug.LogWarning("Warning! No released shade out to test with, skipping..."); return; }
        AddAttacker(ReleasedShade._Released.GetComponent<UnitTeam>());
    }

    [Button("Test Aggro From Player")]
    void TestAggroFromPlayer()
    {
        //editor button (play mode only) that acts like the player's body just hit this unit
        if (Application.isPlaying == false) return;
        if (Player.player == null) { Debug.LogWarning("Warning! No player to test with, skipping..."); return; }
        AddAttacker(Player.player.GetComponent<UnitTeam>());
    }
    #endregion
}

[System.Serializable]
public class AggroEntry
{
    [Tooltip("The unit that hit us")]
    public UnitTeam _Attacker;

    [Tooltip("When it last hit us, in seconds since the game started")]
    public float _LastHitTime;
}

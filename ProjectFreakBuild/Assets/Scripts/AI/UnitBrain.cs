using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(UnitTargeting))]
public class UnitBrain : MonoBehaviour
{
    //The AI's decision maker, for enemies and shades. A few times a second it asks every action in _Actions
    //"how good an idea are you right now?" (a score), and runs the best one. Personality values feed into the scores,
    //so the same actions can play like a coward or a berserker.
    //It never moves the unit itself: actions give orders to the mover (EnemyMovement or NavGuideDriver) through IUnitMover.

    [Header("Data")]
    [InfoBox("This enemy's personality comes from its EnemySO (AI foldout)", "UsesEnemyData")]
    [HideIf("UsesEnemyData")]
    [Tooltip("This unit's personality: a preset plus any overrides. Enemies set this on their EnemySO instead")]
    [SerializeField] PersonalitySetup _PersonalitySetup = new PersonalitySetup();

    [Tooltip("What this unit can do. Each think, the highest scoring action runs. Left empty, it gets the default set for its role (enemy or shade) when the game starts")]
    [SerializeReference] List<AIAction> _Actions = new List<AIAction>();

    [Header("Settings")]
    [Tooltip("How often it rethinks what to do when it's near the camera, in seconds")]
    [SerializeField, Min(0.05f)] float _ThinkInterval = 0.2f;

    [Tooltip("How often it rethinks when it's far from the camera, in seconds. Far away units think less often to save performance. Shades always use the normal interval")]
    [SerializeField, Min(0.05f)] float _FarThinkInterval = 0.6f;

    [Tooltip("Closer to the camera's target than this, it thinks at the normal interval, in meters")]
    [SerializeField, Min(0f)] float _NearDistance = 15f;

    [Tooltip("Farther than this, it thinks at the far interval. In between it blends, in meters")]
    [SerializeField, Min(0f)] float _FarDistance = 35f;

    [Tooltip("Popcorn rank enemies multiply their think interval by this. 1.5 = they think a third less often. There are lots of them and they don't need to be sharp")]
    [SerializeField, Min(1f)] float _PopcornThinkMultiplier = 1.5f;

    [Tooltip("While it's still committed to an action (see Commit Time), another action has to score this much higher to take over. Stops it twitching between two choices")]
    [SerializeField, Range(0f, 1f)] float _InterruptMargin = 0.15f;

    [Tooltip("How close it gets to its target to fight, in meters. Used by Chase and Engage")]
    [SerializeField, Min(0f)] float _FightRange = 2f; //temp: abilities will give real ranges once they're hooked in (ability overhaul)

    [Tooltip("Editor only. Writes the current action and target above the unit while playing. Shows in the Scene view, and in the Game view when its Gizmos button is on")]
    [SerializeField] bool _ShowDebugLabel = true;

    [ShowIf("_ShowDebugLabel")]
    [Tooltip("How high above the unit the debug label sits, in meters")]
    [SerializeField] float _DebugLabelHeight = 2.5f;

    [Header("References")]
    [Tooltip("Picks who to go after. Grabbed from this object if left empty")]
    [SerializeField] UnitTargeting _Targeting;

    [Header("Runtime Data")]
    [Tooltip("The personality it's using right now, built from the preset and overrides when the game starts. Every action's score uses these. You can tweak them during play to test, but changes are lost when play stops")]
    [SerializeField] AIPersonality _Personality = new AIPersonality();

    [Tooltip("The action running right now (read only)")]
    [SerializeField, ReadOnly] string _CurrentActionName = "None";

    [Tooltip("How often it's thinking right now, in seconds, after distance and rank (read only)")]
    [SerializeField, ReadOnly] float _CurrentThinkInterval;

    [Tooltip("True while it can't give orders, like mid-knockback, waiting on the NavMesh, or the player driving the shade (read only)")]
    [SerializeField, ReadOnly] bool _IsPaused;

    [Tooltip("What every action scored on the last think (read only)")]
    [SerializeField, ReadOnly] List<ActionScoreEntry> _Scores = new List<ActionScoreEntry>();

    //local variables
    IUnitMover _mover; //interfaces can't show in the inspector, so this is always found with GetComponent
    IUnitHealth _health;
    UnitTeam _team;
    Shade _shade; //only on shades, used to find the player to follow
    AIAction _currentAction;
    float _actionStartTime;
    Vector3 _homePosition;
    EnemySO _enemyData; //only on enemies, used for the rank
    float _nextThinkTime;

    private void Awake()
    {
        if (_Targeting == null) _Targeting = GetComponent<UnitTargeting>();
        _mover = GetComponent<IUnitMover>(); //GetComponent works with interfaces too, it finds whichever component uses it
        _health = GetComponent<IUnitHealth>();
        _team = GetComponent<UnitTeam>();
        _shade = GetComponent<Shade>();
        _homePosition = transform.position; //remembers where it started, so wandering stays near home
        if (TryGetComponent(out EnemyMovement enemyMovement)) _enemyData = enemyMovement.GetEnemyData();

        if (_mover == null) { Debug.LogError($"Error! No EnemyMovement or NavGuideDriver found for the UnitBrain on {gameObject.name}", this); enabled = false; return; }
        if (_Targeting == null) { Debug.LogError($"Error! No UnitTargeting found for the UnitBrain on {gameObject.name}", this); enabled = false; return; }

        ResetPersonality();
        if (_Actions.Count == 0) FillDefaultActions();
    }

    private void OnEnable()
    {
        //the first think happens after a random short delay, so a room full of enemies doesn't all think on the same frame
        _nextThinkTime = Time.time + Random.Range(0f, _ThinkInterval);
    }

    private void OnDisable()
    {
        //ends whatever it was doing (Update stops running on its own while disabled)
        if (_currentAction != null) _currentAction.End(this);
        _currentAction = null;
        _CurrentActionName = "None";
        if (_mover != null && _mover.IsActive()) _mover.StopMoving();
    }

    #region Thinking
    private void Update()
    {
        //thinks on a timer instead of every frame, which keeps lots of enemies cheap. The timer length changes with distance and rank
        if (Time.time < _nextThinkTime) return;
        _CurrentThinkInterval = GetThinkInterval();
        _nextThinkTime = Time.time + _CurrentThinkInterval;
        Think();
    }

    float GetThinkInterval()
    {
        //function that works out how long until the next think: slower when far from the camera, and for Popcorn enemies
        if (_shade != null) return _ThinkInterval; //shades are always close by and should feel sharp

        float interval = _ThinkInterval;
        Transform focus = GetCameraFocus();
        if (focus != null)
        {
            float distance = DistanceTo(focus.position);
            float farness = Mathf.InverseLerp(_NearDistance, _FarDistance, distance); //0 = near distance or closer, 1 = far distance or farther
            interval = Mathf.Lerp(_ThinkInterval, _FarThinkInterval, farness);
        }

        if (_enemyData != null && _enemyData._Rank == EnemyType.Rank.Popcorn) interval *= _PopcornThinkMultiplier;
        return interval;
    }

    Transform GetCameraFocus()
    {
        //function that finds what the camera is following (the player, or the shade while the player drives it)
        if (CameraManager._CamManager != null && CameraManager._CamManager._currentFollowTarget != null) return CameraManager._CamManager._currentFollowTarget.transform;
        if (Player.player != null) return Player.player.transform;
        return null;
    }

    void Think()
    {
        //function that scores every action, switches to a better one if it should, then runs the current one
        if (_mover.IsActive() == false) { Pause(); return; }
        _IsPaused = false;

        AIAction bestAction = null;
        float bestScore = 0f;
        float currentScore = 0f;

        ResizeScoreList();
        for (int i = 0; i < _Actions.Count; i++)
        {
            AIAction action = _Actions[i];
            if (action == null) { _Scores[i]._Action = "(empty slot)"; _Scores[i]._Score = 0f; continue; }

            float score = Mathf.Max(0f, action.Score(this)) * action._Weight;
            _Scores[i]._Action = action.GetActionName();
            _Scores[i]._Score = score;

            if (action == _currentAction) currentScore = score;
            if (score > bestScore)
            {
                bestAction = action;
                bestScore = score;
            }
        }

        if (bestAction != _currentAction && ShouldSwitch(bestScore, currentScore)) SwitchAction(bestAction);
        if (_currentAction != null) _currentAction.Tick(this);
    }

    bool ShouldSwitch(float bestScore, float currentScore)
    {
        //function that decides if a better scoring action is allowed to take over from the current one
        if (_currentAction == null || currentScore <= 0f) return true; //nothing running, or the current action can't continue
        if (_currentAction.IsFinished(this)) return true;

        //still committed: the new action has to win by a clear margin. After the commit time, any better score wins
        float margin = 0f;
        if (Time.time - _actionStartTime < _Personality._CommitTime) margin = _InterruptMargin;
        return bestScore > currentScore + margin;
    }

    void SwitchAction(AIAction newAction)
    {
        //function that ends the current action and starts the new one (or just stops, if there's nothing worth doing)
        if (_currentAction != null) _currentAction.End(this);

        _currentAction = newAction;
        _actionStartTime = Time.time;

        if (_currentAction == null)
        {
            _CurrentActionName = "None";
            _mover.StopMoving();
            return;
        }

        _CurrentActionName = _currentAction.GetActionName();
        _currentAction.Begin(this);
    }

    void Pause()
    {
        //function that drops the current action while the unit can't take orders. It picks fresh once it can again
        //it doesn't tell the mover to stop, so things like a knockback slide still carry on to where it was going
        if (_IsPaused) return;
        _IsPaused = true;
        if (_currentAction != null) _currentAction.End(this);
        _currentAction = null;
        _CurrentActionName = "Paused";
    }
    #endregion

    #region Action Info
    //functions actions use to read the situation and give orders

    public IUnitMover GetMover()
    {
        //the mover to give orders to
        return _mover;
    }

    public UnitTeam GetTarget()
    {
        //who it's going after, can be null
        return _Targeting.GetTarget();
    }

    public UnitType.TargetTier GetTargetTier()
    {
        //which priority group the target is in (like Vacant Body)
        return _Targeting.GetTargetTier();
    }

    public UnitTeam GetTeam()
    {
        //this unit's own team info
        return _team;
    }

    public AIPersonality GetPersonality()
    {
        //this unit's personality values
        return _Personality;
    }

    public void SetPersonality(AIPersonality personality)
    {
        //function for swapping in a different personality during play (like a shade losing control). Keeps its own copy
        if (personality == null) return;
        _Personality = personality.Copy();
    }

    public void ResetPersonality()
    {
        //function that goes back to the personality from its preset and overrides
        _Personality = GetPersonalitySetup().BuildPersonality();
    }

    public static void ReloadAllPersonalities()
    {
        //function that makes every active unit rebuild its personality from its preset and overrides.
        //Used by the Reload buttons after changing a preset during play
        List<UnitTeam> units = UnitRegistry.GetUnits();
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].TryGetComponent(out UnitBrain brain)) brain.ResetPersonality();
        }
    }

    public float GetFightRange()
    {
        //how close it gets to fight, in meters
        return _FightRange;
    }

    public float GetHealthPercent()
    {
        //how healthy it is, 0 to 1. Units without a health script count as full health
        if (_health == null) return 1f;
        return _health.GetHealthPercent();
    }

    public Vector3 GetHomePosition()
    {
        //where it started
        return _homePosition;
    }

    public Transform GetLeader()
    {
        //who it follows. Shades follow whoever summoned them, or the player. Enemies don't have a leader (yet)
        if (_shade == null) return null;
        if (_shade.PlayerRef != null) return _shade.PlayerRef.transform;
        if (Player.player != null) return Player.player.transform;
        return null;
    }

    public float DistanceTo(Vector3 point)
    {
        //flat distance to a point, ignoring height (so a floating shade measures the same as a walking enemy)
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }
    #endregion

    #region Initialize
    void FillDefaultActions()
    {
        //function that gives the unit the default action set for its role
        if (_team != null && _team._Role == UnitType.Role.Shade) FillShadeDefaults();
        else FillEnemyDefaults();
    }

    [Button("Fill Enemy Defaults")]
    void FillEnemyDefaults()
    {
        //editor button that replaces the action list with the standard enemy set
        _Actions = new List<AIAction>();
        _Actions.Add(new ChaseAction());
        _Actions.Add(new EngageAction());
        _Actions.Add(new FleeAction());
        _Actions.Add(new WanderAction());
    }

    [Button("Fill Shade Defaults")]
    void FillShadeDefaults()
    {
        //editor button that replaces the action list with the standard shade set
        _Actions = new List<AIAction>();
        _Actions.Add(new FollowLeaderAction());
        _Actions.Add(new ChaseAction());
        _Actions.Add(new EngageAction());
        _Actions.Add(new FleeAction());
        _Actions.Add(new WanderAction());
        _Actions.Add(new SeekFightAction());
    }
    #endregion

    #region Debugging
    private void OnValidate()
    {
        //keeps the far distance from being closer than the near distance
        if (_FarDistance < _NearDistance) _FarDistance = _NearDistance;
    }

#if UNITY_EDITOR
    //everything between #if UNITY_EDITOR and #endif only exists in the editor. The built game leaves it out completely
    static GUIStyle _debugLabelStyle;

    private void OnDrawGizmos()
    {
        //writes the current action (and target) above the unit while playing
        if (_ShowDebugLabel == false || Application.isPlaying == false) return;

        if (_debugLabelStyle == null)
        {
            //made once and shared by every brain, instead of a new one every frame
            _debugLabelStyle = new GUIStyle();
            _debugLabelStyle.fontStyle = FontStyle.Bold;
            _debugLabelStyle.alignment = TextAnchor.MiddleCenter;
        }
        _debugLabelStyle.normal.textColor = Color.white;
        if (_IsPaused) _debugLabelStyle.normal.textColor = Color.gray;

        string label = _CurrentActionName;
        UnitTeam target = null;
        if (_Targeting != null) target = _Targeting.GetTarget();
        if (target != null) label += " > " + target.name;

        UnityEditor.Handles.Label(transform.position + Vector3.up * _DebugLabelHeight, label, _debugLabelStyle); //Handles draws editor-only helpers like text in the scene
    }
#endif
    #endregion

    #region Test Tools
    [Button("Reload Personality")]
    void ReloadPersonalityButton()
    {
        //editor button (play mode only) that rebuilds this unit's personality, after changing its preset or overrides during play
        if (Application.isPlaying == false) return;
        ResetPersonality();
    }
    #endregion

    #region Tools
    PersonalitySetup GetPersonalitySetup()
    {
        //function that finds where this unit's personality is set: its EnemySO for enemies, otherwise the brain's own setup
        if (TryGetComponent(out EnemyMovement enemyMovement))
        {
            EnemySO enemyData = enemyMovement.GetEnemyData();
            if (enemyData != null && enemyData._Personality != null) return enemyData._Personality;
        }
        if (_PersonalitySetup == null) _PersonalitySetup = new PersonalitySetup(); //safety net so there's always something to build from
        return _PersonalitySetup;
    }

    bool UsesEnemyData()
    {
        //used by the inspector to hide the brain's own personality setup on enemies, since theirs is on the EnemySO
        return TryGetComponent(out EnemyMovement enemyMovement) && enemyMovement.GetEnemyData() != null;
    }

    void ResizeScoreList()
    {
        //function that keeps one score readout entry per action
        while (_Scores.Count < _Actions.Count) _Scores.Add(new ActionScoreEntry());
        while (_Scores.Count > _Actions.Count) _Scores.RemoveAt(_Scores.Count - 1);
    }
    #endregion
}

[System.Serializable]
public class ActionScoreEntry
{
    [Tooltip("The action's name")]
    public string _Action;

    [Tooltip("What it scored on the last think, after its weight")]
    public float _Score;
}

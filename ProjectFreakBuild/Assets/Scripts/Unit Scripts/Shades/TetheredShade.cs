using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody), typeof(UnitHover), typeof(ShadeTail))] //Unity adds these automatically when this script is added
public class TetheredShade : MonoBehaviour, IShadeForm
{
    //The shade in its tethered form: bound to the player by the tail and floating beside them.
    //It glides in a straight line to its slot (passing through the player instead of circling around) and only turns to face things.
    //No collider and no UnitTeam, so it can't bump into anything and enemies can't target it (see Shade Forms Plan)

    [Header("Settings")]
    [Tooltip("How quickly the shade closes the gap to its slot, in seconds. Lower = snappier, higher = floatier")]
    [SerializeField, Min(0.01f)] float _CatchUpTime = 0.15f;

    [Tooltip("Fastest the shade can glide, in meters per second")]
    [SerializeField, Min(0.1f)] float _MaxSpeed = 20f;

    [Tooltip("How fast the shade turns to face the player's facing, in degrees per second")]
    [SerializeField, Min(0f)] float _TurnSpeed = 540f;

    [Tooltip("If the shade ends up farther than this from its slot (a teleport, a spawn point), it snaps there instead of gliding, in meters")]
    [SerializeField, Min(1f)] float _SnapDistance = 15f;

    [Tooltip("How far from the player the shade sits if its evolution has no Tether Distance set, in meters")]
    [SerializeField, Min(0f)] float _FallbackTetherDistance = 2f;

    [FoldoutGroup("Walls & Height")]
    [Tooltip("Which layers count as walls that pull the shade in closer. Units, projectiles and items are left off by default")]
    [SerializeField] LayerMask _WallLayers = Physics.DefaultRaycastLayers;

    [FoldoutGroup("Walls & Height")]
    [Tooltip("How fat the wall check is, in meters. Roughly the shade's body radius")]
    [SerializeField, Min(0f)] float _WallCheckRadius = 0.4f;

    [FoldoutGroup("Walls & Height")]
    [Tooltip("Gap kept between the shade and a wall it's pulled in by, in meters")]
    [SerializeField, Min(0f)] float _WallPadding = 0.2f;

    [FoldoutGroup("Walls & Height")]
    [Tooltip("How quickly the shade moves to the player's floor height when there's no floor under it (a pit or ledge), in seconds")]
    [SerializeField, Min(0.01f)] float _HeightCatchUpTime = 0.2f;

    [Header("References")]
    [Tooltip("Empty child the evolution's Tethered Art is spawned into. Uses this object if left empty")]
    [SerializeField] Transform _ArtHolder;

    [Header("Runtime Data")]
    [Tooltip("The shade slot this shade came from (read only)")]
    [SerializeField, ReadOnly] ShadeSO _SlotData;

    [Tooltip("The evolution its art and tether distance come from (read only)")]
    [SerializeField, ReadOnly] ShadeEvolutionSO _EvolutionData;

    [Tooltip("Where the shade is trying to sit: Resting (behind the player) or Casting (in front, toward the aim) (read only)")]
    [SerializeField, ReadOnly] ShadeFormType.Slot _CurrentSlot = ShadeFormType.Slot.Resting;

    [Tooltip("The spot the shade is gliding to this physics step (read only)")]
    [SerializeField, ReadOnly] Vector3 _SlotPosition;

    //local variables
    Rigidbody _RB;
    UnitHover _hover;
    ShadeTail _tail;
    GameObject _summoner; //the player this shade is tethered to
    Rigidbody _summonerRB;
    CharacterMovement _summonerMovement;
    UnitHover _summonerHover;
    GameObject _artInstance;
    ShadeArtRig _artRig;
    Coroutine _sceneSnapTimer;

    private void Awake()
    {
        _RB = GetComponent<Rigidbody>();
        _hover = GetComponent<UnitHover>();
        _tail = GetComponent<ShadeTail>();
        if (_ArtHolder == null) _ArtHolder = transform;

        //set the rigidbody up in code so the prefab needs no hand setup
        _RB.useGravity = false; //UnitHover holds it up over floors, and HoldHeight handles pits, so gravity would only fight them
        _RB.freezeRotation = true; //it only turns when this script turns it
        _RB.interpolation = RigidbodyInterpolation.Interpolate; //smooths the motion between physics steps

        //like the player, the tethered shade survives scene loads (only works on objects with no parent)
        DontDestroyOnLoad(gameObject);
    }

    private void Reset()
    {
        //Unity calls Reset when this component is first added. Walls skip units, projectiles and items by default
        _WallLayers = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Units", "Projectile", "Item"); //& ~ removes those layers from the mask
    }

    private void OnEnable()
    {
        //listens for scene loads so it can snap back to the player afterwards
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_sceneSnapTimer != null) StopCoroutine(_sceneSnapTimer);
        _sceneSnapTimer = null;
    }

    private void FixedUpdate()
    {
        //glides toward its slot every physics step
        if (_summoner == null) return; //not set up yet, or the player is gone

        _SlotPosition = GetSlotPosition();

        //too far away to glide (teleport, spawn point), just jump there
        if (FlatDistance(_RB.position, _SlotPosition) > _SnapDistance)
        {
            SnapToSlot();
            return;
        }

        MoveTowardSlot();
        HoldHeight();
        FaceForward();
    }

    #region Shade Form Interface
    public void Setup(ShadeSO slot, GameObject summoner)
    {
        //function the Shade Manager calls right after spawning this shade
        if (summoner == null) { Debug.LogError($"Error! No summoner given to {gameObject.name}, it has nobody to tether to", this); return; }

        _SlotData = slot;
        _EvolutionData = slot != null ? slot._CurrentEvolution : null; //"? :" picks the left value if the check is true, the right one if not
        if (_EvolutionData == null) Debug.LogWarning($"Warning! No evolution on the shade slot for {gameObject.name}, using the fallback tether distance and no art...", this);

        _summoner = summoner;
        _summonerRB = summoner.GetComponent<Rigidbody>();
        _summonerMovement = summoner.GetComponent<CharacterMovement>();
        _summonerHover = summoner.GetComponent<UnitHover>();

        SpawnArt();
        _tail.Setup(_summonerHover, GetTailConnectionPoint());
    }

    public void Appear()
    {
        //instant for now: pops straight into its slot. todo: rise-out-of-the-shadow effect once shade art exists
        SnapToSlot();
    }

    public void Dismiss()
    {
        //destroys it for now. todo: sink-into-the-player effect once shade art exists
        Destroy(gameObject);
    }
    #endregion

    #region Art
    void SpawnArt()
    {
        //function that spawns the evolution's tethered art and hides the parts the tethered form doesn't use
        if (_EvolutionData == null || _EvolutionData._TetheredArt == null) { Debug.LogWarning($"Warning! No Tethered Art on the evolution for {gameObject.name}, spawning no art...", this); return; }

        if (_artInstance != null) Destroy(_artInstance);
        _artInstance = Instantiate(_EvolutionData._TetheredArt, _ArtHolder.position, _ArtHolder.rotation, _ArtHolder);

        _artRig = _artInstance.GetComponent<ShadeArtRig>();
        if (_artRig == null) { Debug.LogWarning($"Warning! No ShadeArtRig on {_EvolutionData._TetheredArt.name}, the tail and casting will use the shade's center...", this); return; }
        _artRig.ApplyForm(ShadeFormType.Form.Tethered);
    }

    Transform GetTailConnectionPoint()
    {
        //where the tail meets the body. Falls back to the shade itself
        if (_artRig == null) return transform;
        return _artRig.GetTailConnectionPoint();
    }

    public Transform GetCastPoint()
    {
        //function for where abilities come from. Falls back to the shade itself
        if (_artRig == null) return transform;
        return _artRig.GetCastPoint();
    }
    #endregion

    #region Slots
    public void SetSlot(ShadeFormType.Slot slot)
    {
        //function for moving between resting (behind the player) and casting (in front, toward the aim)
        //todo: used once the tethered shade can cast abilities (waits for the ability overhaul)
        _CurrentSlot = slot;
    }

    Vector3 GetSlotPosition()
    {
        //function that works out where the shade should sit right now. Only the flat (x/z) position matters, height is handled separately
        Vector3 facing = GetPlayerFacing();
        Vector3 origin = _summoner.transform.position;

        //resting sits opposite the player's facing. PlayerInputDriver already makes facing follow travel, or the aim while aiming,
        //so this one rule covers "behind the direction of travel" and "opposite the aim target"
        Vector3 direction = -facing;
        float extraReach = 0f;

        if (_CurrentSlot == ShadeFormType.Slot.Casting)
        {
            direction = facing;
            //how far the cast point sticks out in front of the body, so it doesn't end up inside a wall either
            Vector3 castOffset = GetCastPoint().position - transform.position;
            extraReach = Mathf.Max(0f, Vector3.Dot(castOffset, direction));
        }

        float distance = LimitByWalls(origin, direction, GetTetherDistance(), extraReach);
        Vector3 slot = origin + direction * distance;
        slot.y = _RB.position.y; //keep the current height, height isn't decided here
        return slot;
    }

    float LimitByWalls(Vector3 origin, Vector3 direction, float distance, float extraReach)
    {
        //function that pulls the slot in closer if a wall is between the player and the slot
        //SphereCast is a raycast with thickness, so a fat shade can't slip half through a wall
        float checkDistance = distance + extraReach;
        if (Physics.SphereCast(origin, _WallCheckRadius, direction, out RaycastHit hit, checkDistance, _WallLayers, QueryTriggerInteraction.Ignore) == false) return distance;

        float allowed = hit.distance - _WallPadding - extraReach;
        return Mathf.Clamp(allowed, 0f, distance); //never closer than on top of the player, never farther than normal
    }

    float GetTetherDistance()
    {
        //how far from the player to sit, from the evolution so big and small shades both look right
        if (_EvolutionData == null) return _FallbackTetherDistance;
        return _EvolutionData._TetherDistance;
    }

    Vector3 GetPlayerFacing()
    {
        //function for which way the player is facing (their aim while aiming), flattened so it's only left/right/forward/back
        Vector3 facing = Vector3.zero;
        if (_summonerMovement != null) facing = _summonerMovement.GetLookDirection();
        if (facing.sqrMagnitude < 0.0001f) facing = _summoner.transform.forward; //nothing has aimed yet, use the body's facing

        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f) return Vector3.forward; //body is pointing straight up or down, pick any direction
        return facing.normalized;
    }
    #endregion

    #region Movement
    void MoveTowardSlot()
    {
        //function that glides the shade in a straight line toward its slot
        //it matches the player's speed and adds a push that closes the gap over _CatchUpTime, so it doesn't trail behind a running player
        Vector3 playerVelocity = Vector3.zero;
        if (_summonerRB != null) playerVelocity = _summonerRB.velocity;

        Vector3 gap = _SlotPosition - _RB.position;
        Vector3 desired = playerVelocity + gap / _CatchUpTime;
        desired.y = 0f;
        desired = Vector3.ClampMagnitude(desired, _MaxSpeed);

        //only set the flat part, the up/down speed belongs to UnitHover or HoldHeight
        _RB.velocity = new Vector3(desired.x, _RB.velocity.y, desired.z);
    }

    void HoldHeight()
    {
        //function for when there's no floor under the shade (a pit or ledge): it holds at the player's floor height instead of falling
        //over a floor, UnitHover's spring does the job, so this does nothing
        if (_hover.IsGrounded()) return;

        float targetHeight = _RB.position.y; //player in the air too: just hold still vertically
        if (_summonerHover != null && _summonerHover.IsGrounded()) targetHeight = _summonerHover.GetFloorHit().point.y + _hover.GetRideHeight();

        float verticalSpeed = (targetHeight - _RB.position.y) / _HeightCatchUpTime;
        verticalSpeed = Mathf.Clamp(verticalSpeed, -_MaxSpeed, _MaxSpeed);
        _RB.velocity = new Vector3(_RB.velocity.x, verticalSpeed, _RB.velocity.z);
    }

    void FaceForward()
    {
        //function that turns the shade to face the same way as the player (their aim while aiming). It never turns to travel
        Quaternion targetRotation = Quaternion.LookRotation(GetPlayerFacing());
        _RB.MoveRotation(Quaternion.RotateTowards(_RB.rotation, targetRotation, _TurnSpeed * Time.fixedDeltaTime));
    }

    void SnapToSlot()
    {
        //function that jumps the shade straight to its slot at the player's height, no gliding
        if (_summoner == null) return;

        _SlotPosition = GetSlotPosition();
        Vector3 snapPosition = new Vector3(_SlotPosition.x, _summoner.transform.position.y, _SlotPosition.z);
        Quaternion snapRotation = Quaternion.LookRotation(GetPlayerFacing());

        //set both the rigidbody and the transform, otherwise interpolation slides it from the old spot for a frame
        _RB.position = snapPosition;
        _RB.rotation = snapRotation;
        transform.SetPositionAndRotation(snapPosition, snapRotation);
        _RB.velocity = Vector3.zero;
    }
    #endregion

    #region Scene Loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //the player gets moved to a spawn point when a scene loads, so snap back to them once they've landed
        if (_sceneSnapTimer != null) StopCoroutine(_sceneSnapTimer);
        _sceneSnapTimer = StartCoroutine(SnapAfterLoad());
    }

    IEnumerator SnapAfterLoad()
    {
        //waits one frame so every spawn point in the new scene has moved the player first
        yield return null;
        _sceneSnapTimer = null;
        SnapToSlot();
    }
    #endregion

    #region Tools
    float FlatDistance(Vector3 a, Vector3 b)
    {
        //distance ignoring height
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
    #endregion

    #region Debugging
    private void OnDrawGizmosSelected()
    {
        //shows the slot the shade is gliding to (play mode) and the wall check size
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _WallCheckRadius);
        if (Application.isPlaying == false || _summoner == null) return;
        Gizmos.DrawLine(_summoner.transform.position, _SlotPosition);
        Gizmos.DrawWireSphere(_SlotPosition, _WallCheckRadius);
    }
    #endregion
}

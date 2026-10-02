using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class UnitTeam : MonoBehaviour
{
    //Says which side a unit is on and what kind of unit it is, and adds it to the UnitRegistry while it's active,
    //so AI can find it without searching the scene. Goes on the player, shades and enemies.

    [Header("Data")]
    [Tooltip("Which side this unit fights for. Units on different teams are hostile to each other")]
    public UnitType.Team _Team = UnitType.Team.Enemy;

    [Tooltip("What kind of unit this is. Player Body gets special target rules while the player is off controlling a shade")]
    public UnitType.Role _Role = UnitType.Role.Enemy;

    [Header("Settings")]
    [Tooltip("How much hostile AI prefers this unit over others in the same priority group. 1 = normal, 2 = twice as appealing, 0.5 = half. It never moves a unit into a different group (attackers always come first, see UnitTargeting)")]
    [SerializeField, Min(0.01f)] float _TargetPriority = 1f;

    [Header("References")]
    [ShowIf("IsPlayerBody")]
    [Tooltip("The input driver that shows whether the player is in this body. While it's off, the body counts as empty. Grabbed from this object if left empty")]
    [SerializeField] PlayerInputDriver _InputDriver;

    private void Awake()
    {
        if (IsPlayerBody() && _InputDriver == null) _InputDriver = GetComponent<PlayerInputDriver>();
        if (IsPlayerBody() && _InputDriver == null) Debug.LogWarning($"Warning! No PlayerInputDriver found for the player body {gameObject.name}, it will never count as empty...", this);
    }

    private void OnEnable()
    {
        //joins the list of active units
        UnitRegistry.Register(this);
    }

    private void OnDisable()
    {
        //leaves the list, so AI stops targeting it (this also runs when the object is destroyed)
        UnitRegistry.Unregister(this);
    }

    #region Team Checks
    public bool IsHostileTo(UnitTeam other)
    {
        //function for checking if another unit is on the opposite side
        if (other == null || other == this) return false;
        return other._Team != _Team;
    }

    public bool IsVacantBody()
    {
        //function for checking if this is the player's body while the player is away controlling a shade
        if (IsPlayerBody() == false || _InputDriver == null) return false;
        return _InputDriver.enabled == false;
    }

    public float GetTargetPriority()
    {
        //function that gives how much hostile AI prefers this unit
        return _TargetPriority;
    }
    #endregion

    #region Tools
    bool IsPlayerBody()
    {
        //used by the inspector to only show the input driver field on the player's body
        return _Role == UnitType.Role.PlayerBody;
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    //A list of every active unit (player, shades, enemies). Units add and remove themselves through UnitTeam,
    //so AI can loop over this list instead of searching the scene with FindObjectsOfType.
    //"static" means there's only ever one list and it doesn't need to sit on an object. Same idea as NavMeshTools

    static List<UnitTeam> _units = new List<UnitTeam>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry()
    {
        //Unity calls this when play mode starts. Static lists can survive between play sessions in the editor
        //(if domain reload is turned off), so we empty it to make sure no old units are left over
        _units.Clear();
    }

    #region Registration
    public static void Register(UnitTeam unit)
    {
        //function a unit calls to add itself to the list
        if (unit == null || _units.Contains(unit)) return; //ignore empty or duplicate registrations
        _units.Add(unit);
    }

    public static void Unregister(UnitTeam unit)
    {
        //function a unit calls to take itself off the list (disabled, destroyed or unloaded)
        _units.Remove(unit);
    }
    #endregion

    #region Tools
    public static List<UnitTeam> GetUnits()
    {
        //function that hands out the list of active units. Read it, don't add or remove from it directly
        return _units;
    }
    #endregion
}

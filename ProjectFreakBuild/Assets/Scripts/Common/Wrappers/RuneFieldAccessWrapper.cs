using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RuneFieldAccessWrapper : MonoBehaviour
{
    //goes on the rune field access point prefab. The access point's timeline calls one of the two launch functions:
    //Advanced for the hub labs, Basic for every other access point. What each kind allows is decided in ShadeManager (Rune Field Access region), not here

    [Header("References")]
    [Tooltip("Where the player is put back when they leave the rune field (position and facing). Left empty, this object's position and rotation are used")]
    [SerializeField] Transform _ReturnLocator;

    #region Launching
    public void LaunchAdvancedField()
    {
        //opens the rune field with everything allowed (hub labs)
        Launch(RuneFieldType.Access.Advanced, "LaunchAdvancedField");
    }

    public void LaunchBasicField()
    {
        //opens the rune field with the basic tools (dungeon entrances, before bosses, safe and vault floors)
        Launch(RuneFieldType.Access.Basic, "LaunchBasicField");
    }

    void Launch(RuneFieldType.Access access, string functionName)
    {
        //tells the Shade Manager which kind of access point this is, then has the Scene Manager swap to the field
        if (ManagerTester(functionName) == false) return;

        Transform returnSpot = _ReturnLocator != null ? _ReturnLocator : transform; //"a ? b : c" picks b if a is true, otherwise c
        ShadeManager._ShadeManager.SetFieldAccess(access);
        SceneManagerObject._SceneManager.TravelToRuneField(returnSpot.position, returnSpot.rotation);
    }
    #endregion

    #region Tools
    bool ManagerTester(string type)
    {
        //checks both managers exist before launching
        if (SceneManagerObject._SceneManager == null) { Debug.LogError($"Error! {type} wrapper on {gameObject.name} couldn't find the Scene Manager", this); return false; }
        if (ShadeManager._ShadeManager == null) { Debug.LogError($"Error! {type} wrapper on {gameObject.name} couldn't find the Shade Manager", this); return false; }
        return true;
    }
    #endregion

    #region Debugging
    private void OnDrawGizmosSelected()
    {
        //shows where the player comes back to (and which way they face) when this access point is selected
        Transform returnSpot = _ReturnLocator != null ? _ReturnLocator : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(returnSpot.position, 0.4f);
        Gizmos.DrawLine(returnSpot.position, returnSpot.position + returnSpot.forward);
    }
    #endregion
}

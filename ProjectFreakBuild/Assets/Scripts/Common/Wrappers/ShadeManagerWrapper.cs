using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scene-side component that forwards calls to the Shade Manager, so UnityEvents (buttons, interaction objects, timeline events, pickups)
//can rebuild, heal or level shades without code. Works on the currently selected shade slot unless the function takes a slot index
public class ShadeManagerWrapper : MonoBehaviour
{
    #region Runtime Entries
    public void RebuildCurrentSlot()
    {
        //works the selected slot's runtime entry out again from its slot asset
        if (ManagerTester("RebuildCurrentSlot") == false) return;
        ShadeManager._ShadeManager.RebuildSlot(ShadeManager._ShadeManager.GetShadeSelectionIndex());
    }

    public void RebuildSlot(int slotIndex)
    {
        //works one slot's runtime entry out again from its slot asset
        if (ManagerTester("RebuildSlot") == false) return;
        ShadeManager._ShadeManager.RebuildSlot(slotIndex);
    }
    #endregion

    #region Health
    public void HealCurrentShade(int amount)
    {
        //heals the selected shade, never above its max HP
        if (ManagerTester("HealCurrentShade") == false) return;
        ShadeManager._ShadeManager.Heal(ShadeManager._ShadeManager.GetShadeSelectionIndex(), amount);
    }

    public void RefillCurrentShadeHealth()
    {
        //puts the selected shade back to full health
        if (ManagerTester("RefillCurrentShadeHealth") == false) return;
        ShadeManager._ShadeManager.RefillHealth(ShadeManager._ShadeManager.GetShadeSelectionIndex());
    }

    public void RefillAllShadeHealth()
    {
        //puts every shade back to full health (like entering the hub or a rest floor)
        if (ManagerTester("RefillAllShadeHealth") == false) return;
        ShadeManager._ShadeManager.RefillAllHealth();
    }
    #endregion

    #region Leveling
    public void AddLevelsToCurrentShade(int amount)
    {
        //adds levels to the selected shade, re-running its saved rune field with the new core power
        if (ManagerTester("AddLevelsToCurrentShade") == false) return;
        ShadeManager._ShadeManager.AddLevels(ShadeManager._ShadeManager.GetShadeSelectionIndex(), amount);
    }
    #endregion

    #region Tools
    bool ManagerTester(string functionName)
    {
        //checks the Shade Manager exists before forwarding a call, so scenes can be tested on their own
        if (ShadeManager._ShadeManager == null) { Debug.LogError($"Error! {functionName} on {gameObject.name}: Shade Manager not found", this); return false; }
        return true;
    }
    #endregion
}

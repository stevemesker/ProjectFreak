using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

//one shade slot: everything long term about the shade in it (see Rune Field Overhaul Plan, "The shade slot is the source of truth")
//base stats come from the current evolution, this only holds what's personal to this shade
//this asset is changed during play ON PURPOSE (saving its rune field, leveling, evolving), and the changes stay in the editor afterwards, which is handy for playtests
//use the Reset Slot button to start it over. It's the one exception to "don't change SO data at runtime"
[CreateAssetMenu(fileName = "SO_NewShade", menuName = "ScriptableObjects/Shades/ShadeSlot", order = 0)]
public class ShadeSO : ScriptableObject
{
    [Header("Data")]
    [Tooltip("The shade's current form. Its base stats, art, body and abilities come from here")]
    [InfoBox("No evolution set: this shade has no base stats or art", InfoMessageType.Warning, "HasNoEvolution")]
    public ShadeEvolutionSO _CurrentEvolution;

    [Tooltip("The evolution gate this shade took in each zone, in order (node indexes in the field scene). Its rank is how many gates it has taken. Bound = 0")]
    public List<int> _GatesTaken = new List<int>();

    [Tooltip("The shade's level. Also its core power on the rune field (plus any fragment)")]
    [Min(1)] public int _Level = 1;

    [Tooltip("Experience toward the next level. Leveling isn't built yet")]
    [Min(0)] public int _XP = 0;

    [Tooltip("How many times the shade can fall in a dungeon before it's gone")]
    [Min(0)] public int _Lives = 3;

    [Tooltip("Extra core power from a slotted core fragment. 0 = no fragment. Slotting one is permanent (see Core Fragments in the Rune Field Overhaul Plan)")]
    [Min(0)] public int _FragmentPower = 0;

    [Header("Rune Field")]
    [Tooltip("The saved rune field: runes, bridges, plugs and active nodes. The field UI edits a copy and writes it back here when the player saves")]
    [FormerlySerializedAs("_StartingRuneField")] //this used to be the starting field the Shade Manager copied, this keeps anything already set
    public RuneFieldData _RuneField = new RuneFieldData();

    [Tooltip("Everything the saved field does to the shade, worked out when it was saved (read only). The Shade Manager builds the shade's stats from this")]
    [SerializeReference, ReadOnly] //SerializeReference lets the list hold different effect types
    public List<RuneEffect> _CompiledEffects = new List<RuneEffect>();

    [Tooltip("A copy of every node the saved field has a rune plugged into, powered or not (read only). Lets the rules run again without the field scene, like on a level-up")]
    [ReadOnly] public List<AbilityNodeEntry> _PluggedNodes = new List<AbilityNodeEntry>();

    #region Getters
    public int GetRank()
    {
        //how many times the shade has evolved. Bound = 0
        if (_GatesTaken == null) return 0;
        return _GatesTaken.Count;
    }

    public int GetCorePower()
    {
        //how much power the shade's rune field core has: its level plus its fragment
        return _Level + _FragmentPower;
    }

    public RuneFieldData GetRuneFieldCopy()
    {
        //returns a copy of the saved field, so editing it doesn't change the save until the player saves
        if (_RuneField == null) return new RuneFieldData();
        return _RuneField.Clone();
    }
    #endregion

    #region Saving
    public void SaveRuneField(RuneFieldData field, List<RuneEffect> compiledEffects, List<AbilityNodeEntry> pluggedNodes)
    {
        //function the Shade Manager calls when the player saves the rune field. Everything is REPLACED, never added to, so nothing can drift
        if (field == null) { Debug.LogError($"Error! Tried to save an empty rune field onto {name}", this); return; }

        _RuneField = field.Clone();
        _CompiledEffects = compiledEffects != null ? compiledEffects : new List<RuneEffect>();
        _PluggedNodes = pluggedNodes != null ? pluggedNodes : new List<AbilityNodeEntry>();
        MarkChanged();
    }

    public void SetLevel(int level)
    {
        //function the Shade Manager calls when the shade levels up. Never below 1. The manager re-runs the rune field afterwards
        _Level = Mathf.Max(1, level);
        MarkChanged();
    }

    public void Evolve(int gateIndex, ShadeEvolutionSO evolution)
    {
        //function the Shade Manager calls when a gate gets power: records the gate (rank goes up by 1) and swaps to the new form
        if (evolution == null) { Debug.LogError($"Error! Tried to evolve {name} with no evolution", this); return; }
        if (_GatesTaken == null) _GatesTaken = new List<int>();

        _GatesTaken.Add(gateIndex);
        _CurrentEvolution = evolution;
        MarkChanged();
    }

    [Button("Reset Slot (playtests)"), GUIColor(1f, 0.6f, 0.6f)]
    void ResetSlot()
    {
        //editor button that starts this shade over: empty field, level 1, no fragment, no gates, back in the Bound form
        //the Bound form is set on the Shade Manager, so it only comes back in play mode. Outside play mode the form is left as it is
        ShadeEvolutionSO bound = ShadeManager._ShadeManager != null ? ShadeManager._ShadeManager.GetBoundEvolution() : null;
        if (bound != null) _CurrentEvolution = bound;
        else Debug.LogWarning($"Warning! No Shade Manager running (or no Bound Evolution set on it), {name} keeps its current form. Reset in play mode to put the form back...", this);

        _RuneField = new RuneFieldData();
        _CompiledEffects = new List<RuneEffect>();
        _PluggedNodes = new List<AbilityNodeEntry>();
        _GatesTaken = new List<int>();
        _Level = 1;
        _XP = 0;
        _FragmentPower = 0;
        MarkChanged();

        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.RebuildSlot(ShadeManager._ShadeManager._ShadeSlots.IndexOf(this)); //in play mode, the stats catch up right away
    }

    void MarkChanged()
    {
        //tells the editor this asset changed so it gets saved to disk, not just kept in memory until Unity closes
        //"#if UNITY_EDITOR" means this part only exists in the editor. UnityEditor code isn't allowed in a built game
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    #endregion

    #region Tools
    bool HasNoEvolution()
    {
        //used by the InfoBox above to decide if the warning shows
        return _CurrentEvolution == null;
    }
    #endregion
}

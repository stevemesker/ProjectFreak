using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_SaveSlot_X", menuName = "ScriptableObjects/SaveSlot", order = 0)]
public class SaveSlotSO : ScriptableObject
{
    [Header("Game Data")]
    public bool _IsEmpty = true;

    [Header("Player Data")]
    public int _SaveChapter;
    public int _PlayerTamerLevel;

    [Header("Shade Data")]
    public int _ShadeSlotSelection;

    [Tooltip("Shade forms the player has seen. One not in here shows blacked out in the rune field's shade info window. A form counts as seen when a shade evolves into it (later: when its evolve cutscene plays). Remove one by hand to test the blacked out look")]
    public List<ShadeEvolutionSO> _SeenEvolutions = new List<ShadeEvolutionSO>();

    [Header("Scene Data")]
    public string _CurrentScene;
    public string _CurrentSceneReadable;

    public void ResetData()
    {
        _IsEmpty = true;
        _SaveChapter = 0;
        _PlayerTamerLevel = 0;
        _ShadeSlotSelection = 0;
        _SeenEvolutions = new List<ShadeEvolutionSO>();
        _CurrentScene = "";
        _CurrentSceneReadable = "";
    }

    public void MarkEvolutionSeen(ShadeEvolutionSO evolution)
    {
        //function the Shade Manager calls when a shade evolves: adds the form to the seen list (once)
        if (evolution == null) return;
        if (_SeenEvolutions == null) _SeenEvolutions = new List<ShadeEvolutionSO>();
        if (_SeenEvolutions.Contains(evolution)) return;

        _SeenEvolutions.Add(evolution);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); //tells the editor this asset changed so it gets saved to disk (editor only)
#endif
    }

    public bool HasSeenEvolution(ShadeEvolutionSO evolution)
    {
        //is this form in the seen list
        return _SeenEvolutions != null && _SeenEvolutions.Contains(evolution);
    }
}

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

    [Header("Scene Data")]
    public string _CurrentScene;
    public string _CurrentSceneReadable;

    public void ResetData()
    {
        _IsEmpty = true;
        _SaveChapter = 0;
        _PlayerTamerLevel = 0;
        _ShadeSlotSelection = 0;
        _CurrentScene = "";
        _CurrentSceneReadable = "";
    }
}

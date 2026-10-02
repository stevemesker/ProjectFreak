using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_AIPersonality_Name", menuName = "AI/Personality Preset", order = 0)]
public class AIPersonalitySO : ScriptableObject
{
    //A shared personality (like "Coward" or "Berserker") that many units can use. Tune it once here and every unit using it changes.
    //Units copy these values when the game starts and never change this asset (see PersonalitySetup).

    [Header("Data")]
    [Tooltip("Notes for yourself: what this personality is for and how it should play")]
    [TextArea(2, 4)] public string _Notes;

    [Tooltip("The personality values. See the Unit Brain note in the GDD for what each one does")]
    public AIPersonality _Values = new AIPersonality();

    #region Test Tools
    [Button("Reload All Units")]
    void ReloadAllUnits()
    {
        //editor button (play mode only): after changing these values during play, every unit picks up its personality again
        if (Application.isPlaying == false) return;
        UnitBrain.ReloadAllPersonalities();
    }
    #endregion
}

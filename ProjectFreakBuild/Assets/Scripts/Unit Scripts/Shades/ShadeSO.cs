using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_NewShade", menuName = "ScriptableObjects/Shades/ShadeSlot", order = 0)]
public class ShadeSO : ScriptableObject
{
    [Tooltip("The shade's base stats, before any runes")]
    public ShadeStats _shadeStats;

    [Tooltip("Old: was changed at runtime by the old rune field. Not used anymore, the Shade Manager works out stats with runes now (see Slot Stats on the Shade Manager)")]
    public ShadeStats _AlteredStats;

    public ShadeEvolutionSO _CurrentEvolution;

    [Tooltip("The rune field this slot starts the game with. Copied by the Shade Manager when the game starts, so playing never changes this asset. Usually left empty")]
    public RuneFieldData _StartingRuneField = new RuneFieldData();

    [Tooltip("Old save format from the previous rune field. Not used anymore")]
    public RuneFieldPackage _RuneFieldPackage;
}

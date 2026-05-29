using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_NewShade", menuName = "ScriptableObjects/Shades/ShadeSlot", order = 0)]
public class ShadeSO : ScriptableObject
{
    public ShadeStats _shadeStats;
    public ShadeStats _AlteredStats;
    public ShadeEvolutionSO _CurrentEvolution;
}

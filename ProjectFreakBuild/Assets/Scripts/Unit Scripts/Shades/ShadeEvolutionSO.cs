using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ShadeEvo_New", menuName = "ScriptableObjects/Shades/EvolutionStats", order = 0)]
public class ShadeEvolutionSO : ScriptableObject
{
    public ShadeStats _coreStats;
    public GameObject _characterArt;
}

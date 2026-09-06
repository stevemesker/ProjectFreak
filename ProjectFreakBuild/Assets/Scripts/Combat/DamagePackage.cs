using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamagePackage 
{
    public GameObject _Source;

    public float _CritMultiplier;

    public float _DamageImpactStrength = .25f;

    public List<DamageEntry> _Entries;
}

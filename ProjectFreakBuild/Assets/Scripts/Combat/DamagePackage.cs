using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePackage : MonoBehaviour
{
    public GameObject _Source;

    public float _CritMultiplier;

    public List<DamageEntry> _Entries = new List<DamageEntry>();
}

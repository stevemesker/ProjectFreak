using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Ability_", menuName = "Ability", order = 0)]
public class AbilitySO : ScriptableObject
{
    [Header("Ability Information")]
    public string _AbilityName;

    [Header("Ability Stats")]
    public float _AbilityCooldown = 0.5f;

}

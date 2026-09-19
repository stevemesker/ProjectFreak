using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Shade : MonoBehaviour, ISummonUnit
{
    //the current shade on the field
    public static Shade shade;

    [Header("Data")]
    public ShadeSO _shadeSlotData;
    public ShadeEvolutionSO _shadeEvoData;

    [FoldoutGroup("Pointers")]
    [Header("Script pointers")]
    public CharacterMovement _movement;
    public GameObject PlayerRef;

    private void OnEnable()
    {
        if (Shade.shade == null) Shade.shade = this;
    }

    #region Control Shade

    public void EnablePlayerControl()
    {
        _movement.EnableMovement();
    }

    public void EnableShadeControl()
    {
        _movement.DisableMovement();
    }
    #endregion

    #region Isummon
    public void AssignSummoner(GameObject Summoner)
    {
        PlayerRef = Summoner;
    }

    public void UpdateStats(CoreStats summonerStats)
    {

    }
    #endregion
}

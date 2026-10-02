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
    [Tooltip("Reads the player's controls and steers _movement while the player controls this shade. Grabbed from this object if left empty")]
    public PlayerInputDriver _InputDriver;
    public GameObject PlayerRef;

    private void Awake()
    {
        if (_InputDriver == null) _InputDriver = GetComponent<PlayerInputDriver>();
    }

    private void OnEnable()
    {
        if (Shade.shade == null) Shade.shade = this;
    }

    #region Control Shade

    public void EnablePlayerControl()
    {
        //the player takes over this shade: turn its input driver on
        if (_InputDriver == null) { Debug.LogError($"Error! No PlayerInputDriver on {gameObject.name}, the player can't control it", this); return; }
        _InputDriver.enabled = true;
    }

    public void EnableShadeControl()
    {
        //the shade acts on its own: turn the player's input off
        //todo: turn the shade's AI driver on here (step 6 of the AI plan). For now it just stands still
        if (_InputDriver == null) { Debug.LogError($"Error! No PlayerInputDriver on {gameObject.name}", this); return; }
        _InputDriver.enabled = false;
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

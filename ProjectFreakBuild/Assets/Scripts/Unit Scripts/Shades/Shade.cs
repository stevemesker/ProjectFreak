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
    [Tooltip("The AI driver that steers the shade along the NavMesh when it acts on its own. Grabbed from this object if left empty")]
    public NavGuideDriver _AIDriver;
    public GameObject PlayerRef;

    private void Awake()
    {
        if (_InputDriver == null) _InputDriver = GetComponent<PlayerInputDriver>();
        if (_AIDriver == null) _AIDriver = GetComponent<NavGuideDriver>();
    }

    private void OnEnable()
    {
        if (Shade.shade == null) Shade.shade = this;
    }

    #region Control Shade

    public void EnablePlayerControl()
    {
        //the player takes over this shade: AI driver off first, then the input driver on
        if (_AIDriver != null) _AIDriver.enabled = false;
        if (_InputDriver == null) { Debug.LogError($"Error! No PlayerInputDriver on {gameObject.name}, the player can't control it", this); return; }
        _InputDriver.enabled = true;
    }

    public void EnableShadeControl()
    {
        //the shade acts on its own: input driver off first, then the AI driver on
        if (_InputDriver != null) _InputDriver.enabled = false;
        if (_AIDriver == null) { Debug.LogWarning($"Warning! No NavGuideDriver on {gameObject.name}, it will just stand still...", this); return; }
        _AIDriver.enabled = true;
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

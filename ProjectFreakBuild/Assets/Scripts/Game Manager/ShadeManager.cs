using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using Sirenix.OdinInspector;

public class ShadeManager : MonoBehaviour
{
    public static ShadeManager _ShadeManager;

    [Header("Pointer")]
    [SerializeField]ElementManagerSO managerScriptableObject;
    [SerializeField] GameObject _ShadePrefab;
    [SerializeField] GameObject _CurrentShade;


    [Header("All possible shade slots")]
    public List<ShadeSO> _ShadeSlots;

    [Header("How many shade slots the player has access to")]
    public int tamerSlotLevel;

    [SerializeField] int currentShadeSelected;
    [SerializeField] List<statBoostPackage> shadeAlterPackages;

    [Header("Local Variables")]
    [SerializeField] float _switchTimeIn = .5f;
    [SerializeField] float _switchTimeOut = .3f;
    Coroutine _transferTimer;
    [SerializeField] bool isBusy;

    private void Awake()
    {
        //sets up the singleton so other scripts can reach this manager with ShadeManager._ShadeManager
        if (_ShadeManager == null) _ShadeManager = this;
    }

    private void OnEnable()
    {
        managerScriptableObject.manager = this;
    }

    #region ShadeControl
    public void SummonShade(Vector3 position)
    {
        if (isBusy) return;
        if (_CurrentShade != null) _CurrentShade.transform.position = Player.player.transform.position + position;
        else _CurrentShade = Instantiate(_ShadePrefab, Player.player.transform.position+position, Quaternion.identity);
        ControlShade(false);
    }

    [Button("Control Shade")]

    public void ShadeControlAbility()
    {
        if (isBusy) return;
        if (_CurrentShade == null) return;
        isBusy = true;
        _transferTimer = StartCoroutine(ShadeControlSwitch(_switchTimeIn, _switchTimeOut, _CurrentShade));
    }

    public void ControlShade(bool control)
    {
        if (control)
        {
            Player.player.DisablePlayerControl();
            Shade.shade.EnablePlayerControl();
        }
        else
        {
            Player.player.EnablePlayerControl();
            Shade.shade.EnableShadeControl();
        }
        
    }

    IEnumerator ShadeControlSwitch(float easeOutTime, float easeInTime, GameObject controlTarget)
    {
        HUDManager._HUD.FadeOut(easeOutTime);
        yield return new WaitForSeconds(easeOutTime + .5f);

        CameraManager._CamManager.SetCamTargetToTarget(controlTarget);
        if (controlTarget == Player.player.gameObject) ControlShade(false); //player is back in their own body
        else ControlShade(true); //player is controlling the shade

        HUDManager._HUD.FadeIn(easeInTime);
        yield return new WaitForSeconds(easeInTime + .1f);
        isBusy = false;
    }

    #endregion

    #region get shade info
    public ShadeSO GetCurrentShade()
    {
        return _ShadeSlots[currentShadeSelected];
    }

    public ShadeSO GetShadeOfIndex(int index)
    {
        return _ShadeSlots[index];
    }
    #endregion

    #region stat change
    public void ReceiveStatBoostPackage(List<statBoostPackage> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            ChangeStat(input[i], 1);
        }
    }

    public void RemoveStatBoostPackage(List<statBoostPackage> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            ChangeStat(input[i], -1);
        }
    }

    public void ChangeStat(statBoostPackage input, int multiplier)
    {
        switch (input._statToChange)
        {
            case (DamageType.StatType.Health):
                _ShadeSlots[currentShadeSelected]._AlteredStats._HP += input._ChangeAmount * multiplier;
                _ShadeSlots[currentShadeSelected]._AlteredStats._Health += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Strength):
                _ShadeSlots[currentShadeSelected]._AlteredStats._STR += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Defense):
                _ShadeSlots[currentShadeSelected]._AlteredStats._DEF += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Agility):
                _ShadeSlots[currentShadeSelected]._AlteredStats._AGI += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Intelect):
                _ShadeSlots[currentShadeSelected]._AlteredStats._INT += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Spirit):
                _ShadeSlots[currentShadeSelected]._AlteredStats._SPR += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Wisdom):
                _ShadeSlots[currentShadeSelected]._AlteredStats._WIS += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Discipline):
                _ShadeSlots[currentShadeSelected]._AlteredStats._DIS += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Wild):
                _ShadeSlots[currentShadeSelected]._AlteredStats._WILD += input._ChangeAmount * multiplier;
                break;
            default:
                Debug.LogWarning("Warning! Stat upgrade package is trying to access a stat that is unaccounted for: " + input._statToChange);
                break;
        }
    }

    #endregion

    #region Save Rune Field Package
    public void SaveCurrentShadeRuneFieldPackage(RuneFieldPackage package)
    {
        _ShadeSlots[currentShadeSelected]._RuneFieldPackage = package;
    }
    #endregion

    #region Shade Selection Querry
    public void SetShadeSelection(int Selection)
    {
        currentShadeSelected = Selection;
    }
    public int GetShadeSelectionIndex()
    {
        return currentShadeSelected;
    }
    #endregion
}

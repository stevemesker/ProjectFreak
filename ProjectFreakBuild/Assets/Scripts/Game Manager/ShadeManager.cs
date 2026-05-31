using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ShadeManager : MonoBehaviour
{
    [Header("Pointer")]
    [SerializeField]ElementManagerSO managerScriptableObject;

    [Header("All possible shade slots")]
    public List<ShadeSO> _ShadeSlots;

    [Header("How many shade slots the player has access to")]
    public int tamerSlotLevel;

    [SerializeField] int currentShadeSelected;
    [SerializeField] List<statBoostPackage> shadeAlterPackages;

    private void OnEnable()
    {
        managerScriptableObject.manager = this;
    }

    #region get shade info
    public ShadeSO getCurrentShade()
    {
        return _ShadeSlots[currentShadeSelected];
    }

    public ShadeSO getShadeOfIndex(int index)
    {
        return _ShadeSlots[index];
    }
    #endregion

    #region stat change
    public void receiveStatBoostPackage(List<statBoostPackage> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            ChangeStat(input[i], 1);
        }
    }

    public void removeStatBoostPackage(List<statBoostPackage> input)
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
            case (StatNameType.Stat.Health):
                _ShadeSlots[currentShadeSelected]._AlteredStats._HP += input._ChangeAmount * multiplier;
                _ShadeSlots[currentShadeSelected]._AlteredStats._Health += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Strength):
                _ShadeSlots[currentShadeSelected]._AlteredStats._STR += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Defense):
                _ShadeSlots[currentShadeSelected]._AlteredStats._DEF += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Agility):
                _ShadeSlots[currentShadeSelected]._AlteredStats._AGI += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Intelect):
                _ShadeSlots[currentShadeSelected]._AlteredStats._INT += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Spirit):
                _ShadeSlots[currentShadeSelected]._AlteredStats._SPR += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Wisdom):
                _ShadeSlots[currentShadeSelected]._AlteredStats._WIS += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Discipline):
                _ShadeSlots[currentShadeSelected]._AlteredStats._DIS += input._ChangeAmount * multiplier;
                break;
            case (StatNameType.Stat.Wild):
                _ShadeSlots[currentShadeSelected]._AlteredStats._WILD += input._ChangeAmount * multiplier;
                break;
            default:
                Debug.LogWarning("Warning! Stat upgrade package is trying to access a stat that is unaccounted for: " + input._statToChange);
                break;
        }
    }

    #endregion

    #region Save Rune Field Package
    public void saveCurrentShadeRuneFieldPackage(RuneFieldPackage package)
    {
        _ShadeSlots[currentShadeSelected]._RuneFieldPackage = package;
    }
    #endregion

    #region Shade Selection Querry
    public void setShadeSelection(int Selection)
    {
        currentShadeSelected = Selection;
    }
    public int getShadeSelectionIndex()
    {
        return currentShadeSelected;
    }
    #endregion
}

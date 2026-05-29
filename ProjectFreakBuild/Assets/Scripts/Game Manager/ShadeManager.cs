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

    private void OnEnable()
    {
        managerScriptableObject.manager = this;
    }

    public void setSelection(int Selection)
    {
        currentShadeSelected = Selection;
    }
}

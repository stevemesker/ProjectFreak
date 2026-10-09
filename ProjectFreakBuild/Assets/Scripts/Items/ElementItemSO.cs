using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SO_NewElement", menuName = "ScriptableObjects/Items/Element", order = 0)]
public class ElementItemSO : ItemSO
{
    [Header("===Ingredient Base Data===")]
    public Sprite itemSprite;

    [Header("Stat Upgrade")]
    [SerializeField] List<statBoostPackage> mypackage;

    [Header("Status Effect Settings")]
    [SerializeField] public UnityEvent statusEffectEnable;
    [SerializeField] public UnityEvent statusEffectDisable;

    [Header("Physical Settings")]
    public DamageType.ElementType element;
    public ElementMaterialType.Type materialType;

    [Header("Grid Settings")]
    public int connectionsAllowed = 2;
    public float connectionDistance = 150;
    public int powerNeeded = 1;

    public void TriggerElementEffects()
    {
        //function that turns this element's effects on (statusEffectEnable is usually hooked up to ElementManagerSO.BoostStats)
        //this used to write the rune object into the package, but this SO is shared by every rune of this type, so runes kept overwriting each other
        statusEffectEnable?.Invoke();
    }

    public void DeactivateElementEffects()
    {
        //function that turns this element's effects off (statusEffectDisable is usually hooked up to ElementManagerSO.ReduceStats)
        statusEffectDisable?.Invoke();
    }

    public List<statBoostPackage> GetStatBoostPackage()
    {
        return mypackage;
    }
}

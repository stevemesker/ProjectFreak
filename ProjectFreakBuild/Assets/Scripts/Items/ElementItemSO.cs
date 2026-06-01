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
    public ElementType.Element element;
    public ElementMaterialType.Type materialType;

    [Header("Grid Settings")]
    public int connectionsAllowed = 2;
    public float connectionDistance = 150;
    public int powerNeeded = 1;

    public void triggerElementEffects(GameObject ElementCarrier)
    {
        mypackage[0]._ElementConnect = ElementCarrier;
        statusEffectEnable?.Invoke();
    }

    public void deactivateElementEffects(GameObject ElementCarrier)
    {
        mypackage[0]._ElementConnect = ElementCarrier;
        statusEffectDisable?.Invoke();
    }

    public List<statBoostPackage> getStatBoostPackage()
    {
        return mypackage;
    }
}

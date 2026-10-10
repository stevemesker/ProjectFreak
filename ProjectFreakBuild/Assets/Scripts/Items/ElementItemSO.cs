using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewElement", menuName = "ScriptableObjects/Items/Element", order = 0)]
public class ElementItemSO : ItemSO
{
    [Header("===Ingredient Base Data===")]
    public Sprite itemSprite;

    [Header("Stat Upgrade")]
    [Tooltip("Stats this rune adds to the shade while it has power. Compiled into the shade's effect list when the rune field is saved")]
    [FormerlySerializedAs("mypackage")] //this list used to be mypackage (old statBoostPackage entries), this keeps the values already set on element assets
    [InfoBox("$_statBoostProblems", InfoMessageType.Warning, "HasStatBoostProblems")]
    [SerializeField] List<StatChangeEffect> _StatBoosts = new List<StatChangeEffect>();

    [Header("Physical Settings")]
    public DamageType.ElementType element;
    public ElementMaterialType.Type materialType;

    [Header("Grid Settings")]
    public int connectionsAllowed = 2;
    public float connectionDistance = 150;
    public int powerNeeded = 1;

    //local variables
    string _statBoostProblems; //filled by OnValidate, shown as a warning box in the inspector

    private void OnValidate()
    {
        //checks the stat boosts for missing stats or 0 amounts so broken runes show up in the inspector
        _statBoostProblems = "";
        if (_StatBoosts == null) return;

        for (int i = 0; i < _StatBoosts.Count; i++)
        {
            if (_StatBoosts[i] == null) continue;
            string problem = _StatBoosts[i].GetSetupProblem();
            if (problem != "") _statBoostProblems += $"Stat boost {i}: {problem}\n";
        }
    }

    public List<StatChangeEffect> GetStatBoosts()
    {
        //returns this rune's stat boosts. Don't change them at runtime: this asset is shared by every rune of this type
        if (_StatBoosts == null) _StatBoosts = new List<StatChangeEffect>();
        return _StatBoosts;
    }

    bool HasStatBoostProblems()
    {
        //used by the InfoBox above to decide if the warning shows
        return string.IsNullOrEmpty(_statBoostProblems) == false;
    }
}

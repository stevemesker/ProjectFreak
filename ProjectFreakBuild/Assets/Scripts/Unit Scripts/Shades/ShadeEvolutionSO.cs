using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

//one evolution (form) a shade can be in. Making an evolution = filling in one of these, the system handles the rest
//anything personal to one shade (level, lives, runes, fragment) lives on its slot (ShadeSO), not here
//no element on purpose: a shade's affinity only comes from the player's runes, so every form stays viable for every element
[CreateAssetMenu(fileName = "SO_ShadeEvo_New", menuName = "ScriptableObjects/Shades/EvolutionStats", order = 0)]
public class ShadeEvolutionSO : ScriptableObject
{
    [FoldoutGroup("Stats", true)]
    [Tooltip("This form's base stats, set by hand. Runes and nodes are added on top. Level, XP, Health (current), Lives and Name in here are ignored: those come from the shade slot")]
    [InfoBox("$_setupProblems", InfoMessageType.Warning, "HasSetupProblems")]
    [FormerlySerializedAs("_coreStats")] //this field used to be _coreStats, this keeps the stats already set on evolution assets
    public ShadeStats _BaseStats;

    [FoldoutGroup("Art", true)]
    [Tooltip("Art prefab spawned while the shade is tethered. Should have a ShadeArtRig on its root")]
    public GameObject _TetheredArt;

    [FoldoutGroup("Art")]
    [Tooltip("Art prefab spawned while the shade is released. Can be the same prefab as Tethered Art (use the rig's hide lists to turn parts off per form)")]
    [FormerlySerializedAs("_characterArt")] //this field used to be called _characterArt, this keeps the art already assigned on existing assets
    public GameObject _ReleasedArt;

    [FoldoutGroup("Body", true)]
    [Tooltip("How far from the player the tethered shade sits, in meters. Bigger shades need more room")]
    [Min(0f)] public float _TetherDistance = 2f;

    [FoldoutGroup("Body")]
    [Tooltip("How wide the shade's body is where the tail connects, in meters. Not used yet, for the real tail later")]
    [Min(0f)] public float _TetherWidth = 0.5f;

    [FoldoutGroup("Body")]
    [Tooltip("How big this form is. Uses the same size classes and Size Class Rules as enemies (knockback, and AI later). Not read by the shade yet")]
    public EnemyType.SizeClass _SizeClass = EnemyType.SizeClass.Medium;

    [FoldoutGroup("Abilities", true)]
    [Tooltip("This form's ultimate ability. Not used yet, shades get their abilities with the Shade Manager runtime entries")]
    public AbilitySO _UltimateAbility;

    [FoldoutGroup("Abilities")]
    [Tooltip("Abilities this form always has, before any runes or nodes. Not used yet, same as above")]
    public List<AbilitySO> _NaturalAbilities = new List<AbilitySO>();

    //local variables
    string _setupProblems; //filled by OnValidate, shown as a warning box in the inspector

    private void OnValidate()
    {
        //checks for missing pieces so an unfinished evolution shows up in the inspector
        _setupProblems = "";
        if (_BaseStats == null || _BaseStats._HP <= 0) _setupProblems += "Base stats have no HP set\n";
        if (_TetheredArt == null) _setupProblems += "No Tethered Art\n";
        if (_ReleasedArt == null) _setupProblems += "No Released Art\n";
        if (_UltimateAbility == null) _setupProblems += "No Ultimate Ability\n";

        if (_NaturalAbilities == null) return;
        for (int i = 0; i < _NaturalAbilities.Count; i++)
        {
            if (_NaturalAbilities[i] == null) _setupProblems += $"Natural ability {i} is empty\n";
        }
    }

    bool HasSetupProblems()
    {
        //used by the InfoBox above to decide if the warning shows
        return string.IsNullOrEmpty(_setupProblems) == false;
    }
}

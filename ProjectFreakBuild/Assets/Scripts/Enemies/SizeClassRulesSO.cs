using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_SizeClassRules", menuName = "Combat/Size Class Rules", order = 0)]
public class SizeClassRulesSO : ScriptableObject
{
    //Shared rules for how each size class reacts to hits. One asset for the whole game, held by the Game Manager.

    [Header("Data")]
    [Tooltip("One entry per size class. Use the Fill Defaults button to start from the standard setup")]
    [SerializeField] List<SizeClassEntry> _SizeClasses = new List<SizeClassEntry>();

    private void OnValidate()
    {
        //editor-only check that every size class has exactly one entry
        foreach (EnemyType.SizeClass size in System.Enum.GetValues(typeof(EnemyType.SizeClass)))
        {
            int count = 0;
            for (int i = 0; i < _SizeClasses.Count; i++)
            {
                if (_SizeClasses[i] != null && _SizeClasses[i]._SizeClass == size) count++;
            }
            if (count == 0) Debug.LogWarning($"Warning! {name} has no entry for size class {size}, it will use full knockback", this);
            if (count > 1) Debug.LogWarning($"Warning! {name} has {count} entries for size class {size}, only the first one is used", this);
        }
    }

    #region Lookups
    public SizeClassEntry GetEntry(EnemyType.SizeClass size)
    {
        //function that finds the rules for a size class, or null if there isn't an entry
        for (int i = 0; i < _SizeClasses.Count; i++)
        {
            if (_SizeClasses[i] != null && _SizeClasses[i]._SizeClass == size) return _SizeClasses[i];
        }
        return null;
    }

    public float GetKnockbackMultiplier(EnemyType.SizeClass size, List<DamageEntry> damageEntries)
    {
        //function that works out how much of a hit's knockback distance a size class actually takes
        SizeClassEntry entry = GetEntry(size);
        if (entry == null) return 1f; //no rules for this size, take the full knockback (OnValidate warns about this)

        //if any part of the hit is an attack type this size can't resist (like Explosion for Large), it takes full knockback
        if (damageEntries != null)
        {
            for (int i = 0; i < damageEntries.Count; i++)
            {
                if (entry._FullKnockbackAttackTypes.Contains(damageEntries[i]._atkType)) return 1f;
            }
        }

        return entry._KnockbackDistanceMultiplier;
    }

    public float GetStaggerMultiplier(EnemyType.SizeClass size)
    {
        //function for how much of a hit's stagger power a size class takes (Small takes it all, Huge none)
        SizeClassEntry entry = GetEntry(size);
        if (entry == null) return 1f; //no rules for this size, take the full stagger chance (OnValidate warns about this)
        return entry._StaggerMultiplier;
    }
    #endregion

    #region Tools
    [Button("Fill Defaults")]
    void FillDefaults()
    {
        //editor button that resets the list to the standard setup
        _SizeClasses.Clear();
        _SizeClasses.Add(new SizeClassEntry(EnemyType.SizeClass.Small, 1f, 1.25f, 1f));
        _SizeClasses.Add(new SizeClassEntry(EnemyType.SizeClass.Medium, 1f, 1f, 0.4f));

        SizeClassEntry large = new SizeClassEntry(EnemyType.SizeClass.Large, 0.5f, 1f, 0.15f);
        large._FullKnockbackAttackTypes.Add(DamageType.AttackType.Explosion);
        _SizeClasses.Add(large);

        _SizeClasses.Add(new SizeClassEntry(EnemyType.SizeClass.Huge, 0f, 1f, 0f));
    }
    #endregion
}

[System.Serializable]
public class SizeClassEntry
{
    [Tooltip("Which size class these rules are for")]
    public EnemyType.SizeClass _SizeClass;

    [Tooltip("How much of a hit's knockback distance this size takes. 1 = full, 0.5 = half, 0 = none")]
    [Min(0f)] public float _KnockbackDistanceMultiplier = 1f;

    [Tooltip("Extra damage this size takes from hits. 1 = normal, 1.25 = 25% more. Not applied yet, comes with the damage pass")]
    [Min(0f)] public float _BonusDamageMultiplier = 1f;

    [Tooltip("Attack types that ignore this size's knockback reduction and always push it the full distance")]
    public List<DamageType.AttackType> _FullKnockbackAttackTypes = new List<DamageType.AttackType>();

    [Tooltip("How much of a hit's stagger power this size takes. Chance to stagger = hit's stagger power × this. 1 = full, 0 = never staggers from regular hits. Mini bosses and bosses ignore this and stagger at set health points instead")]
    [Range(0f, 1f)] public float _StaggerMultiplier = 1f;

    public SizeClassEntry() { } //empty constructor so Unity can make new entries when you press + in the inspector

    public SizeClassEntry(EnemyType.SizeClass size, float knockbackMultiplier, float bonusDamageMultiplier, float staggerMultiplier)
    {
        //constructor used by the Fill Defaults button
        _SizeClass = size;
        _KnockbackDistanceMultiplier = knockbackMultiplier;
        _BonusDamageMultiplier = bonusDamageMultiplier;
        _StaggerMultiplier = staggerMultiplier;
    }
}

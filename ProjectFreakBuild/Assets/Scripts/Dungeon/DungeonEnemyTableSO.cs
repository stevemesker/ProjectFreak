using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_EnemyTable_DungeonName", menuName = "Dungeon/Enemy Table", order = 0)]
public class DungeonEnemyTableSO : ScriptableObject
{
    //The list of enemies a dungeon can spawn. Enemy spawners on a floor ask for an enemy of a rank (Popcorn, Basic, Lieutenant)
    //and this picks one, using each enemy's weight. Plugged into a DungeonSO's Enemy Table slot.
    //Read only at runtime: picking an enemy never changes this asset.

    [Header("Data")]
    [InfoBox("Some entries have no prefab, or a prefab without an Enemy Movement + EnemySO. They'll be skipped", InfoMessageType.Warning, "HasBrokenEntries")]
    [InfoBox("MiniBoss and Boss enemies in this list are never picked. They're placed by hand in their arenas", InfoMessageType.Info, "HasBossEntries")]
    [Tooltip("Every enemy this dungeon can spawn. Just drop in enemy prefabs: each one's rank comes from its EnemySO")]
    public List<EnemyTableEntry> _Enemies = new List<EnemyTableEntry>();

    [Tooltip("Changes how many enemies of each rank spawners place on a type of floor. e.g. Vault: Popcorn x0, Lieutenant x2. Floor types or ranks not listed use x1")]
    public List<FloorTypeMultiplierEntry> _FloorTypeMultipliers = new List<FloorTypeMultiplierEntry>();

    #region Picking
    public GameObject GetEnemy(EnemyType.Rank rank)
    {
        //function that picks a random enemy prefab of a rank. Higher weights get picked more often. Returns null if there's no enemy of that rank

        //first pass: add up the weights of every enemy that can be picked
        float totalWeight = 0f;
        for (int i = 0; i < _Enemies.Count; i++)
        {
            if (CanPick(_Enemies[i], rank)) totalWeight += _Enemies[i]._Weight;
        }
        if (totalWeight <= 0f) return null;

        //second pass: roll a number along the total, then walk the list until the roll lands inside an enemy's share
        //(like a number line split into chunks, where bigger weights get bigger chunks)
        float roll = Random.Range(0f, totalWeight);
        GameObject lastMatch = null;
        for (int i = 0; i < _Enemies.Count; i++)
        {
            if (CanPick(_Enemies[i], rank) == false) continue;

            lastMatch = _Enemies[i]._EnemyPrefab;
            if (roll < _Enemies[i]._Weight) return lastMatch;
            roll -= _Enemies[i]._Weight;
        }

        return lastMatch; //only reached if the roll landed exactly on the total (rounding), so the last enemy gets it
    }

    public float GetCountMultiplier(POIType.Type floorType, EnemyType.Rank rank)
    {
        //function that returns how much to multiply a spawner's count for this rank on this type of floor. x1 if it isn't listed
        for (int i = 0; i < _FloorTypeMultipliers.Count; i++)
        {
            if (_FloorTypeMultipliers[i]._FloorType != floorType) continue;

            List<RankMultiplierEntry> rankMultipliers = _FloorTypeMultipliers[i]._RankMultipliers;
            for (int j = 0; j < rankMultipliers.Count; j++)
            {
                if (rankMultipliers[j]._Rank == rank) return rankMultipliers[j]._Multiplier;
            }
        }
        return 1f;
    }

    bool CanPick(EnemyTableEntry entry, EnemyType.Rank rank)
    {
        //function that checks if an entry is set up properly, has some weight, and is the rank we want
        if (entry == null || entry._Weight <= 0f) return false;
        if (entry.TryGetRank(out EnemyType.Rank entryRank) == false) return false;
        return entryRank == rank;
    }
    #endregion

    #region Debugging
    bool HasBrokenEntries()
    {
        //editor check for the warning box: any entry missing its prefab or EnemySO
        for (int i = 0; i < _Enemies.Count; i++)
        {
            if (_Enemies[i] == null || _Enemies[i].TryGetRank(out EnemyType.Rank rank) == false) return true;
        }
        return false;
    }

    bool HasBossEntries()
    {
        //editor check for the info box: any MiniBoss or Boss in the list
        for (int i = 0; i < _Enemies.Count; i++)
        {
            if (_Enemies[i] == null || _Enemies[i].TryGetRank(out EnemyType.Rank rank) == false) continue;
            if (rank == EnemyType.Rank.MiniBoss || rank == EnemyType.Rank.Boss) return true;
        }
        return false;
    }
    #endregion
}

[System.Serializable]
public class EnemyTableEntry
{
    [Tooltip("The enemy prefab. Needs an Enemy Movement with an EnemySO assigned, which is where its rank comes from")]
    public GameObject _EnemyPrefab;

    [Tooltip("How often this enemy is picked compared to others of the same rank. 2 = twice as often as 1, 0 = never")]
    [Min(0f)] public float _Weight = 1f;

    //ShowInInspector lets Odin show a value that isn't saved. This one just displays the rank read from the prefab, so you can check it at a glance
    [ShowInInspector, ReadOnly]
    string RankPreview
    {
        get
        {
            if (TryGetRank(out EnemyType.Rank rank)) return rank.ToString();
            return "Missing prefab or EnemySO";
        }
    }

    public bool TryGetRank(out EnemyType.Rank rank)
    {
        //function that reads the rank off the prefab's EnemySO. Returns false if the prefab or its EnemySO is missing
        rank = EnemyType.Rank.Basic;
        if (_EnemyPrefab == null) return false;
        if (_EnemyPrefab.TryGetComponent(out EnemyMovement enemyMovement) == false) return false;

        EnemySO enemyData = enemyMovement.GetEnemyData();
        if (enemyData == null) return false;

        rank = enemyData._Rank;
        return true;
    }
}

[System.Serializable]
public class FloorTypeMultiplierEntry
{
    [Tooltip("The type of floor (map node type) these multipliers apply to")]
    public POIType.Type _FloorType;

    [Tooltip("How much to multiply each rank's spawn count by on this floor type. Ranks not listed use x1")]
    public List<RankMultiplierEntry> _RankMultipliers = new List<RankMultiplierEntry>();
}

[System.Serializable]
public class RankMultiplierEntry
{
    [Tooltip("Which rank of enemy this multiplier changes")]
    public EnemyType.Rank _Rank;

    [Tooltip("Multiplies the spawner's count for this rank. 0 = none, 1 = normal, 2 = double")]
    [Min(0f)] public float _Multiplier = 1f;
}

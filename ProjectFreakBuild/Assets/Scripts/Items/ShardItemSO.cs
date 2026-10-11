using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//a core fragment ("shard"): extra core power for a shade's rune field. One asset per tier, so shards of the same tier stack in the inventory like any item
//a shade can have one slotted, for good (ShadeManager.SlotShard). Its core power is then its level plus this shard's power
//dropping shards when a shade dies or is broken down is part of the fail state work, not built yet
[CreateAssetMenu(fileName = "SO_Shard_Minor", menuName = "ScriptableObjects/Items/Shard", order = 1)]
public class ShardItemSO : ItemSO
{
    [Header("Shard")]
    [Tooltip("Which tier this shard is. Starting tiers: Minor (lvl 4-10), Basic (11-20), Major (21-40), Perfect (41-50)")]
    public ShardType.Tier _Tier = ShardType.Tier.Minor;

    [Tooltip("How much core power this shard adds to the shade it's slotted into. Starting numbers: Minor 1, Basic 3, Major 7, Perfect 15")]
    [Min(1)] public int _Power = 1;
}

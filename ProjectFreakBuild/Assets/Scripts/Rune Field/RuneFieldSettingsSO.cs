using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the rune field numbers every shade slot shares. One asset (SO_RuneField_Settings) lives on the Shade Manager
//the rules need these even without the field scene open (level-ups in a dungeon), so they live here instead of on the scene
//ability nodes are NOT in here: they're objects in the field scene, and each slot keeps snapshots of the ones it plugged into
[CreateAssetMenu(fileName = "SO_RuneField_Settings", menuName = "Rune Field/Settings", order = 0)]
public class RuneFieldSettingsSO : ScriptableObject
{
    public const float RingTolerance = 1f; //how close to a ring counts as sitting on it, in field units. Covers tiny float errors from moving things in the editor

    [Header("Core")]
    [Tooltip("How far the core reaches to make a bridge, in field units (a node is about 100 wide). A bridge to the core works if the rune is within the core's reach or the rune's own reach, whichever is bigger")]
    [Min(0f)] public float _CoreReach = 150f;

    [Tooltip("How many bridges the core can have. 0 = no limit")]
    [Min(0)] public int _CoreMaxBridges = 0;

    [Tooltip("How wide the core's footprint is, in field units. Runes can't be placed overlapping it. Its own setting so the core can stand out as the centerpiece")]
    [Min(1f)] public float _CoreSize = 100f;

    [Header("Field")]
    [Tooltip("Extra room past the outer ring, in field units. Runes can't be placed out there, but dragging (and the background) go this far so the field doesn't end in a hard cut")]
    [Min(0f)] public float _EdgeBleed = 100f;

    [Tooltip("How close a rune has to be dropped to an ability node's center to plug in, in field units. Keep it smaller than Rune Size, so landing near a node always means snapping into it")]
    [Min(0f)] public float _NodeSnapRadius = 70f;

    [Tooltip("How far past their reach a rune's bridges may stretch when it snaps into a node, in field units. While the rune sits in the node, its bridges get this extra length")]
    [Min(0f)] public float _SnapStretch = 30f;

    [Header("Zones")]
    [Tooltip("How many zones (rings) the field has, one per rank: Bound, Unbound, Ascendant, Legend. Evolution gates go on every ring but the last one, which is the field's outer edge")]
    [Min(1)] public int _ZoneCount = 4;

    [Tooltip("How wide every zone ring is, in field units. Zone 1's edge is 1 x this from the core, zone 2's is 2 x, and so on")]
    [Min(1f)] public float _ZoneWidth = 300f;

    [Header("Runes")]
    [Tooltip("How wide a rune's footprint is, in field units. Runes can't overlap each other, the core or a node (nodes are rune sized, since each holds one rune)")]
    [Min(1f)] public float _RuneSize = 100f;

    #region Zone Math
    public float GetRingRadius(int ring)
    {
        //how far ring number "ring" is from the core, in field units. Ring 1 is zone 1's outer edge
        return ring * _ZoneWidth;
    }

    public float GetFieldEdge()
    {
        //the outermost ring. Runes can't be placed past it
        return GetRingRadius(_ZoneCount);
    }

    public int GetLastGateRing()
    {
        //gates go on every ring except the outer edge (nothing to evolve into after the last zone)
        return _ZoneCount - 1;
    }

    public int GetZone(float distance)
    {
        //which zone a spot this far from the core is in. A spot sitting on ring k counts as zone k, so a gate on ring 1 belongs to zone 1
        //CeilToInt rounds up (1.2 -> 2). The tolerance is taken off first so a spot a hair past the ring still counts as on it
        int zone = Mathf.CeilToInt((distance - RingTolerance) / _ZoneWidth);
        return Mathf.Clamp(zone, 1, _ZoneCount); //the core counts as zone 1, and anything past the edge counts as the last zone
    }

    public bool IsOnRing(float distance, out int ring)
    {
        //checks if a spot this far from the core sits on a ring, and which one
        //RoundToInt gives the closest ring number, then it checks the spot is actually within the tolerance of it
        ring = Mathf.RoundToInt(distance / _ZoneWidth);
        return ring >= 1 && Mathf.Abs(distance - GetRingRadius(ring)) <= RingTolerance;
    }

    public int GetNextRingOut(float distance)
    {
        //the closest ring further out than this spot. A spot already sitting on a ring gets the one after it
        //the tolerance is added first, so "on ring 1" (even a hair inside it) rounds up to ring 2
        return Mathf.CeilToInt((distance + RingTolerance) / _ZoneWidth);
    }
    #endregion

    #region Tools
#if UNITY_EDITOR
    static RuneFieldSettingsSO _editorCache; //found once, then reused, so the gizmos aren't searching the project every frame

    public static RuneFieldSettingsSO FindInProject()
    {
        //editor only: finds the settings asset in the project so editor tools (zone gizmos, the node snap buttons) work with nothing assigned
        //AssetDatabase is the editor's view of the project files. FindAssets("t:Type") returns the GUID of every asset of that type
        if (_editorCache != null) return _editorCache;

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RuneFieldSettingsSO");
        if (guids.Length == 0) return null;
        if (guids.Length > 1) Debug.LogWarning("Warning! More than one Rune Field Settings asset in the project, the editor tools are using the first one found...");

        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
        _editorCache = UnityEditor.AssetDatabase.LoadAssetAtPath<RuneFieldSettingsSO>(path);
        return _editorCache;
    }
#endif
    #endregion
}

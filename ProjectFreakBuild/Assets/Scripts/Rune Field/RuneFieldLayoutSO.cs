using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//the parts of the rune field that are the same for every shade slot: the core's settings and where the ability nodes are
[CreateAssetMenu(fileName = "SO_RuneField_Layout_Name", menuName = "Rune Field/Layout", order = 0)]
public class RuneFieldLayoutSO : ScriptableObject
{
    [Header("Settings")]
    [Tooltip("How far the core reaches to make a bridge, in field units (a node is about 100 wide). A bridge to the core works if the rune is within the core's reach or the rune's own reach, whichever is bigger")]
    [Min(0f)] public float _CoreReach = 150f;

    [Tooltip("How many bridges the core can have. 0 = no limit")]
    [Min(0)] public int _CoreMaxBridges = 0;

    [Tooltip("How far from the core runes can be placed, in field units. Keeps runes inside the area the camera can see. 0 = no limit")]
    [Min(0f)] public float _FieldRadius = 1000f;

    [Header("Data")]
    [Tooltip("Every ability node on the field. Unlock and lockout lists point at other nodes by their index in this list")]
    [InfoBox("$_layoutProblems", InfoMessageType.Warning, "HasLayoutProblems")]
    public List<AbilityNodeEntry> _AbilityNodes = new List<AbilityNodeEntry>();

    //local variables
    string _layoutProblems; //filled by OnValidate, shown as a warning box in the inspector

    private void OnValidate()
    {
        //checks the node lists for indexes that don't exist or nodes that point at themselves, so broken setups show up in the inspector
        _layoutProblems = "";

        for (int i = 0; i < _AbilityNodes.Count; i++)
        {
            AbilityNodeEntry node = _AbilityNodes[i];
            if (node == null) continue;

            CheckIndexList(node._Unlocks, i, "unlocks");
            CheckIndexList(node._Lockouts, i, "lockouts");
        }
    }

    #region Tools
    void CheckIndexList(List<int> indexes, int nodeIndex, string listName)
    {
        //function that adds a warning line for every bad index in one of a node's lists
        if (indexes == null) return;

        for (int j = 0; j < indexes.Count; j++)
        {
            if (indexes[j] < 0 || indexes[j] >= _AbilityNodes.Count)
                _layoutProblems += $"Node {nodeIndex} {listName} has index {indexes[j]}, which isn't in the node list\n";
            else if (indexes[j] == nodeIndex)
                _layoutProblems += $"Node {nodeIndex} {listName} itself\n";
        }
    }

    bool HasLayoutProblems()
    {
        //used by the InfoBox above to decide if the warning shows
        return string.IsNullOrEmpty(_layoutProblems) == false;
    }
    #endregion
}

[System.Serializable]
public class AbilityNodeEntry
{
    [Tooltip("Name to show for this node (and to tell nodes apart in the inspector)")]
    public string _Name = "New Node";

    [Tooltip("Where the node sits on the field, in field units. The core is at (0, 0)")]
    public Vector2 _Position;

    [Tooltip("How close a rune has to be dropped to the node's center to plug in, in field units")]
    [Min(0f)] public float _SnapRadius = 50f;

    [Tooltip("Indexes of nodes that must be on before this one can turn on")]
    public List<int> _Unlocks = new List<int>();

    [Tooltip("Indexes of nodes this one locks out while it's on")]
    public List<int> _Lockouts = new List<int>();

    //todo: what the node actually does (shade ability, stat upgrade, evolution) gets added with the overhaul
}

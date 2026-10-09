using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the saved state of one rune field (one per shade slot). Just data, the rules live in RuneField
//positions are in "field units", the same scale the old UI used (a node is about 100 wide). The 3D view decides how big a field unit is in the world
[System.Serializable]
public class RuneFieldData
{
    [Header("Data")]
    [Tooltip("Every rune placed on the field")]
    public List<PlacedRuneEntry> _Runes = new List<PlacedRuneEntry>();

    [Tooltip("Every bridge on the field. A bridge joins two runes, or a rune and the core (core ID is -1)")]
    public List<RuneBridgeEntry> _Bridges = new List<RuneBridgeEntry>();

    [Tooltip("Which rune is plugged into which ability node")]
    public List<NodePlugEntry> _Plugs = new List<NodePlugEntry>();

    [Tooltip("Ability nodes that were on the last time the field was worked out. Kept so two nodes that lock each other don't swap which one wins when something unrelated changes")]
    public List<int> _ActiveNodes = new List<int>();

    [Tooltip("The ID the next placed rune will get. IDs never get reused, so saved bridges and plugs can't end up pointing at the wrong rune")]
    public int _NextRuneID = 0;

    #region Tools
    public RuneFieldData Clone()
    {
        //function that makes a full copy of this data
        //used for the save button: the player edits a copy, saving copies it back, leaving without saving just throws the copy away
        RuneFieldData copy = new RuneFieldData();
        copy._NextRuneID = _NextRuneID;

        for (int i = 0; i < _Runes.Count; i++)
        {
            PlacedRuneEntry rune = new PlacedRuneEntry();
            rune._ID = _Runes[i]._ID;
            rune._Element = _Runes[i]._Element; //the SO itself is shared on purpose, it never changes at runtime
            rune._Position = _Runes[i]._Position;
            copy._Runes.Add(rune);
        }

        for (int i = 0; i < _Bridges.Count; i++)
        {
            RuneBridgeEntry bridge = new RuneBridgeEntry();
            bridge._A = _Bridges[i]._A;
            bridge._B = _Bridges[i]._B;
            copy._Bridges.Add(bridge);
        }

        for (int i = 0; i < _Plugs.Count; i++)
        {
            NodePlugEntry plug = new NodePlugEntry();
            plug._NodeIndex = _Plugs[i]._NodeIndex;
            plug._RuneID = _Plugs[i]._RuneID;
            copy._Plugs.Add(plug);
        }

        copy._ActiveNodes.AddRange(_ActiveNodes); //ints copy by value, so AddRange is enough here
        return copy;
    }
    #endregion
}

[System.Serializable]
public class PlacedRuneEntry
{
    [Tooltip("This rune's ID on the field. Unique and never reused")]
    public int _ID;

    [Tooltip("Which element rune this is. Its reach, max bridges and power cost come from here")]
    public ElementItemSO _Element;

    [Tooltip("Where the rune sits on the field, in field units. The core is at (0, 0)")]
    public Vector2 _Position;
}

[System.Serializable]
public class RuneBridgeEntry
{
    [Tooltip("ID of one end of the bridge (-1 = the core)")]
    public int _A;

    [Tooltip("ID of the other end of the bridge (-1 = the core)")]
    public int _B;
}

[System.Serializable]
public class NodePlugEntry
{
    [Tooltip("Index of the ability node in the layout's node list")]
    public int _NodeIndex;

    [Tooltip("ID of the rune plugged into it")]
    public int _RuneID;
}

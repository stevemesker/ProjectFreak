using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RunePackage
{
    [Tooltip("element data scriptable object held within the element rune")]
    public ElementItemSO _elementDataPointer;
    [Tooltip("position of the element on the rune field without the scroll zoom applied")]
    public Vector3 _elementPosition;
    [Tooltip("index reference of the elements attached to this element")]
    public List<int> _connectionIndexRef;
    [Tooltip("The element's current power")]
    public int _currentPower;
}

[System.Serializable]
public class NodePackage
{
    [Tooltip("refers to which rune this is in the package")]
    public int _NodeIndex;
    //
    [Tooltip("refers to the index of the element it is attached to")]
    public int _ElementIndex;

    [Tooltip("If the attached run is powered")]
    public bool _IsPowered;
}
[System.Serializable]
public class CorePackage
{
    [Tooltip("Current Power of the Core")]
    public int _CoreCurrentPower;
    [Tooltip("index reference of the elements attached to the core")]
    public List<int> _ConnectionIndexRef;
}

[System.Serializable]
public class RuneFieldPackage
{
    //
    [Tooltip("list of runes")]
    public List<RunePackage> _Runes;
    //
    [Tooltip("list of edited nodes")]
    public List<NodePackage> _Nodes;

    public CorePackage _Core;
}

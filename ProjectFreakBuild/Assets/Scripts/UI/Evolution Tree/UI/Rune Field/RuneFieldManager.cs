using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//the 2D rune field UI. It's only a view now: every rule lives in RuneField (Scripts/Rune Field), this script draws the field
//and passes the player's drags to it. It kept its old name and fields so the scene and prefabs didn't need setting up again
public class RuneFieldManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far the core reaches to make a bridge, in field units (the field's own pixels at zoom 1). Only used when the Shade Manager has no layout asset and one is built from this scene")]
    [SerializeField, Min(0f)] float _CoreReach = 150f;

    [Tooltip("How close a rune has to be dropped to an ability node's center to plug in, in field units. Only used when building the layout from this scene")]
    [SerializeField, Min(0f)] float _NodeSnapRadius = 70f;

    [Tooltip("How far past its reach a rune has to be pulled before its bridges start tearing, in field units")]
    [SerializeField, Min(0f)] float _TearSlack = 2f;

    [Header("References")]
    [Tooltip("The core node object. It's the center (0, 0) of the field")]
    [SerializeField] GameObject CorePointer;

    [Tooltip("Every ability node object, in the same order as the layout's node list")]
    public List<GameObject> ListOfNodes;

    [Tooltip("Rune objects currently shown (filled at runtime). Runes placed in the scene by hand are removed on start, the field is built from the shade slot's data")]
    [SerializeField] private List<GameObject> ListOfRunes;

    [Tooltip("Rune prefab spawned for every rune on the field. Needs an ElementItem on its root. Its Bridge Prefab is used for the bridges")]
    [SerializeField] GameObject elementPrefab;

    //local variables
    RuneField _field; //the rules for the field being edited
    RuneFieldLayoutSO _layout; //core settings and ability nodes
    int _slotIndex = -1; //which shade slot is open
    int _draggingRuneID = -1; //rune being dragged right now, or -1
    CoreNode _core;
    Dictionary<int, ElementItem> _runeViews = new Dictionary<int, ElementItem>(); //rune ID -> the object showing it
    List<NodeBridge> _bridgeViews = new List<NodeBridge>();

    private void Start()
    {
        //removes the test runes placed in the scene, then opens whichever shade slot is selected
        if (CorePointer == null) { Debug.LogError($"Error! Core Pointer not assigned on {gameObject.name}", this); return; }
        if (elementPrefab == null) { Debug.LogError($"Error! Element Prefab not assigned on {gameObject.name}", this); return; }
        _core = CorePointer.GetComponent<CoreNode>();

        ClearHandPlacedRunes();

        if (ShadeManager._ShadeManager == null) { Debug.LogWarning($"Warning! No Shade Manager found, {gameObject.name} can't load a shade slot. Leaving the field empty...", this); return; }
        LoadRuneField(ShadeManager._ShadeManager.GetShadeSelectionIndex());
    }

    private void OnDestroy()
    {
        //stops listening to the field so it doesn't call into a destroyed object
        if (_field != null) _field.OnFieldChanged -= OnFieldChanged;
    }

    #region Loading And Saving
    public void LoadRuneField(int index)
    {
        //opens a shade slot's saved rune field (the shade slot buttons call this). Unsaved changes on the open slot are thrown away
        if (ShadeManager._ShadeManager == null) { Debug.LogError($"Error! Shade Manager not found, can't load rune field on {gameObject.name}", this); return; }
        if (CorePointer == null) return; //already logged in Start
        if (ShadeManager._ShadeManager.IsValidSlot(index) == false) { Debug.LogWarning($"Warning! Shade slot {index} doesn't exist, {gameObject.name} isn't loading anything...", this); return; }
        if (_layout == null) SetUpLayout();

        ShadeManager._ShadeManager.SetShadeSelection(index);
        _slotIndex = index;
        StartField(ShadeManager._ShadeManager.GetSavedRuneField(index)); //this is a copy, so editing it doesn't change the save until the player saves
    }

    [Button("Save Current Field")]
    public void SaveRuneSlot()
    {
        //saves the field onto the open shade slot, which also works out the shade's stats again. Hook a UI button to this
        if (_field == null || ShadeManager._ShadeManager == null) return;
        ShadeManager._ShadeManager.SaveRuneField(_slotIndex, _field.GetData());
    }

    [Button("Discard Changes")]
    public void DiscardChanges()
    {
        //goes back to the slot's saved field
        if (_slotIndex == -1) return;
        LoadRuneField(_slotIndex);
    }

    [Button("Test Clear")]
    public void ClearRuneField()
    {
        //empties the field being edited (nothing changes on the slot until the player saves)
        if (_field == null) return;
        StartField(new RuneFieldData());
    }

    void StartField(RuneFieldData data)
    {
        //swaps in a new field and redraws everything
        if (_field != null) _field.OnFieldChanged -= OnFieldChanged;

        _field = new RuneField(data, _layout, ShadeManager._ShadeManager.GetCorePower(_slotIndex));
        _field.OnFieldChanged += OnFieldChanged; //"+=" subscribes, so OnFieldChanged runs every time the field changes

        ClearViews();
        RefreshView(false); //node events don't fire on load, the nodes were already on/off when this field was saved
    }
    #endregion

    #region Layout
    void SetUpLayout()
    {
        //uses the Shade Manager's layout, or builds one from this scene's nodes if it doesn't have one yet
        _layout = ShadeManager._ShadeManager.GetRuneFieldLayout();

        if (_layout == null)
        {
            _layout = BuildLayoutFromScene();
            ShadeManager._ShadeManager.SetRuneFieldLayout(_layout);
            return;
        }

        PlaceNodesFromLayout();
    }

    RuneFieldLayoutSO BuildLayoutFromScene()
    {
        //makes a layout (in memory only, not an asset) from where the node objects sit and their unlock/lockout lists
        RuneFieldLayoutSO layout = ScriptableObject.CreateInstance<RuneFieldLayoutSO>();
        layout.name = "Layout built from " + gameObject.name;
        layout._CoreReach = _CoreReach;
        layout._CoreMaxBridges = 0;
        layout._FieldRadius = 0f; //no limit, the UI panel decides how far the player can drag

        for (int i = 0; i < ListOfNodes.Count; i++)
        {
            AbilityNodeEntry entry = new AbilityNodeEntry();
            entry._SnapRadius = _NodeSnapRadius;
            layout._AbilityNodes.Add(entry); //added even if the object is missing so the indexes still line up with ListOfNodes

            if (ListOfNodes[i] == null) { entry._Name = "Missing node"; entry._SnapRadius = 0f; continue; }
            entry._Name = ListOfNodes[i].name;
            entry._Position = WorldToField(ListOfNodes[i].transform.position);

            if (ListOfNodes[i].TryGetComponent(out EvolutionNode node) == false) continue;
            entry._Unlocks = NodesToIndexes(node.GetUnlockNodes());
            entry._Lockouts = NodesToIndexes(node.GetLockoutNodes());
        }

        return layout;
    }

    List<int> NodesToIndexes(List<EvolutionNode> nodes)
    {
        //turns a list of node objects into their indexes in ListOfNodes. Nodes that aren't in the list are skipped
        List<int> indexes = new List<int>();
        if (nodes == null) return indexes;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            int index = ListOfNodes.IndexOf(nodes[i].gameObject);
            if (index != -1) indexes.Add(index);
        }
        return indexes;
    }

    void PlaceNodesFromLayout()
    {
        //moves the node objects to where the layout asset says they are
        if (ListOfNodes.Count != _layout._AbilityNodes.Count)
            Debug.LogWarning($"Warning! {gameObject.name} has {ListOfNodes.Count} node objects but the layout has {_layout._AbilityNodes.Count} nodes. Only matching ones are placed...", this);

        int count = Mathf.Min(ListOfNodes.Count, _layout._AbilityNodes.Count);
        for (int i = 0; i < count; i++)
        {
            if (ListOfNodes[i] == null) continue;
            ListOfNodes[i].transform.position = FieldToWorld(_layout._AbilityNodes[i]._Position);
        }
    }
    #endregion

    #region Player Actions
    public void PlaceRuneFromInventory(ElementItemSO element, Vector2 screenPosition)
    {
        //called when an element is dragged from the inventory list onto the field
        //todo: take it out of the inventory (and give it back if the field isn't saved), see Known Issues
        if (_field == null) return;
        if (_field.TryPlaceRune(element, WorldToField(screenPosition), out int runeID) == false) return;

        _field.TryPlugRune(runeID); //plug in first, it snaps the rune to the node's center
        _field.ConnectNearby(runeID); //then bridge from where it ended up
    }

    public void BeginRuneDrag(int runeID)
    {
        //the player picked up a rune
        if (_field == null) return;
        _draggingRuneID = runeID;
        _field.UnplugRune(runeID);
    }

    public void DragRune(int runeID, Vector2 screenPosition)
    {
        //moves a rune with the pointer, kept within reach of its bridges, and starts tearing any bridge pulled too far
        if (_field == null || _runeViews.ContainsKey(runeID) == false) return;

        Vector2 desired = WorldToField(screenPosition);
        Vector2 clamped = _field.ClampToBridges(runeID, desired);
        _field.MoveRune(runeID, clamped);

        //show the rune snapping into a node it's hovering, without plugging it in until it's dropped
        Vector2 shownPosition = clamped;
        int nodeIndex = _field.FindNodeAt(clamped);
        if (nodeIndex != -1 && _field.CanSnapToNode(runeID, nodeIndex)) shownPosition = _layout._AbilityNodes[nodeIndex]._Position;

        _runeViews[runeID].transform.position = FieldToWorld(shownPosition);
        UpdateBridgePositions();
        UpdateTearing(runeID, desired);
    }

    public void EndRuneDrag(int runeID)
    {
        //the player let go of a rune: plug into a node if it's on one, then bridge to whatever's in reach
        StopAllTearing();
        _draggingRuneID = -1;
        if (_field == null || _field.GetRune(runeID) == null) return;

        _field.TryPlugRune(runeID);
        _field.ConnectNearby(runeID);
        RefreshView(true); //puts the rune exactly where the data says, even if nothing changed
    }

    public void TearBridge(int idA, int idB)
    {
        //called by a bridge when its tear timer fills up
        if (_field == null) return;
        _field.Disconnect(idA, idB);
    }
    #endregion

    #region Drawing
    void OnFieldChanged()
    {
        //runs every time the field recalculates (see RuneField.OnFieldChanged)
        RefreshView(true);
    }

    void RefreshView(bool fireNodeEvents)
    {
        //makes every object on the field match the field's data
        if (_field == null) return;

        SyncRuneViews();
        RebuildBridgeViews();

        foreach (KeyValuePair<int, ElementItem> entry in _runeViews)
        {
            entry.Value.ShowPower(_field.IsRunePowered(entry.Key), _field.GetPowerNeeded(entry.Key));
        }

        for (int i = 0; i < ListOfNodes.Count; i++)
        {
            if (ListOfNodes[i] == null) continue;
            if (ListOfNodes[i].TryGetComponent(out EvolutionNode node) == false) continue;
            node.ShowState(_field.IsNodeActive(i), _field.IsNodeLockedOut(i), fireNodeEvents);
        }

        if (_core != null) _core.ShowPower(_field.GetPowerLeft(), _field.GetMaxPower());
    }

    void SyncRuneViews()
    {
        //spawns objects for new runes, removes objects for runes that are gone, and moves the rest into place
        List<PlacedRuneEntry> runes = _field.GetData()._Runes;

        //remove views whose rune is gone. The keys are copied first because a dictionary can't be changed while looping over it
        List<int> shownIDs = new List<int>(_runeViews.Keys);
        for (int i = 0; i < shownIDs.Count; i++)
        {
            if (_field.GetRune(shownIDs[i]) != null) continue;
            ListOfRunes.Remove(_runeViews[shownIDs[i]].gameObject);
            Destroy(_runeViews[shownIDs[i]].gameObject);
            _runeViews.Remove(shownIDs[i]);
        }

        for (int i = 0; i < runes.Count; i++)
        {
            PlacedRuneEntry rune = runes[i];
            if (_runeViews.ContainsKey(rune._ID) == false)
            {
                GameObject spawned = Instantiate(elementPrefab, transform);
                if (spawned.TryGetComponent(out ElementItem view) == false) { Debug.LogError($"Error! Element Prefab on {gameObject.name} has no ElementItem on its root", this); Destroy(spawned); continue; }

                view.Setup(this, rune._ID, rune._Element);
                _runeViews.Add(rune._ID, view);
                ListOfRunes.Add(spawned);
            }

            if (rune._ID != _draggingRuneID) _runeViews[rune._ID].transform.position = FieldToWorld(rune._Position); //the dragged rune follows the pointer instead
        }
    }

    void RebuildBridgeViews()
    {
        //throws away every bridge object and makes one for each bridge in the data. There are only ever a few dozen, so this is cheap
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            if (_bridgeViews[i] != null) Destroy(_bridgeViews[i].gameObject);
        }
        _bridgeViews.Clear();

        GameObject bridgePrefab = GetBridgePrefab();
        if (bridgePrefab == null) { Debug.LogError($"Error! No Bridge Prefab set on the rune prefab used by {gameObject.name}", this); return; }

        List<RuneBridgeEntry> bridges = _field.GetData()._Bridges;
        for (int i = 0; i < bridges.Count; i++)
        {
            GameObject endA = GetEndObject(bridges[i]._A);
            GameObject endB = GetEndObject(bridges[i]._B);
            if (endA == null || endB == null) continue;

            GameObject spawned = Instantiate(bridgePrefab, endA.transform.position, Quaternion.identity, transform);
            spawned.transform.SetAsFirstSibling(); //drawn behind the runes and nodes
            spawned.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

            if (spawned.TryGetComponent(out NodeBridge bridge) == false) { Destroy(spawned); continue; }
            bridge.Setup(this, bridges[i]._A, bridges[i]._B, endA, endB);
            _bridgeViews.Add(bridge);
        }

        UpdateBridgePositions();
    }

    void UpdateBridgePositions()
    {
        //stretches every bridge between its two ends
        float scale = GetScale();
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            NodeBridge bridge = _bridgeViews[i];
            if (bridge == null || bridge.connectionOne == null || bridge.connectionTwo == null) continue;
            float length = Vector3.Distance(bridge.connectionOne.transform.position, bridge.connectionTwo.transform.position) / scale;
            bridge.UpdatePosition(length);
        }
    }

    void UpdateTearing(int runeID, Vector2 desiredPosition)
    {
        //starts tearing every bridge on this rune that's being pulled past its reach, and stops the rest
        float scale = GetScale();
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            NodeBridge bridge = _bridgeViews[i];
            if (bridge == null || bridge.Touches(runeID) == false) continue;

            float stretch = _field.GetBridgeStretch(runeID, desiredPosition, bridge.GetOtherEnd(runeID));
            if (stretch > _TearSlack) bridge.StartTearing(stretch * scale); //in screen pixels, which is what the old tearing values were tuned for
            else bridge.StopTearing();
        }
    }

    void StopAllTearing()
    {
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            if (_bridgeViews[i] != null) _bridgeViews[i].StopTearing();
        }
    }

    void ClearViews()
    {
        //removes every rune and bridge object
        foreach (KeyValuePair<int, ElementItem> entry in _runeViews)
        {
            if (entry.Value != null) Destroy(entry.Value.gameObject);
        }
        _runeViews.Clear();
        ListOfRunes.Clear();

        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            if (_bridgeViews[i] != null) Destroy(_bridgeViews[i].gameObject);
        }
        _bridgeViews.Clear();
    }

    void ClearHandPlacedRunes()
    {
        //removes runes that were placed in the scene by hand for testing the old system
        for (int i = 0; i < ListOfRunes.Count; i++)
        {
            if (ListOfRunes[i] != null) Destroy(ListOfRunes[i]);
        }
        ListOfRunes.Clear();
    }

    public void UpdateScaler()
    {
        //the zoom script calls this. Nothing to do anymore: runes, bridges and nodes are children of the field so they scale with it,
        //and the rules don't use colliders now. Kept so the zoom script's event hookup doesn't break
    }
    #endregion

    #region Tools
    float GetScale()
    {
        //how big one field unit is on screen right now (changes with zoom)
        float scale = transform.lossyScale.x;
        return scale == 0f ? 1f : scale; //never divide by 0
    }

    Vector2 WorldToField(Vector3 worldPosition)
    {
        //turns a screen/UI position into field units, with the core at (0, 0)
        return (worldPosition - CorePointer.transform.position) / GetScale();
    }

    Vector3 FieldToWorld(Vector2 fieldPosition)
    {
        //turns field units into a screen/UI position
        return CorePointer.transform.position + (Vector3)(fieldPosition * GetScale());
    }

    GameObject GetEndObject(int id)
    {
        //returns the object at one end of a bridge (the core or a rune)
        if (id == RuneField.CoreID) return CorePointer;
        if (_runeViews.TryGetValue(id, out ElementItem view)) return view.gameObject;
        return null;
    }

    GameObject GetBridgePrefab()
    {
        //bridges use the bridge prefab set on the rune prefab, so there's nothing new to assign here
        if (elementPrefab == null) return null;
        if (elementPrefab.TryGetComponent(out ElementItem prefabRune) == false) return null;
        return prefabRune.GetBridgePrefab();
    }
    #endregion
}

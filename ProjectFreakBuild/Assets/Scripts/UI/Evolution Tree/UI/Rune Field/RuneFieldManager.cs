using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

//the 2D rune field UI. It's only a view now: every rule lives in RuneField (Scripts/Rune Field), this script draws the field
//and passes the player's drags to it. It kept its old name and fields so the scene and prefabs didn't need setting up again
public class RuneFieldManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far past its reach a rune has to be pulled before its bridges start tearing, in field units")]
    [SerializeField, Min(0f)] float _TearSlack = 2f;

    [Tooltip("Tint for a rune (or inventory button) dragged over a spot it can't go")]
    [SerializeField] Color _InvalidColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Popup title when saving would commit the shade to evolving")]
    [SerializeField] string _EvolveTitle = "Evolve?";

    [Tooltip("Popup message when the gate already has power, so saving evolves the shade right away. {0} = the form's name, {1} = the zone that locks")]
    [SerializeField, TextArea] string _EvolveNowMessage = "Saving will evolve this shade into {0} right away. Zone {1} locks for good: its runes and bridges can't be moved, removed or torn anymore.";

    [Tooltip("Popup message when the gate doesn't have power yet. {0} = the form's name, {1} = the zone that locks")]
    [SerializeField, TextArea] string _EvolveLaterMessage = "This shade will evolve into {0} as soon as the gate has power, locking zone {1} for good. Until then you can still take the rune out of the gate.";

    [FoldoutGroup("Zone Gizmos")]
    [Tooltip("Color of the zone the player can build in (and every ring outside play mode)")]
    [SerializeField] Color _OpenZoneColor = new Color(0.3f, 1f, 0.4f, 1f);

    [FoldoutGroup("Zone Gizmos")]
    [Tooltip("Color of zones the shade evolved out of, and the outline drawn around frozen runes")]
    [SerializeField] Color _FrozenZoneColor = new Color(0.4f, 0.8f, 1f, 1f);

    [FoldoutGroup("Zone Gizmos")]
    [Tooltip("Color of zones the shade can't reach yet")]
    [SerializeField] Color _LockedZoneColor = new Color(1f, 0.35f, 0.35f, 1f);

    [FoldoutGroup("Zone Gizmos")]
    [Tooltip("Color of the edge bleed circle: how far past the outer ring the background (and dragging) should go")]
    [SerializeField] Color _BleedColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("References")]
    [Tooltip("The core node object. It's the center (0, 0) of the field")]
    [SerializeField] GameObject CorePointer;

    [Tooltip("Every ability node object. A node's spot in this list is its index, which saved fields point at, so don't reorder it once fields are saved")]
    public List<GameObject> ListOfNodes;

    [Tooltip("Rune objects currently shown (filled at runtime). Runes placed in the scene by hand are removed on start, the field is built from the shade slot's data")]
    [SerializeField] private List<GameObject> ListOfRunes;

    [Tooltip("Rune prefab spawned for every rune on the field. Needs an ElementItem on its root. Its Bridge Prefab is used for the bridges")]
    [SerializeField] GameObject elementPrefab;

    [Tooltip("The popup that asks Save / Discard / Cancel when leaving or switching slots with unsaved changes. Without one, unsaved changes are thrown away (with a warning)")]
    [SerializeField] ConfirmPopup _Popup;

    [Tooltip("Optional. The UI save button: greyed out while there's nothing to save. Hook its On Click to SaveRuneSlot")]
    [SerializeField] Button _SaveButton;

    [Header("Events")]
    [Tooltip("Runs when the player leaves the rune field (after any save/discard question). Hook the exit timeline or whatever closes the field here")]
    [SerializeField] UnityEvent _OnLeave;

    [Tooltip("Runs when the open shade evolves (saving powered its gate). Hook evolve effects or a timeline here. The field has already reloaded with the new zone open")]
    [SerializeField] UnityEvent _OnEvolved;

    [Header("Runtime Data")]
    [Tooltip("True while the field has changes that aren't saved yet (read only)")]
    [SerializeField, ReadOnly] bool _HasUnsavedChanges;

    public event System.Action OnDraftChanged; //fires whenever the draft changes or a field loads, so the inventory list can update its counts

    //local variables
    RuneField _field; //the rules for the field being edited
    RuneFieldSettingsSO _settings; //shared rune field numbers, from the Shade Manager
    List<AbilityNodeEntry> _nodes; //the ability nodes as the rules see them, built from ListOfNodes
    int _slotIndex = -1; //which shade slot is open
    int _draggingRuneID = -1; //rune being dragged right now, or -1
    Vector2 _dragDropPosition; //where the dragged rune would be dropped (kept within reach of its bridges), in field units
    List<int> _dragStartBridges = new List<int>(); //what the dragged rune was bridged to when the drag started, put back if the drop is refused
    bool _unsavedBeforeDrag; //whether there were unsaved changes before the drag, put back if the drop is refused
    CoreNode _core;
    Dictionary<int, ElementItem> _runeViews = new Dictionary<int, ElementItem>(); //rune ID -> the object showing it
    List<NodeBridge> _bridgeViews = new List<NodeBridge>();
    PlayerInput _input; //for Escape (the UI map's Cancel action, which also covers a gamepad's back button)

    private void Awake()
    {
        _input = new PlayerInput();
    }

    private void OnEnable()
    {
        _input.Enable();
        _input.UI.Cancel.performed += OnCancelPressed;
    }

    private void OnDisable()
    {
        _input.UI.Cancel.performed -= OnCancelPressed;
        _input.Disable();
    }

    private void Start()
    {
        //removes the test runes placed in the scene, then opens whichever shade slot is selected
        if (CorePointer == null) { Debug.LogError($"Error! Core Pointer not assigned on {gameObject.name}", this); return; }
        if (elementPrefab == null) { Debug.LogError($"Error! Element Prefab not assigned on {gameObject.name}", this); return; }
        _core = CorePointer.GetComponent<CoreNode>();

        ClearHandPlacedRunes();

        if (ShadeManager._ShadeManager == null) { Debug.LogWarning($"Warning! No Shade Manager found, {gameObject.name} can't load a shade slot. Leaving the field empty...", this); return; }
        ShadeManager._ShadeManager.OnShadeEvolved += OnShadeEvolved; //subscribed here, not OnEnable, because the manager's Awake has run by Start
        LoadRuneField(ShadeManager._ShadeManager.GetShadeSelectionIndex());
    }

    private void OnDestroy()
    {
        //stops listening to the field and the Shade Manager so they don't call into a destroyed object
        if (_field != null) _field.OnFieldChanged -= OnFieldChanged;
        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.OnShadeEvolved -= OnShadeEvolved;
    }

    #region Loading And Saving
    public void LoadRuneField(int index)
    {
        //opens a shade slot's saved rune field. Unsaved changes on the open slot are thrown away without asking (SelectSlot asks first)
        if (ShadeManager._ShadeManager == null) { Debug.LogError($"Error! Shade Manager not found, can't load rune field on {gameObject.name}", this); return; }
        if (CorePointer == null) return; //already logged in Start
        if (ShadeManager._ShadeManager.IsValidSlot(index) == false) { Debug.LogWarning($"Warning! Shade slot {index} doesn't exist, {gameObject.name} isn't loading anything...", this); return; }
        _settings = ShadeManager._ShadeManager.GetRuneFieldSettings(); //before the nodes, the gate check needs the zone width
        if (_nodes == null) _nodes = BuildNodesFromScene();

        ShadeManager._ShadeManager.SetShadeSelection(index);
        _slotIndex = index;
        StartField(ShadeManager._ShadeManager.GetSavedRuneField(index)); //this is a copy, so editing it doesn't change the save until the player saves
    }

    [Button("Save Current Field")]
    public void SaveRuneSlot()
    {
        //saves the field onto the open shade slot. Hook the UI save button to this
        //if the save commits the shade to evolving (a rune newly plugged into a gate), the popup asks first
        TrySave(null);
    }

    void TrySave(System.Action afterSave)
    {
        //saves, asking first if a gate was newly plugged. afterSave runs only once the save actually happened (Cancel skips it)
        if (_field == null || ShadeManager._ShadeManager == null) return;

        int gate = GetNewGatePlug();
        if (gate == -1) { SaveNow(afterSave); return; }
        if (_Popup == null)
        {
            Debug.LogWarning($"Warning! No popup assigned on {gameObject.name} to confirm evolving, saving without asking...", this);
            SaveNow(afterSave);
            return;
        }

        _Popup.Show(_EvolveTitle, GetEvolveMessage(gate), "Save", () => SaveNow(afterSave), "Cancel", null);
    }

    void SaveNow(System.Action afterSave)
    {
        //the save itself. The Shade Manager changes the inventory by the difference, writes the slot, evolves the shade if a gate has power, and rebuilds its stats
        //afterwards the saved field is loaded again, so the draft starts over matching the slot (with the new zone open if it evolved)
        if (ShadeManager._ShadeManager.SaveRuneField(_slotIndex, _field) == false) return; //nothing saved, the draft stays as it is
        LoadRuneField(_slotIndex);
        afterSave?.Invoke();
    }

    int GetNewGatePlug()
    {
        //returns the gate the draft has a rune in that the saved field doesn't, or -1. Only a new commitment asks, so saving again later doesn't nag
        int gate = _field.GetPluggedGate();
        if (gate == -1) return -1;

        RuneFieldData saved = ShadeManager._ShadeManager.GetSavedRuneField(_slotIndex);
        for (int i = 0; i < saved._Plugs.Count; i++)
        {
            if (saved._Plugs[i]._NodeIndex == gate) return -1; //already committed in an earlier save
        }
        return gate;
    }

    string GetEvolveMessage(int gate)
    {
        //fills in the evolve popup's message: the form's name and the zone that locks
        ShadeEvolutionSO evolution = _field.GetNode(gate).GetEvolution();
        string formName = evolution != null ? evolution.name : "its next form";
        string message = _field.IsNodeActive(gate) ? _EvolveNowMessage : _EvolveLaterMessage;
        return string.Format(message, formName, _field.GetOpenZone()); //puts the name where {0} is and the zone where {1} is
    }

    void OnShadeEvolved(int slotIndex)
    {
        //the Shade Manager evolved a shade. If it's the one open here, reload so the new zone opens and the old one freezes, then run On Evolved
        if (slotIndex != _slotIndex) return;
        LoadRuneField(_slotIndex);
        _OnEvolved?.Invoke();
    }

    [Button("Discard Changes")]
    public void DiscardChanges()
    {
        //goes back to the slot's saved field. Runes the draft was using are free again, since they never left the inventory
        if (_slotIndex == -1) return;
        LoadRuneField(_slotIndex);
    }

    [Button("Test Clear")]
    public void ClearRuneField()
    {
        //empties the field being edited (nothing changes on the slot until the player saves)
        if (_field == null) return;
        StartField(new RuneFieldData());
        SetUnsavedChanges(true);
    }

    void StartField(RuneFieldData data)
    {
        //swaps in a new field and redraws everything
        if (_field != null) _field.OnFieldChanged -= OnFieldChanged;

        _field = new RuneField(data, _settings, _nodes, ShadeManager._ShadeManager.GetCorePower(_slotIndex), ShadeManager._ShadeManager.GetRank(_slotIndex));
        _field.OnFieldChanged += OnFieldChanged; //"+=" subscribes, so OnFieldChanged runs every time the field changes

        ClearViews();
        RefreshView(false); //node events don't fire on load, the nodes were already on/off when this field was saved
        SetUnsavedChanges(false);
    }
    #endregion

    #region Draft
    public void SelectSlot(int index)
    {
        //function the shade slot buttons call. With unsaved changes it asks Save / Discard / Cancel before switching
        if (index == _slotIndex) return; //already open
        AskAboutUnsavedChanges(() => LoadRuneField(index)); //"() => ..." stores the switch as a small function to run once the question is answered
    }

    public void RequestLeave()
    {
        //function for the close button (and Escape). With unsaved changes it asks Save / Discard / Cancel, then runs On Leave
        AskAboutUnsavedChanges(() => _OnLeave?.Invoke());
    }

    void AskAboutUnsavedChanges(System.Action continueWith)
    {
        //runs continueWith right away if nothing is unsaved. Otherwise shows the popup: Save or Discard then continue, Cancel stays put
        if (_HasUnsavedChanges == false) { continueWith?.Invoke(); return; }

        if (_Popup == null)
        {
            Debug.LogWarning($"Warning! No popup assigned on {gameObject.name}, throwing the unsaved changes away...", this);
            DiscardChanges();
            continueWith?.Invoke();
            return;
        }

        _Popup.Show("Unsaved Changes", "This rune field has changes that aren't saved yet.",
            "Save", () => TrySave(continueWith), //only continues once the save actually happened (it may ask about evolving first)
            "Discard", () => { DiscardChanges(); continueWith?.Invoke(); },
            "Cancel", null); //null: the button just closes the popup
    }

    void OnCancelPressed(InputAction.CallbackContext context)
    {
        //Escape: closes the popup if one is open, otherwise asks to leave
        if (_Popup != null && _Popup.IsOpen()) { _Popup.Close(); return; }
        RequestLeave();
    }

    void SetUnsavedChanges(bool unsaved)
    {
        //remembers whether there's anything to save, and greys the save button out when there isn't
        _HasUnsavedChanges = unsaved;
        if (_SaveButton != null) _SaveButton.interactable = unsaved;
    }

    public bool HasUnsavedChanges()
    {
        return _HasUnsavedChanges;
    }

    public int GetPendingUse(ElementItemSO element)
    {
        //how many of this rune the draft uses on top of the saved field (negative if the draft took some off)
        //only one draft exists for now. With more drafts later, this would add up every open draft
        if (_field == null || ShadeManager._ShadeManager == null) return 0;
        return _field.GetData().CountRunes(element) - ShadeManager._ShadeManager.GetSavedRuneCount(_slotIndex, element);
    }

    public int GetAvailable(ElementItemSO element)
    {
        //how many of this rune can still be placed: what the inventory has, minus what the draft is already using on top of the save
        if (InventoryManager._PlayerInventory == null) return int.MaxValue; //no inventory (testing the scene on its own), so no limit
        return InventoryManager._PlayerInventory.GetElementCount(element) - GetPendingUse(element);
    }
    #endregion

    #region Nodes
    List<AbilityNodeEntry> BuildNodesFromScene()
    {
        //makes the rules' node list from the node objects in this scene: where they sit, their unlock/lockout lists and their effects
        //each node's index is its spot in ListOfNodes. Missing objects are skipped, so the other nodes keep their indexes
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();

        for (int i = 0; i < ListOfNodes.Count; i++)
        {
            if (ListOfNodes[i] == null) { Debug.LogWarning($"Warning! Node {i} in List Of Nodes on {gameObject.name} is empty, skipping it...", this); continue; }

            AbilityNodeEntry entry = new AbilityNodeEntry();
            entry._NodeIndex = i;
            entry._Name = ListOfNodes[i].name;
            entry._Position = WorldToField(ListOfNodes[i].transform.position);

            if (ListOfNodes[i].TryGetComponent(out EvolutionNode node))
            {
                entry._Unlocks = NodesToIndexes(node.GetUnlockNodes());
                entry._Lockouts = NodesToIndexes(node.GetLockoutNodes());
                entry._Effects = RuneEffect.CloneList(node.GetEffects()); //copies, so the rules never share effect objects with the scene node
            }

            nodes.Add(entry);
        }

        //setup checks, once per scene. The lockouts get fixed for this run either way, the errors are so the scene gets fixed for good
        string lockoutProblems = RuneField.FixOneWayLockouts(nodes);
        if (lockoutProblems != "") Debug.LogError($"Error! One-way lockouts on {gameObject.name}:\n{lockoutProblems}", this);

        string gateProblems = RuneField.CheckGates(nodes, _settings);
        if (gateProblems != "") Debug.LogError($"Error! Evolution gates on {gameObject.name} need fixing:\n{gateProblems}", this);

        return nodes;
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
    #endregion

    #region Player Actions
    public void PlaceRuneFromInventory(ElementItemSO element, Vector2 screenPosition)
    {
        //called when an element is dragged from the inventory list onto the field
        //it only comes out of the inventory when the field is saved. Until then it counts as pending (see GetPendingUse)
        //a bad spot (overlapping something) just doesn't place it
        if (CanPlaceFromInventory(element, screenPosition) == false) return;
        if (_field.TryPlaceRune(element, WorldToField(screenPosition), out int runeID) == false) return; //snaps into a node if it's on one

        _field.ConnectNearby(runeID); //then bridges from where it ended up
        SetUnsavedChanges(true);
    }

    public bool CanPlaceFromInventory(ElementItemSO element, Vector2 screenPosition)
    {
        //checks a rune from the inventory could be dropped here: one is left to place, and the spot is clear. The inventory list uses this to tint while dragging
        if (_field == null || element == null) return false;
        if (GetAvailable(element) <= 0) return false;
        return _field.CanDropAt(RuneField.NoRuneID, WorldToField(screenPosition), out Vector2 finalPosition, out int nodeIndex);
    }

    public Color GetInvalidColor()
    {
        return _InvalidColor;
    }

    public void BeginRuneDrag(int runeID)
    {
        //the player picked up a rune. Its saved spot doesn't change until it's dropped somewhere allowed
        //frozen runes (in a zone the shade evolved out of) can't be picked up at all
        if (_field == null || _field.GetRune(runeID) == null) return;
        if (_field.IsRuneFrozen(runeID)) return;
        _draggingRuneID = runeID;
        _dragDropPosition = _field.GetPosition(runeID);
        _dragStartBridges = _field.GetConnections(runeID);
        _unsavedBeforeDrag = _HasUnsavedChanges;
    }

    public void DragRune(int runeID, Vector2 screenPosition)
    {
        //moves the rune's view with the pointer, kept within reach of its bridges, and starts tearing any bridge pulled too far
        //it shows where the rune would land (snapped into a node if it's on one), or tints it if it can't go there
        if (_field == null || _runeViews.ContainsKey(runeID) == false) return;
        if (runeID != _draggingRuneID) return; //the drag was refused when it started (like a frozen rune)

        Vector2 desired = WorldToField(screenPosition);
        _dragDropPosition = _field.ClampToBridges(runeID, desired);

        bool allowed = _field.CanDropAt(runeID, _dragDropPosition, out Vector2 landingPosition, out int nodeIndex);
        _runeViews[runeID].transform.position = FieldToWorld(allowed ? landingPosition : _dragDropPosition);
        _runeViews[runeID].ShowInvalid(allowed == false, _InvalidColor);

        UpdateBridgePositions();
        UpdateTearing(runeID, desired);
    }

    public void EndRuneDrag(int runeID, PointerEventData eventData)
    {
        //the player let go of a rune:
        //on the inventory list it comes off the field. On an allowed spot it moves there (plugging into a node if it's on one) and bridges to what's in reach
        //anywhere else it goes back to where it was, with any bridges torn during the drag put back, as if the drag never happened
        if (runeID != _draggingRuneID) return; //the drag was refused when it started, nothing moved
        StopAllTearing();
        _draggingRuneID = -1;
        if (_runeViews.TryGetValue(runeID, out ElementItem view)) view.ShowInvalid(false, _InvalidColor);
        if (_field == null || _field.GetRune(runeID) == null) return;

        if (IsOverInventoryList(eventData))
        {
            _field.RemoveRune(runeID); //takes its bridges and plug with it. It goes back to the inventory when the field is saved
            SetUnsavedChanges(true);
            return;
        }

        if (_field.TryDropRune(runeID, _dragDropPosition))
        {
            _field.ConnectNearby(runeID);
            SetUnsavedChanges(true);
        }
        else
        {
            _field.RestoreBridges(runeID, _dragStartBridges);
            SetUnsavedChanges(_unsavedBeforeDrag); //nothing changed, so the save button goes back to how it was
        }
        RefreshView(true); //puts the rune exactly where the data says
    }

    public void TearBridge(int idA, int idB)
    {
        //called by a bridge when its tear timer fills up
        if (_field == null) return;
        _field.Disconnect(idA, idB);
        SetUnsavedChanges(true);
    }

    bool IsOverInventoryList(PointerEventData eventData)
    {
        //checks if the pointer is over the inventory list (anything under an IngredientListWindow), so no extra setup is needed
        if (eventData == null || EventSystem.current == null) return false;

        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, hits); //every UI object under the pointer
        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].gameObject.GetComponentInParent<IngredientListWindow>() != null) return true;
        }
        return false;
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
            bool active = _field.IsNodeActive(i);
            node.ShowState(active, active == false && _field.IsNodeBlocked(i), fireNodeEvents); //locked look: can't take a rune (locked zone, frozen gate, or locked out)
        }

        if (_core != null) _core.ShowPower(_field.GetPowerLeft(), _field.GetMaxPower());
        OnDraftChanged?.Invoke();
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

    #region Zone Tools
    RuneFieldSettingsSO GetToolSettings()
    {
        //the settings the editor tools use: the field's own once it's loaded, otherwise (editor only) the settings asset found in the project
        if (_settings != null) return _settings;
#if UNITY_EDITOR
        return RuneFieldSettingsSO.FindInProject();
#else
        return null;
#endif
    }

    public bool SnapNodeToRing(Transform node, int ring)
    {
        //function the node's Snap buttons call: keeps the node's angle around the core and moves it exactly onto a ring
        //so every gate on a ring is the same distance from the core. Returns false (and logs why) if it couldn't
        RuneFieldSettingsSO settings = GetToolSettings();
        if (settings == null) { Debug.LogError($"Error! No Rune Field Settings asset found, {gameObject.name} can't snap nodes to rings", this); return false; }
        if (CorePointer == null) { Debug.LogError($"Error! Core Pointer not assigned on {gameObject.name}, there's no center to snap around", this); return false; }
        if (node == null) return false;

        if (ring < 1 || ring > settings.GetLastGateRing())
        {
            Debug.LogWarning($"Warning! Ring {ring} isn't a gate ring (gates go on rings 1 to {settings.GetLastGateRing()}, the last ring is the field's edge). {node.name} wasn't moved...", node);
            return false;
        }

        Vector2 fieldPosition = WorldToField(node.position);
        Vector2 direction = fieldPosition.sqrMagnitude > 0.0001f ? fieldPosition.normalized : Vector2.up; //a node sitting on the core has no angle, so it goes straight up

        Vector3 target = FieldToWorld(direction * settings.GetRingRadius(ring));
        target.z = node.position.z; //only slides it across the field, keeps its depth

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(node, "Snap Node To Ring"); //Ctrl+Z puts it back, and marks the scene as changed
#endif
        node.position = target;
        return true;
    }

    public int GetNextRingOut(Transform node)
    {
        //the closest ring further out than the node. A node already on a ring gets the next one. 0 if there are no settings
        RuneFieldSettingsSO settings = GetToolSettings();
        if (settings == null || CorePointer == null || node == null) return 0;
        return settings.GetNextRingOut(WorldToField(node.position).magnitude);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //draws the zone rings around the core in the scene view (and the game view while Gizmos is on). Editor only, nothing shows in a build
        //in play mode the rings are colored by state (frozen, open, locked) and frozen runes get an outline
        RuneFieldSettingsSO settings = GetToolSettings();
        if (settings == null || CorePointer == null) return;

        float scale = GetScale();
        Vector3 center = CorePointer.transform.position;
        Vector3 normal = transform.forward; //the field faces the camera, so the rings are drawn flat on it

        //Handles is the editor's drawing toolbox (like Gizmos, with flat circles and text labels)
        for (int ring = 1; ring <= settings._ZoneCount; ring++)
        {
            float radius = settings.GetRingRadius(ring) * scale;
            UnityEditor.Handles.color = GetZoneGizmoColor(ring);
            UnityEditor.Handles.DrawWireDisc(center, normal, radius);
            UnityEditor.Handles.Label(center + transform.up * radius, $"Zone {ring}");
        }

        UnityEditor.Handles.color = _BleedColor;
        UnityEditor.Handles.DrawWireDisc(center, normal, (settings.GetFieldEdge() + settings._EdgeBleed) * scale);

        if (_field == null) return;
        UnityEditor.Handles.color = _FrozenZoneColor;
        float runeRadius = settings._RuneSize * 0.5f * scale;
        foreach (KeyValuePair<int, ElementItem> entry in _runeViews)
        {
            if (entry.Value == null || _field.IsRuneFrozen(entry.Key) == false) continue;
            UnityEditor.Handles.DrawWireDisc(entry.Value.transform.position, normal, runeRadius);
        }
    }

    Color GetZoneGizmoColor(int zone)
    {
        //frozen (evolved out of), open (can build in) or locked (not reached yet). Outside play mode every ring is drawn open
        if (_field == null) return _OpenZoneColor;
        if (zone < _field.GetOpenZone()) return _FrozenZoneColor;
        if (zone == _field.GetOpenZone()) return _OpenZoneColor;
        return _LockedZoneColor;
    }
#endif
    #endregion
}

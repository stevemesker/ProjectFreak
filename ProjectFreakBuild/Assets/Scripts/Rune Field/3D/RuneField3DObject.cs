using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

//the 3D rune field. A view over the RuneField rules, like RuneFieldManager is for the 2D field
//this object is the field's center (0, 0) and its flat plane: field X runs along its right, field Y along its forward, in meters (see RuneFieldSettingsSO._FieldUnitsPerMeter)
//it opens a slot's field, draws it (core, nodes, runes, bridges), runs mouse dragging through the rules, and the draft / save flow (popups, slot switching, leaving)
[DefaultExecutionOrder(100)] //runs after the camera each frame, so a dragged rune follows the pointer with the camera already moved
[RequireComponent(typeof(RuneFieldPointerObject), typeof(RuneFieldLabelsObject))] //the pointer and the labels live on the same object
public class RuneField3DObject : MonoBehaviour, IRuneFieldView //IRuneFieldView: the inventory list and slot buttons talk to this the same way as the 2D field
{
    public static RuneField3DObject _Field3D; //the 3D field in this scene (one per scene)

    [Header("Data")]
    [Tooltip("Rune colors by element, until the rune shader exists. Elements not in the list use Default Rune Color")]
    [SerializeField] List<ElementColorEntry> _ElementColors = new List<ElementColorEntry>();

    [Tooltip("Color for runes whose element isn't in Element Colors")]
    [SerializeField] Color _DefaultRuneColor = Color.white;

    [Header("Settings")]
    [Tooltip("How far past its reach a rune has to be pulled before its bridges start tearing, in field units")]
    [SerializeField, Min(0f)] float _TearSlack = 2f;

    [Tooltip("Tint for a rune (or inventory button) dragged over a spot it can't go")]
    [SerializeField] Color _InvalidColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("How high a dragged rune floats above the field, in meters")]
    [SerializeField, Min(0f)] float _DragLift = 0.3f;

    [Tooltip("Extra room around a rune that still counts as pointing at it, in field units. Makes runes easier to grab")]
    [SerializeField, Min(0f)] float _PickPadding = 10f;

    [Tooltip("Popup title when saving would commit the shade to evolving")]
    [SerializeField] string _EvolveTitle = "Evolve?";

    [Tooltip("Popup message when the gate already has power, so saving evolves the shade right away. {0} = the form's name, {1} = the zone that locks")]
    [SerializeField, TextArea] string _EvolveNowMessage = "Saving will evolve this shade into {0} right away. Zone {1} locks for good: its runes and bridges can't be moved, removed or torn anymore.";

    [Tooltip("Popup message when the gate doesn't have power yet. {0} = the form's name, {1} = the zone that locks")]
    [SerializeField, TextArea] string _EvolveLaterMessage = "This shade will evolve into {0} as soon as the gate has power, locking zone {1} for good. Until then you can still take the rune out of the gate.";

    [Header("References")]
    [Tooltip("The core in the scene. It gets moved to this object's position (the field's center) on start")]
    [SerializeField] Core3DObject _Core;

    [Tooltip("Every ability node in the scene. A node's spot in this list is its index, which saved fields point at, so don't reorder it once fields are saved. Fill Node List (Tools) adds the ones under this object")]
    [SerializeField] List<Node3DObject> _Nodes = new List<Node3DObject>();

    [Tooltip("Spawned for every rune on the field (PFB_Rune_3D). Gets a Rune3DObject added if it doesn't have one")]
    [SerializeField] GameObject _RunePrefab;

    [Tooltip("Spawned for every bridge (PFB_Bridge_3D). Its mesh must start at its origin and run 1 m along +Z. Gets a Bridge3DObject added if it doesn't have one")]
    [SerializeField] GameObject _BridgePrefab;

    [Tooltip("The popup that asks Save / Discard / Cancel and confirms evolving. Without one, unsaved changes are thrown away (with a warning) and saving doesn't ask")]
    [SerializeField] ConfirmPopup _Popup;

    [Tooltip("Optional. The UI save button: greyed out while there's nothing to save. Hook its On Click to SaveRuneSlot")]
    [SerializeField] Button _SaveButton;

    [Tooltip("The side panel (inventory and shade slots) that gamepad Y / Tab opens and closes. Left empty, the one in the scene is found on start")]
    [SerializeField] SideWindowManager _SidePanel;

    [Header("Events")]
    [Tooltip("Runs when the player leaves the rune field (after any save / discard question). Hook whatever closes the field here")]
    [SerializeField] UnityEvent _OnLeave;

    [Tooltip("Runs when the open shade evolves, after the field reloads with the new zone open. Hook evolve effects here")]
    [SerializeField] UnityEvent _OnEvolved;

    [Header("Runtime Data")]
    [Tooltip("Which shade slot is open (read only)")]
    [SerializeField, ReadOnly] int _SlotIndex = -1;

    [Tooltip("True while the field has changes that aren't saved yet (read only)")]
    [SerializeField, ReadOnly] bool _HasUnsavedChanges;

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
    [Tooltip("Color of the edge bleed circle: how far past the outer ring dragging (and the camera) can go")]
    [SerializeField] Color _BleedColor = new Color(1f, 1f, 1f, 0.25f);

    public event System.Action OnDraftChanged; //fires whenever the draft changes or a field loads, so other UI (inventory counts, labels) can update

    //local variables
    RuneField _field; //the rules for the field being shown
    RuneFieldSettingsSO _settings; //shared rune field numbers, from the Shade Manager
    List<AbilityNodeEntry> _nodeEntries; //the nodes as the rules see them, built from _Nodes
    Dictionary<int, Rune3DObject> _runeViews = new Dictionary<int, Rune3DObject>(); //rune ID -> the object showing it
    List<Bridge3DObject> _bridgeViews = new List<Bridge3DObject>();
    RuneFieldPointerObject _pointer;
    PlayerInput _input;
    int _hoveredRuneID = RuneField.NoRuneID; //rune under the pointer, or NoRuneID
    int _draggingRuneID = RuneField.NoRuneID; //rune being dragged right now, or NoRuneID
    Vector2 _dragDropPosition; //where the dragged rune would be dropped (kept within reach of its bridges), in field units
    List<int> _dragStartBridges = new List<int>(); //what the dragged rune was bridged to when the drag started, put back if the drop is refused
    bool _unsavedBeforeDrag; //whether there were unsaved changes before the drag, put back if the drop is refused
    bool _carrying; //true while a gamepad is carrying a rune (A picks up, A again drops), false for a mouse drag (held)
    bool _panelFocus; //true while the side panel has gamepad focus (stick and A work the panel, not the field)
    ElementItemSO _carryElement; //a rune taken out of the inventory list with a gamepad, not placed yet. Null when not carrying one
    Rune3DObject _carryGhost; //the stand-in object showing that rune on the reticle
    int _carryStartFrame = -1; //the frame it was taken out, so the same A press doesn't place it straight away
    int _panelFocusEndFrame = -1; //the frame the panel lost focus, so the A press that closed it doesn't also grab a rune

    private void Reset()
    {
        //runs when the component is first added: finds the core and nodes under this object and the 3D prefabs, so there's nothing to drag in by hand
#if UNITY_EDITOR
        _Core = GetComponentInChildren<Core3DObject>(true);
        _RunePrefab = FindPrefab("PFB_Rune_3D");
        _BridgePrefab = FindPrefab("PFB_Bridge_3D");
        Node3DObject[] found = GetComponentsInChildren<Node3DObject>(true);
        for (int i = 0; i < found.Length; i++) _Nodes.Add(found[i]);
#endif
    }

    private void Awake()
    {
        //scene singleton: a second field in the same scene is a mistake, one in another scene is just an old scene still unloading
        if (_Field3D != null && _Field3D != this && _Field3D.gameObject.scene == gameObject.scene)
        {
            Debug.LogError($"Error! Two 3D rune fields in {gameObject.scene.name} ({_Field3D.gameObject.name} and {gameObject.name}), removing {gameObject.name}", this);
            Destroy(gameObject);
            return;
        }
        _Field3D = this;

        _input = new PlayerInput();
        _pointer = GetComponent<RuneFieldPointerObject>();
        if (_pointer == null) _pointer = gameObject.AddComponent<RuneFieldPointerObject>(); //RequireComponent adds it in the editor, this covers objects set up before that
        if (GetComponent<RuneFieldLabelsObject>() == null) gameObject.AddComponent<RuneFieldLabelsObject>();
    }

    private void OnEnable()
    {
        _input.RuneField.Enable();
        _input.RuneField.Back.performed += OnBackPressed;
        _input.RuneField.TogglePanel.performed += OnTogglePanelPressed;
    }

    private void OnDisable()
    {
        _input.RuneField.Back.performed -= OnBackPressed;
        _input.RuneField.TogglePanel.performed -= OnTogglePanelPressed;
        _input.RuneField.Disable();
    }

    private void Start()
    {
        //checks references, puts the core at the center, then opens whichever shade slot is selected
        if (_RunePrefab == null) { Debug.LogError($"Error! Rune Prefab not assigned on {gameObject.name}", this); return; }
        if (_BridgePrefab == null) { Debug.LogError($"Error! Bridge Prefab not assigned on {gameObject.name}", this); return; }
        if (_Core != null) _Core.transform.position = transform.position;
        if (_SidePanel == null) _SidePanel = FindObjectOfType<SideWindowManager>(true); //one-time search on start, so there's nothing to drag in
        SetUnsavedChanges(false);

        if (ShadeManager._ShadeManager == null) { Debug.LogWarning($"Warning! No Shade Manager found, {gameObject.name} can't load a shade slot. Leaving the field empty...", this); return; }
        ShadeManager._ShadeManager.OnShadeEvolved += OnShadeEvolved; //subscribed here, not OnEnable, because the manager's Awake has run by Start
        LoadRuneField(ShadeManager._ShadeManager.GetShadeSelectionIndex());
    }

    private void OnDestroy()
    {
        //stops listening so nothing calls into a destroyed object, and clears the singleton if it's this one
        if (_field != null) _field.OnFieldChanged -= OnFieldChanged;
        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.OnShadeEvolved -= OnShadeEvolved;
        EndCarryFromInventory();
        if (_Field3D == this) _Field3D = null;
    }

    private void LateUpdate()
    {
        //works out which rune is under the pointer, then starts, moves or ends a drag
        //mouse: hold the left button to drag. Gamepad: A picks a rune up, A again drops it, X sends it back to the inventory, B puts it back (OnBackPressed)
        //LateUpdate (and running after the camera) so the rune is placed with this frame's camera
        if (_field == null || _pointer == null) return;

        UpdateHover();
        UpdatePanelFocus();

        InputAction select = _input.RuneField.Select;
        bool pressed = select.WasPressedThisFrame() && select.activeControl != null;
        bool gamepadPress = pressed && select.activeControl.device is Gamepad;
        bool mousePress = pressed && select.activeControl.device is Mouse && _pointer.IsUsingMouse(); //IsUsingMouse leaves out the reticle's virtual mouse
        bool returnPress = _input.RuneField.ReturnToInventory.WasPressedThisFrame();

        if (_carryElement != null) { UpdateCarryFromInventory(gamepadPress, returnPress); return; }
        if (_panelFocus || Time.frameCount == _panelFocusEndFrame) return; //the side panel has the gamepad: the stick and A work the panel, not the field

        if (_draggingRuneID == RuneField.NoRuneID)
        {
            if (returnPress) { ReturnRuneToInventory(_hoveredRuneID); return; }

            bool canGrab = _hoveredRuneID != RuneField.NoRuneID && _pointer.IsOverCanvas() == false && _pointer.IsVirtualButtonDown() == false;
            if ((mousePress || gamepadPress) && canGrab)
            {
                BeginRuneDrag(_hoveredRuneID);
                _carrying = gamepadPress && _draggingRuneID != RuneField.NoRuneID;
            }
            return;
        }

        if (_carrying)
        {
            if (returnPress) ReturnRuneToInventory(_draggingRuneID);
            else if (gamepadPress) EndRuneDrag();
            else DragRune();
            return;
        }

        if (select.IsPressed()) DragRune();
        else EndRuneDrag();
    }

    #region Loading And Saving
    public void LoadRuneField(int index)
    {
        //opens a shade slot's saved rune field. Unsaved changes on the open slot are thrown away without asking (SelectSlot asks first)
        if (ShadeManager._ShadeManager == null) { Debug.LogError($"Error! Shade Manager not found, can't load rune field on {gameObject.name}", this); return; }
        if (ShadeManager._ShadeManager.IsValidSlot(index) == false) { Debug.LogWarning($"Warning! Shade slot {index} doesn't exist, {gameObject.name} isn't loading anything...", this); return; }
        _settings = ShadeManager._ShadeManager.GetRuneFieldSettings(); //before the nodes, the gate check needs the zone width
        if (_nodeEntries == null) _nodeEntries = BuildNodesFromScene();

        ShadeManager._ShadeManager.SetShadeSelection(index);
        _SlotIndex = index;
        StartField(ShadeManager._ShadeManager.GetSavedRuneField(index)); //this is a copy, so editing it doesn't change the save until the player saves
    }

    void StartField(RuneFieldData data)
    {
        //swaps in a new field and redraws everything
        if (_field != null) _field.OnFieldChanged -= OnFieldChanged;

        _field = new RuneField(data, _settings, _nodeEntries, ShadeManager._ShadeManager.GetCorePower(_SlotIndex), ShadeManager._ShadeManager.GetRank(_SlotIndex));
        _field.OnFieldChanged += OnFieldChanged;

        _draggingRuneID = RuneField.NoRuneID;
        _hoveredRuneID = RuneField.NoRuneID;
        _carrying = false;
        EndCarryFromInventory();
        ClearViews();
        RefreshView(false); //node events don't fire on load, the nodes were already on/off when this field was saved
        SetUnsavedChanges(false);
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
        if (ShadeManager._ShadeManager.SaveRuneField(_SlotIndex, _field) == false) return; //nothing saved, the draft stays as it is
        LoadRuneField(_SlotIndex);
        afterSave?.Invoke();
    }

    int GetNewGatePlug()
    {
        //returns the gate the draft has a rune in that the saved field doesn't, or -1. Only a new commitment asks, so saving again later doesn't nag
        int gate = _field.GetPluggedGate();
        if (gate == -1) return -1;

        RuneFieldData saved = ShadeManager._ShadeManager.GetSavedRuneField(_SlotIndex);
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

    [Button("Discard Changes")]
    public void DiscardChanges()
    {
        //goes back to the slot's saved field. Runes the draft was using are free again, since they never left the inventory
        if (_SlotIndex == -1) return;
        LoadRuneField(_SlotIndex);
    }

    [Button("Test Clear")]
    public void ClearRuneField()
    {
        //empties the field being edited (nothing changes on the slot until the player saves)
        if (_field == null) return;
        StartField(new RuneFieldData());
        SetUnsavedChanges(true);
    }

    public void RefreshCorePower()
    {
        //gives the open field the slot's current core power (after a level change). The draft keeps its unsaved changes
        if (_field == null || ShadeManager._ShadeManager == null) return;
        _field.SetMaxPower(ShadeManager._ShadeManager.GetCorePower(_SlotIndex)); //recalculates the field, which redraws it
    }

    void OnShadeEvolved(int slotIndex)
    {
        //the Shade Manager evolved a shade. If it's the one open here, reload so the new zone opens and the old one freezes
        if (slotIndex != _SlotIndex) return;
        LoadRuneField(_SlotIndex);
        _OnEvolved?.Invoke();
    }
    #endregion

    #region Draft
    public void SelectSlot(int index)
    {
        //function the shade slot buttons call. With unsaved changes it asks Save / Discard / Cancel before switching
        if (index == _SlotIndex) return; //already open
        AskAboutUnsavedChanges(() => LoadRuneField(index)); //"() => ..." stores the switch as a small function to run once the question is answered
    }

    public void RequestLeave()
    {
        //function for the close button (and Escape / B). With unsaved changes it asks Save / Discard / Cancel, then runs On Leave
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

    void OnBackPressed(InputAction.CallbackContext context)
    {
        //Escape / B: puts back a rune being carried or dragged, closes the side panel if it has gamepad focus, closes the popup if one is open, otherwise asks to leave
        if (_carryElement != null) { EndCarryFromInventory(); return; }
        if (_draggingRuneID != RuneField.NoRuneID) { CancelRuneDrag(); return; }
        if (_panelFocus && _SidePanel != null && _SidePanel.IsOpen()) { _SidePanel.ToggleSidePanel(); SetPanelFocus(false); return; }
        if (_Popup != null && _Popup.IsOpen()) { _Popup.Close(); return; }
        RequestLeave();
    }

    void OnTogglePanelPressed(InputAction.CallbackContext context)
    {
        //gamepad Y / Tab: opens or closes the side panel (inventory and shade slots). Not while a popup is open
        if (_SidePanel == null) return;
        if (_Popup != null && _Popup.IsOpen()) return;
        if (_carryElement != null) EndCarryFromInventory(); //reopening the list puts a carried rune back
        _SidePanel.ToggleSidePanel();

        bool gamepad = context.control != null && context.control.device is Gamepad;
        SetPanelFocus(gamepad && _SidePanel.IsOpen()); //on gamepad the panel takes focus, so something in it is selected right away
    }

    void SetPanelFocus(bool focus)
    {
        //gives the side panel gamepad focus (selects its first item) or takes it away (clears the selection)
        if (_panelFocus && focus == false) _panelFocusEndFrame = Time.frameCount;
        _panelFocus = focus;
        if (EventSystem.current == null) return;
        if (focus) SelectFirstInPanel();
        else if (_SidePanel != null && _SidePanel.Contains(EventSystem.current.currentSelectedGameObject)) EventSystem.current.SetSelectedGameObject(null);
    }

    void SelectFirstInPanel()
    {
        //selects the first rune in the open list, or the first tab
        if (_SidePanel == null || EventSystem.current == null) return;
        Selectable first = _SidePanel.GetFirstSelectable();
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    void UpdatePanelFocus()
    {
        //drops panel focus when the panel closes (a tab pressed again) or the mouse is used, and selects something again if the selection got lost (like the list refreshing)
        if (_panelFocus == false) return;
        if (_SidePanel == null || _SidePanel.IsOpen() == false || _pointer.IsUsingMouse()) { SetPanelFocus(false); return; }
        if (EventSystem.current == null) return;
        if (_SidePanel.Contains(EventSystem.current.currentSelectedGameObject) == false) SelectFirstInPanel();
    }

    public bool IsPanelFocused()
    {
        //true while the side panel has gamepad focus. The camera stops moving with the stick and the pointer stops clicking the canvas
        return _panelFocus;
    }

    void SetUnsavedChanges(bool unsaved)
    {
        //remembers whether there's anything to save, and greys the save button out when there isn't
        _HasUnsavedChanges = unsaved;
        if (_SaveButton != null) _SaveButton.interactable = unsaved;
    }

    public int GetPendingUse(ElementItemSO element)
    {
        //how many of this rune the draft uses on top of the saved field (negative if the draft took some off)
        if (_field == null || ShadeManager._ShadeManager == null) return 0;
        return _field.GetData().CountRunes(element) - ShadeManager._ShadeManager.GetSavedRuneCount(_SlotIndex, element);
    }

    public int GetAvailable(ElementItemSO element)
    {
        //how many of this rune can still be placed: what the inventory has, minus what the draft is already using on top of the save
        if (InventoryManager._PlayerInventory == null) return int.MaxValue; //no inventory (testing the scene on its own), so no limit
        return InventoryManager._PlayerInventory.GetElementCount(element) - GetPendingUse(element);
    }
    #endregion

    #region Player Actions
    void UpdateHover()
    {
        //finds the rune under the pointer (not while over the canvas), and lights it up
        int hovered = RuneField.NoRuneID;
        if (_draggingRuneID == RuneField.NoRuneID && _pointer.IsOverCanvas() == false && _pointer.TryGetFieldPoint(out Vector3 point)) hovered = FindRuneAt(WorldToField(point));
        if (hovered == _hoveredRuneID) return;

        if (_runeViews.TryGetValue(_hoveredRuneID, out Rune3DObject oldView) && oldView != null) oldView.ShowHover(false);
        _hoveredRuneID = hovered;
        if (_runeViews.TryGetValue(_hoveredRuneID, out Rune3DObject newView) && newView != null) newView.ShowHover(true);
    }

    int FindRuneAt(Vector2 fieldPosition)
    {
        //the rune whose footprint (plus Pick Padding) covers this spot, closest one first. NoRuneID if none
        //math instead of colliders: the rules already know where every rune is, so no colliders or layers to set up
        if (_settings == null) return RuneField.NoRuneID;
        float reach = _settings._RuneSize * 0.5f + _PickPadding;
        int closest = RuneField.NoRuneID;
        float closestDistance = reach;

        List<PlacedRuneEntry> runes = _field.GetData()._Runes;
        for (int i = 0; i < runes.Count; i++)
        {
            float distance = Vector2.Distance(runes[i]._Position, fieldPosition);
            if (distance > closestDistance) continue;
            closestDistance = distance;
            closest = runes[i]._ID;
        }
        return closest;
    }

    public bool IsHoveringRune()
    {
        //true while the pointer is on a rune or dragging one. The camera checks this so clicking a rune doesn't also pan
        return _hoveredRuneID != RuneField.NoRuneID || _draggingRuneID != RuneField.NoRuneID;
    }

    public bool IsDraggingRune()
    {
        return _draggingRuneID != RuneField.NoRuneID;
    }

    void BeginRuneDrag(int runeID)
    {
        //the player picked up a rune. Its saved spot doesn't change until it's dropped somewhere allowed
        //frozen runes (in a zone the shade evolved out of) can't be picked up at all
        if (_field.GetRune(runeID) == null) return;
        if (_field.IsRuneFrozen(runeID)) return;

        _draggingRuneID = runeID;
        _dragDropPosition = _field.GetPosition(runeID);
        _dragStartBridges = _field.GetConnections(runeID);
        _unsavedBeforeDrag = _HasUnsavedChanges;
        if (_runeViews.TryGetValue(runeID, out Rune3DObject view) && view != null) view.ShowHover(false);
        _hoveredRuneID = RuneField.NoRuneID;
    }

    void DragRune()
    {
        //moves the rune's view with the pointer, kept within reach of its bridges, and starts tearing any bridge pulled too far
        //it shows where the rune would land (snapped into a node if it's on one), or tints it if it can't go there
        if (_runeViews.TryGetValue(_draggingRuneID, out Rune3DObject view) == false || view == null) return;
        if (_pointer.TryGetFieldPoint(out Vector3 point) == false) return; //pointing at the sky, the rune stays where it was

        Vector2 desired = WorldToField(point);
        _dragDropPosition = _field.ClampToBridges(_draggingRuneID, desired);

        bool allowed = _field.CanDropAt(_draggingRuneID, _dragDropPosition, out Vector2 landingPosition, out int nodeIndex);
        bool overList = _pointer.IsOverInventoryList(); //over the list, letting go takes it off the field, so it isn't shown as invalid
        view.transform.position = FieldToWorld(allowed ? landingPosition : _dragDropPosition) + transform.up * _DragLift;
        view.ShowInvalid(allowed == false && overList == false, _InvalidColor);

        UpdateBridgePositions();
        UpdateTearing(_draggingRuneID, desired);
    }

    void EndRuneDrag()
    {
        //the player let go of a rune:
        //on the inventory list it comes off the field. On an allowed spot it moves there (plugging into a node if it's on one) and bridges to what's in reach
        //anywhere else it goes back to where it was, with any bridges torn during the drag put back, as if the drag never happened
        int runeID = _draggingRuneID;
        StopAllTearing();
        _draggingRuneID = RuneField.NoRuneID;
        _carrying = false;
        if (_runeViews.TryGetValue(runeID, out Rune3DObject view) && view != null) view.ShowInvalid(false, _InvalidColor);
        if (_field.GetRune(runeID) == null) return;

        if (_pointer.IsOverInventoryList())
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

    void CancelRuneDrag()
    {
        //Escape / B while dragging: the rune goes back with any bridges torn during the drag, as if the drag never happened
        int runeID = _draggingRuneID;
        StopAllTearing();
        _draggingRuneID = RuneField.NoRuneID;
        _carrying = false;
        if (_runeViews.TryGetValue(runeID, out Rune3DObject view) && view != null) view.ShowInvalid(false, _InvalidColor);
        if (_field.GetRune(runeID) == null) return;

        _field.RestoreBridges(runeID, _dragStartBridges);
        SetUnsavedChanges(_unsavedBeforeDrag);
        RefreshView(true);
    }

    public void BeginCarryFromInventory(ElementItemSO element)
    {
        //A on a rune in the inventory list (gamepad, from ElementDataObject): closes the panel and puts the rune on the reticle. A again places it, B or X put it back
        //only while the panel has gamepad focus, so mouse clicks on the list are left alone (the mouse drags instead)
        if (_panelFocus == false || element == null || _field == null) return;
        if (GetAvailable(element) <= 0) return;
        if (_draggingRuneID != RuneField.NoRuneID) return;

        if (_SidePanel != null && _SidePanel.IsOpen()) _SidePanel.ToggleSidePanel();
        SetPanelFocus(false);
        EndCarryFromInventory();

        GameObject spawned = Instantiate(_RunePrefab, transform.position, transform.rotation, transform);
        spawned.name = $"Carried Rune ({element.name})";
        _carryGhost = spawned.GetComponent<Rune3DObject>();
        if (_carryGhost == null) _carryGhost = spawned.AddComponent<Rune3DObject>();
        _carryGhost.Setup(RuneField.NoRuneID, element, GetElementColor(element));
        _carryGhost.ShowPower(true, element.powerNeeded);

        _carryElement = element;
        _carryStartFrame = Time.frameCount;
    }

    void UpdateCarryFromInventory(bool gamepadPress, bool returnPress)
    {
        //moves the carried rune with the reticle, red over a bad spot. A places it (if the spot is allowed), X puts it back
        if (returnPress) { EndCarryFromInventory(); return; }
        if (_carryGhost == null || _pointer.TryGetFieldPoint(out Vector3 point) == false) return;

        Vector2 position = WorldToField(point);
        bool allowed = _field.CanDropAt(RuneField.NoRuneID, position, out Vector2 landingPosition, out int nodeIndex); //called first, so landingPosition always gets a value
        if (GetAvailable(_carryElement) <= 0) allowed = false;
        _carryGhost.transform.position = FieldToWorld(allowed ? landingPosition : position) + transform.up * _DragLift;
        _carryGhost.ShowInvalid(allowed == false, _InvalidColor);

        if (gamepadPress == false || Time.frameCount == _carryStartFrame || allowed == false) return;
        ElementItemSO element = _carryElement;
        EndCarryFromInventory();
        if (_field.TryPlaceRune(element, position, out int runeID) == false) return; //snaps into a node if it's on one
        _field.ConnectNearby(runeID);
        SetUnsavedChanges(true);
    }

    void EndCarryFromInventory()
    {
        //stops carrying a rune from the list (placed or put back): the stand-in object goes away
        if (_carryGhost != null) Destroy(_carryGhost.gameObject);
        _carryGhost = null;
        _carryElement = null;
    }

    public bool IsCarryingFromInventory()
    {
        return _carryElement != null;
    }

    void ReturnRuneToInventory(int runeID)
    {
        //gamepad X: takes a rune off the field (the hovered one, or the one being carried). It goes back to the inventory when the field is saved
        //frozen runes can't be taken off
        if (runeID == RuneField.NoRuneID || _field.GetRune(runeID) == null) return;
        if (_field.IsRuneFrozen(runeID)) return;

        if (runeID == _draggingRuneID)
        {
            StopAllTearing();
            _draggingRuneID = RuneField.NoRuneID;
            _carrying = false;
        }
        if (runeID == _hoveredRuneID) _hoveredRuneID = RuneField.NoRuneID;

        _field.RemoveRune(runeID); //takes its bridges and plug with it
        SetUnsavedChanges(true);
    }

    public void PlaceRuneFromInventory(ElementItemSO element, Vector2 screenPosition)
    {
        //called by the inventory list (ElementDataObject) when a rune is dragged from it onto the field
        //it only comes out of the inventory when the field is saved. Until then it counts as pending (see GetPendingUse)
        if (CanPlaceFromInventory(element, screenPosition) == false) return;
        if (_pointer.TryScreenToField(screenPosition, out Vector3 point) == false) return;
        if (_field.TryPlaceRune(element, WorldToField(point), out int runeID) == false) return; //snaps into a node if it's on one

        _field.ConnectNearby(runeID); //then bridges from where it ended up
        SetUnsavedChanges(true);
    }

    public bool CanPlaceFromInventory(ElementItemSO element, Vector2 screenPosition)
    {
        //checks a rune from the inventory could be dropped here: one is left to place, and the spot is clear. The inventory list uses this to tint while dragging
        if (_field == null || element == null || _pointer == null) return false;
        if (GetAvailable(element) <= 0) return false;
        if (_pointer.TryScreenToField(screenPosition, out Vector3 point) == false) return false;
        return _field.CanDropAt(RuneField.NoRuneID, WorldToField(point), out Vector2 finalPosition, out int nodeIndex);
    }

    public Color GetInvalidColor()
    {
        return _InvalidColor;
    }

    public void TearBridge(int idA, int idB)
    {
        //called by a bridge when its tear timer fills up
        if (_field == null) return;
        _field.Disconnect(idA, idB);
        SetUnsavedChanges(true);
    }
    #endregion

    #region Getters
    public RuneField GetField() { return _field; }
    public Core3DObject GetCore() { return _Core; }
    public int GetHoveredRuneID() { return _hoveredRuneID; }
    public int GetDraggingRuneID() { return _draggingRuneID; }

    public Rune3DObject GetRuneView(int runeID)
    {
        //the object showing a rune, or null
        _runeViews.TryGetValue(runeID, out Rune3DObject view);
        return view;
    }
    public RuneFieldSettingsSO GetSettings() { return _settings; }
    public bool HasUnsavedChanges() { return _HasUnsavedChanges; }
    #endregion

    #region Units
    public float GetUnitsPerMeter()
    {
        //how many field units fit in a meter. Falls back to 100 if there are no settings yet
        RuneFieldSettingsSO settings = GetToolSettings();
        return settings != null ? settings._FieldUnitsPerMeter : 100f;
    }

    public Vector3 FieldToWorld(Vector2 fieldPosition)
    {
        //turns a spot in field units (core at 0, 0) into a world position on the field's plane
        float unitsPerMeter = GetUnitsPerMeter();
        return transform.position + transform.right * (fieldPosition.x / unitsPerMeter) + transform.forward * (fieldPosition.y / unitsPerMeter);
    }

    public Vector2 WorldToField(Vector3 worldPosition)
    {
        //turns a world position into field units, flattened onto the field's plane (height is ignored)
        //Dot measures how far the offset goes along each of the field's directions
        Vector3 offset = worldPosition - transform.position;
        float unitsPerMeter = GetUnitsPerMeter();
        return new Vector2(Vector3.Dot(offset, transform.right), Vector3.Dot(offset, transform.forward)) * unitsPerMeter;
    }
    #endregion

    #region Nodes
    List<AbilityNodeEntry> BuildNodesFromScene()
    {
        //makes the rules' node list from the node objects: where they sit, their unlock/lockout lists and their effects
        //each node's index is its spot in _Nodes. Empty spots are skipped, so the other nodes keep their indexes
        List<AbilityNodeEntry> nodes = new List<AbilityNodeEntry>();

        for (int i = 0; i < _Nodes.Count; i++)
        {
            if (_Nodes[i] == null) { Debug.LogWarning($"Warning! Node {i} in Nodes on {gameObject.name} is empty, skipping it...", this); continue; }

            AbilityNodeEntry entry = new AbilityNodeEntry();
            entry._NodeIndex = i;
            entry._Name = _Nodes[i].name;
            entry._Position = WorldToField(_Nodes[i].transform.position);
            entry._Unlocks = NodesToIndexes(_Nodes[i].GetUnlockNodes());
            entry._Lockouts = NodesToIndexes(_Nodes[i].GetLockoutNodes());
            entry._Effects = RuneEffect.CloneList(_Nodes[i].GetEffects()); //copies, so the rules never share effect objects with the scene node
            nodes.Add(entry);
        }

        //setup checks, once per scene. The lockouts get fixed for this run either way, the errors are so the scene gets fixed for good
        string lockoutProblems = RuneField.FixOneWayLockouts(nodes);
        if (lockoutProblems != "") Debug.LogError($"Error! One-way lockouts on {gameObject.name}:\n{lockoutProblems}", this);

        string gateProblems = RuneField.CheckGates(nodes, _settings);
        if (gateProblems != "") Debug.LogError($"Error! Evolution gates on {gameObject.name} need fixing:\n{gateProblems}", this);

        return nodes;
    }

    List<int> NodesToIndexes(List<Node3DObject> nodes)
    {
        //turns a list of node objects into their indexes in _Nodes. Nodes that aren't in the list are skipped
        List<int> indexes = new List<int>();
        if (nodes == null) return indexes;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            int index = _Nodes.IndexOf(nodes[i]);
            if (index != -1) indexes.Add(index);
        }
        return indexes;
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

        foreach (KeyValuePair<int, Rune3DObject> entry in _runeViews)
        {
            entry.Value.ShowPower(_field.IsRunePowered(entry.Key), _field.GetPowerNeeded(entry.Key));
            entry.Value.ShowFrozen(_field.IsRuneFrozen(entry.Key));
        }

        for (int i = 0; i < _Nodes.Count; i++)
        {
            if (_Nodes[i] == null) continue;
            bool active = _field.IsNodeActive(i);
            _Nodes[i].ShowState(active, active == false && _field.IsNodeBlocked(i), fireNodeEvents); //locked look: can't take a rune (locked zone, frozen gate, or locked out)
        }

        if (_Core != null) _Core.ShowPower(_field.GetPowerLeft(), _field.GetMaxPower());
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
            if (_runeViews[shownIDs[i]] != null) Destroy(_runeViews[shownIDs[i]].gameObject);
            _runeViews.Remove(shownIDs[i]);
        }

        for (int i = 0; i < runes.Count; i++)
        {
            PlacedRuneEntry rune = runes[i];
            if (_runeViews.ContainsKey(rune._ID) == false)
            {
                Rune3DObject view = SpawnRune(rune);
                if (view == null) continue;
                _runeViews.Add(rune._ID, view);
            }
            if (rune._ID != _draggingRuneID) _runeViews[rune._ID].transform.SetPositionAndRotation(FieldToWorld(rune._Position), transform.rotation); //the dragged rune follows the pointer instead
        }
    }

    Rune3DObject SpawnRune(PlacedRuneEntry rune)
    {
        //spawns one rune object and sets it up. The script gets added if the prefab doesn't have it yet
        GameObject spawned = Instantiate(_RunePrefab, FieldToWorld(rune._Position), transform.rotation, transform);
        spawned.name = $"Rune {rune._ID} ({(rune._Element != null ? rune._Element.name : "no element")})";

        Rune3DObject view = spawned.GetComponent<Rune3DObject>();
        if (view == null) view = spawned.AddComponent<Rune3DObject>();
        view.Setup(rune._ID, rune._Element, GetElementColor(rune._Element));
        return view;
    }

    void RebuildBridgeViews()
    {
        //throws away every bridge object and makes one for each bridge in the data. There are only ever a few dozen, so this is cheap
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            if (_bridgeViews[i] != null) Destroy(_bridgeViews[i].gameObject);
        }
        _bridgeViews.Clear();

        List<RuneBridgeEntry> bridges = _field.GetData()._Bridges;
        for (int i = 0; i < bridges.Count; i++)
        {
            Transform endA = GetEndTransform(bridges[i]._A);
            Transform endB = GetEndTransform(bridges[i]._B);
            if (endA == null || endB == null) continue;

            GameObject spawned = Instantiate(_BridgePrefab, endA.position, transform.rotation, transform);
            spawned.name = $"Bridge {bridges[i]._A} - {bridges[i]._B}";

            Bridge3DObject bridge = spawned.GetComponent<Bridge3DObject>();
            if (bridge == null) bridge = spawned.AddComponent<Bridge3DObject>();
            bridge.Setup(bridges[i]._A, bridges[i]._B, endA, endB, transform.up);
            _bridgeViews.Add(bridge);
        }

        UpdateBridgePositions();
    }

    void UpdateBridgePositions()
    {
        //stretches every bridge between its two ends
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            if (_bridgeViews[i] != null) _bridgeViews[i].UpdatePosition();
        }
    }

    void UpdateTearing(int runeID, Vector2 desiredPosition)
    {
        //starts tearing every bridge on this rune that's being pulled past its reach, and stops the rest
        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            Bridge3DObject bridge = _bridgeViews[i];
            if (bridge == null || bridge.Touches(runeID) == false) continue;

            float stretch = _field.GetBridgeStretch(runeID, desiredPosition, bridge.GetOtherEnd(runeID));
            if (stretch > _TearSlack) bridge.StartTearing(stretch);
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
        foreach (KeyValuePair<int, Rune3DObject> entry in _runeViews)
        {
            if (entry.Value != null) Destroy(entry.Value.gameObject);
        }
        _runeViews.Clear();

        for (int i = 0; i < _bridgeViews.Count; i++)
        {
            if (_bridgeViews[i] != null) Destroy(_bridgeViews[i].gameObject);
        }
        _bridgeViews.Clear();
    }

    Transform GetEndTransform(int id)
    {
        //the object at one end of a bridge: the core (or this object if there's no core) or a rune
        if (id == RuneField.CoreID) return _Core != null ? _Core.transform : transform;
        if (_runeViews.TryGetValue(id, out Rune3DObject view) && view != null) return view.transform;
        return null;
    }

    Color GetElementColor(ElementItemSO element)
    {
        //the rune's color from Element Colors, or the default
        if (element == null) return _DefaultRuneColor;
        for (int i = 0; i < _ElementColors.Count; i++)
        {
            if (_ElementColors[i] != null && _ElementColors[i]._Element == element.element) return _ElementColors[i]._Color;
        }
        return _DefaultRuneColor;
    }
    #endregion

    #region Tools
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
        if (node == null) return false;

        if (ring < 1 || ring > settings.GetLastGateRing())
        {
            Debug.LogWarning($"Warning! Ring {ring} isn't a gate ring (gates go on rings 1 to {settings.GetLastGateRing()}, the last ring is the field's edge). {node.name} wasn't moved...", node);
            return false;
        }

        Vector2 fieldPosition = WorldToField(node.position);
        Vector2 direction = fieldPosition.sqrMagnitude > 0.0001f ? fieldPosition.normalized : Vector2.up; //a node sitting on the core has no angle, so it goes straight "up" the field

        Vector3 target = FieldToWorld(direction * settings.GetRingRadius(ring));
        target += transform.up * Vector3.Dot(node.position - transform.position, transform.up); //keeps the node's height above the field

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
        if (settings == null || node == null) return 0;
        return settings.GetNextRingOut(WorldToField(node.position).magnitude);
    }

    public int GetPreviousRingIn(Transform node)
    {
        //the closest ring further in than the node. A node already on a ring gets the one before it. 0 if there's no ring inside it (or no settings)
        RuneFieldSettingsSO settings = GetToolSettings();
        if (settings == null || node == null) return 0;
        return settings.GetPreviousRingIn(WorldToField(node.position).magnitude);
    }

    [FoldoutGroup("Tools"), Button("Fill Node List")]
    void FillNodeList()
    {
        //editor button: adds every Node3DObject under this object that isn't in the list yet, at the end, so existing indexes never change
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Fill Node List");
#endif
        Node3DObject[] found = GetComponentsInChildren<Node3DObject>(true);
        int added = 0;
        for (int i = 0; i < found.Length; i++)
        {
            if (_Nodes.Contains(found[i])) continue;
            _Nodes.Add(found[i]);
            added++;
        }
        if (added == 0) Debug.LogWarning($"Warning! No new nodes found under {gameObject.name}. Nodes need a Node3DObject and to be children of the field...", this);
    }

    [FoldoutGroup("Tools"), Button("Fill Element Colors")]
    void FillElementColors()
    {
        //editor button: adds one entry for every element that isn't in Element Colors yet
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Fill Element Colors");
#endif
        //System.Enum.GetValues lists every value of an enum, so new elements show up without changing this code
        foreach (DamageType.ElementType element in System.Enum.GetValues(typeof(DamageType.ElementType)))
        {
            bool listed = false;
            for (int i = 0; i < _ElementColors.Count; i++)
            {
                if (_ElementColors[i] != null && _ElementColors[i]._Element == element) listed = true;
            }
            if (listed == false) _ElementColors.Add(new ElementColorEntry(element, _DefaultRuneColor));
        }
    }

#if UNITY_EDITOR
    static GameObject FindPrefab(string prefabName)
    {
        //editor only: finds a prefab in the project by its exact name, for Reset. Null if there isn't one
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:Prefab"); //searches names containing the text, so the exact name is checked below
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName) return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }
#endif
    #endregion

    #region Test Tools
    [FoldoutGroup("Test Tools"), Button("Test Place Rune")]
    void TestPlaceRune(ElementItemSO element, Vector2 fieldPosition)
    {
        //play mode button for quick tests: places a rune at a spot in field units (a rune is 100 wide, the core sits at 0, 0) and bridges it
        if (_field == null) { Debug.LogWarning("Warning! No field open, press play first..."); return; }
        if (element == null) return;
        if (_field.TryPlaceRune(element, fieldPosition, out int runeID) == false) { Debug.LogWarning($"Warning! A {element.name} can't go at {fieldPosition} (overlapping, outside the open zone, or a node that can't take it)...", this); return; }
        _field.ConnectNearby(runeID);
        SetUnsavedChanges(true);
    }
    #endregion

    #region Debugging
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //draws the zone rings flat on the field in the scene view (and the game view while Gizmos is on). Editor only
        //in play mode the rings are colored by state (frozen, open, locked) and frozen runes get an outline
        RuneFieldSettingsSO settings = GetToolSettings();
        if (settings == null) return;

        float unitsPerMeter = GetUnitsPerMeter();
        Vector3 center = transform.position;
        Vector3 normal = transform.up;

        for (int ring = 1; ring <= settings._ZoneCount; ring++)
        {
            float radius = settings.GetRingRadius(ring) / unitsPerMeter;
            UnityEditor.Handles.color = GetZoneGizmoColor(ring);
            UnityEditor.Handles.DrawWireDisc(center, normal, radius);
            UnityEditor.Handles.Label(center + transform.forward * radius, $"Zone {ring}");
        }

        UnityEditor.Handles.color = _BleedColor;
        UnityEditor.Handles.DrawWireDisc(center, normal, (settings.GetFieldEdge() + settings._EdgeBleed) / unitsPerMeter);

        if (_field == null) return;
        UnityEditor.Handles.color = _FrozenZoneColor;
        float runeRadius = settings._RuneSize * 0.5f / unitsPerMeter;
        foreach (KeyValuePair<int, Rune3DObject> entry in _runeViews)
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

[System.Serializable]
public class ElementColorEntry
{
    [Tooltip("The element")]
    public DamageType.ElementType _Element;

    [Tooltip("The color runes of this element get")]
    public Color _Color = Color.white;

    public ElementColorEntry()
    {
        //empty constructor so the inspector's "add" button can make a new entry
    }

    public ElementColorEntry(DamageType.ElementType element, Color color)
    {
        //used by Fill Element Colors
        _Element = element;
        _Color = color;
    }
}

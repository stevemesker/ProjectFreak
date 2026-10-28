using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

//test build tools for the 3D rune field (for sending the test to friends). Odin buttons don't exist in a build, so this makes real ones
//on start it puts the starting runes into the inventory, then builds a small button panel and a controls panel on the canvas:
//Level Up / Level Down (to try power and evolving), Reset Slot, Refill Runes and Quit, plus the open slot's name and level
//the Toggle Key (` by default) hides and shows the panels
//temp: a dev tool. It stays until the dev command console exists (spawning items, enemies etc.), where these tools will probably move [Notes for the future]
public class RuneFieldTestKitObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Runes put into the inventory when the scene starts, and topped back up to these counts by Refill Runes")]
    [SerializeField] List<RuneCountEntry> _StartingRunes = new List<RuneCountEntry>();

    [Tooltip("The controls shown in the corner of the screen")]
    [SerializeField, TextArea(6, 20)] string _ControlsText =
        "<b>Mouse & Keyboard</b>\n" +
        "Left click rune: drag  |  Left click field: pan\n" +
        "Middle mouse: pan  |  WASD: move\n" +
        "Scroll or [ ]: zoom  |  Esc: back  |  Tab: side panel\n" +
        "Drag from the list to place, onto the list to remove\n\n" +
        "<b>Gamepad</b>\n" +
        "Left stick: move  |  Right stick: free cursor\n" +
        "LT / RT: zoom  |  A: pick up / drop\n" +
        "X: send rune back  |  B: back  |  Y: side panel\n" +
        "Y: open the list, A on a rune: take it out, A: place it";

    [Header("Settings")]
    [Tooltip("Show the button panel and controls panel")]
    [SerializeField] bool _ShowPanels = true;

    [Tooltip("Keyboard key that hides and shows the panels. Backquote is the ` key (under Esc), Backslash is \\")]
    [SerializeField] Key _ToggleKey = Key.Backquote;

    [Tooltip("Size of the panel text")]
    [SerializeField, Min(1f)] float _FontSize = 20f;

    [Tooltip("Panel background color")]
    [SerializeField] Color _PanelColor = new Color(0f, 0f, 0f, 0.6f);

    [Tooltip("Button color")]
    [SerializeField] Color _ButtonColor = new Color(0.25f, 0.25f, 0.3f, 1f);

    [Header("References")]
    [Tooltip("The canvas the panels go on. Left empty, the first canvas found in the scene is used")]
    [SerializeField] Canvas _Canvas;

    //local variables
    const float ButtonWidth = 200f;
    const float ButtonHeight = 44f;
    TextMeshProUGUI _slotLabel; //shows the open slot's name and level
    GameObject _buttonPanel; //the two panels, kept so the Toggle Key can hide and show them
    GameObject _controlsPanel;

    private void Start()
    {
        //fills the inventory, then builds the panels (one-time canvas search if none is assigned)
        RefillRunes();
        if (_ShowPanels == false) return;

        if (_Canvas == null) _Canvas = FindObjectOfType<Canvas>();
        if (_Canvas == null) { Debug.LogWarning($"Warning! No canvas found for {gameObject.name}, the test buttons won't show...", this); return; }
        BuildButtonPanel();
        BuildControlsPanel();
    }

    private void Update()
    {
        //the Toggle Key hides and shows the panels, then the slot label is kept current (leveling and evolving change it)
        //Keyboard.current is the Input System's keyboard, read directly. Fine for a dev tool: no input action to set up (null when there's no keyboard)
        if (Keyboard.current != null && _ToggleKey != Key.None && Keyboard.current[_ToggleKey].wasPressedThisFrame) TogglePanels();

        if (_slotLabel == null || _slotLabel.gameObject.activeInHierarchy == false || ShadeManager._ShadeManager == null) return;
        int slotIndex = ShadeManager._ShadeManager.GetShadeSelectionIndex();
        ShadeSO slot = ShadeManager._ShadeManager.IsValidSlot(slotIndex) ? ShadeManager._ShadeManager.GetShadeOfIndex(slotIndex) : null;
        if (slot == null) { _slotLabel.text = "No shade slot"; return; }

        string form = slot._CurrentEvolution != null ? slot._CurrentEvolution.name : "no form";
        string text = $"{slot.name}\nLv {slot._Level}  ({form})";
        if (_slotLabel.text != text) _slotLabel.text = text; //setting text makes TextMeshPro rebuild, so only when it changed
    }

    #region Actions
    [Button("Level Up")]
    public void LevelUp()
    {
        //adds a level to the open slot (more core power). If the extra power reaches a plugged gate, the shade evolves
        ChangeLevel(1);
    }

    [Button("Level Down")]
    public void LevelDown()
    {
        //takes a level away from the open slot (it never goes below 1)
        ChangeLevel(-1);
    }

    void ChangeLevel(int amount)
    {
        //changes the open slot's level, then gives the open field the new core power (an evolve reloads the field by itself)
        if (ShadeManager._ShadeManager == null) return;
        ShadeManager._ShadeManager.AddLevels(ShadeManager._ShadeManager.GetShadeSelectionIndex(), amount);
        if (RuneField3DObject._Field3D != null) RuneField3DObject._Field3D.RefreshCorePower();
    }

    [Button("Reset Slot")]
    public void ResetSlot()
    {
        //starts the open slot over (empty field, level 1, Bound form) and reloads the field. Runes on its field are gone, Refill Runes gets more
        if (ShadeManager._ShadeManager == null) return;
        int slotIndex = ShadeManager._ShadeManager.GetShadeSelectionIndex();
        if (ShadeManager._ShadeManager.IsValidSlot(slotIndex) == false) return;

        ShadeManager._ShadeManager.GetShadeOfIndex(slotIndex).ResetSlot();
        if (RuneField3DObject._Field3D != null) RuneField3DObject._Field3D.LoadRuneField(slotIndex);
    }

    [Button("Refill Runes")]
    public void RefillRunes()
    {
        //tops the inventory back up to the starting counts (runes already on saved fields don't count, they've left the inventory)
        if (InventoryManager._PlayerInventory == null) { Debug.LogWarning($"Warning! No Inventory Manager found, {gameObject.name} can't add the starting runes...", this); return; }

        for (int i = 0; i < _StartingRunes.Count; i++)
        {
            RuneCountEntry entry = _StartingRunes[i];
            if (entry == null || entry._Rune == null) continue;
            int missing = entry._Count - InventoryManager._PlayerInventory.GetElementCount(entry._Rune);
            if (missing > 0) InventoryManager._PlayerInventory.AddElement(entry._Rune, missing);
        }
    }

    void TogglePanels()
    {
        //hides both panels if they're showing, shows them if they're hidden
        if (_buttonPanel == null && _controlsPanel == null) return; //not built (Show Panels is off, or no canvas)
        bool show = _buttonPanel != null ? _buttonPanel.activeSelf == false : _controlsPanel.activeSelf == false;
        if (_buttonPanel != null) _buttonPanel.SetActive(show);
        if (_controlsPanel != null) _controlsPanel.SetActive(show);
    }

    public void Quit()
    {
        //closes the game, or stops play mode in the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region Building
    void BuildButtonPanel()
    {
        //the button column in the top-left corner, with the slot label on top
        RectTransform panel = MakePanel("Test Kit Buttons", new Vector2(0f, 1f), new Vector2(16f, -16f));
        _buttonPanel = panel.gameObject;
        _slotLabel = MakeText(panel, "", TextAlignmentOptions.Left);

        MakeButton(panel, "Level Up", LevelUp);
        MakeButton(panel, "Level Down", LevelDown);
        MakeButton(panel, "Reset Slot", ResetSlot);
        MakeButton(panel, "Refill Runes", RefillRunes);
        MakeButton(panel, "Quit", Quit);
    }

    void BuildControlsPanel()
    {
        //the controls list in the bottom-left corner
        RectTransform panel = MakePanel("Test Kit Controls", new Vector2(0f, 0f), new Vector2(16f, 16f));
        _controlsPanel = panel.gameObject;
        MakeText(panel, _ControlsText, TextAlignmentOptions.Left);
    }

    RectTransform MakePanel(string panelName, Vector2 corner, Vector2 offset)
    {
        //a background box pinned to a corner that grows to fit what's put in it (a layout group stacks the children, a size fitter sizes the box)
        GameObject panelObject = new GameObject(panelName, typeof(RectTransform));
        panelObject.transform.SetParent(_Canvas.transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = offset;

        Image background = panelObject.AddComponent<Image>();
        background.color = _PanelColor;

        VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    TextMeshProUGUI MakeText(RectTransform parent, string text, TextAlignmentOptions alignment)
    {
        //a block of text inside a panel
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = _FontSize;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    void MakeButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        //a button inside a panel that runs onClick
        GameObject buttonObject = new GameObject(label, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.AddComponent<Image>();
        background.color = _ButtonColor;
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        LayoutElement size = buttonObject.AddComponent<LayoutElement>(); //tells the panel's layout how big the button wants to be
        size.preferredWidth = ButtonWidth;
        size.preferredHeight = ButtonHeight;

        TextMeshProUGUI text = MakeText(buttonObject.GetComponent<RectTransform>(), label, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero; //stretches the text over the whole button
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    #endregion
}

[System.Serializable]
public class RuneCountEntry
{
    [Tooltip("The rune")]
    public ElementItemSO _Rune;

    [Tooltip("How many")]
    [Min(0)] public int _Count = 5;
}

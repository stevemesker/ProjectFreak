using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

//a reusable popup window: a title, a message and up to 3 buttons (like "Save / Discard / Cancel")
//any script can show it with Show(...) and say what each button does. Clicking a button closes the popup, then runs that button's action
//setup: put this on an empty object under a canvas and press Build Default Layout. It makes the window and wires it up, then style it however you like
public class ConfirmPopup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The whole popup window (backing, panel, texts, buttons). Shown and hidden by this script. Must be a child of this object, so this script keeps running while it's hidden")]
    [SerializeField] GameObject _Window;

    [Tooltip("Text for the popup's title")]
    [SerializeField] TextMeshProUGUI _TitleText;

    [Tooltip("Text for the popup's message")]
    [SerializeField] TextMeshProUGUI _MessageText;

    [Tooltip("The popup's buttons, left to right. Up to 3 are used. Buttons with no action are hidden")]
    [SerializeField] List<PopupButtonEntry> _Buttons = new List<PopupButtonEntry>();

    [Header("Settings")]
    [Tooltip("Size of the panel built by Build Default Layout, in pixels")]
    [SerializeField] Vector2 _DefaultPanelSize = new Vector2(620f, 320f);

    //local variables
    List<System.Action> _actions = new List<System.Action>(); //what each button does for the popup that's showing. System.Action is a function stored in a variable

    private void Awake()
    {
        //starts hidden, and hooks every button up to its slot in the action list
        if (_Window == null) { Debug.LogError($"Error! Window not assigned on {gameObject.name}. Press Build Default Layout on it", this); return; }

        for (int i = 0; i < _Buttons.Count; i++)
        {
            if (_Buttons[i] == null || _Buttons[i]._Button == null) continue;
            int index = i; //a copy for the line below. Without it every button would use the loop's last value of i
            _Buttons[i]._Button.onClick.AddListener(() => PressButton(index)); //"() => ..." is a small inline function the button runs when clicked
        }
        _Window.SetActive(false);
    }

    #region Showing
    public void Show(string title, string message, string labelA, System.Action actionA, string labelB = null, System.Action actionB = null, string labelC = null, System.Action actionC = null)
    {
        //function that opens the popup. Pass a label and what happens for each button you want (up to 3). Leave the rest out and they're hidden
        //an action can be null if the button should only close the popup (like Cancel)
        if (_Window == null) { Debug.LogError($"Error! Window not assigned on {gameObject.name}, can't show the popup", this); return; }

        if (_TitleText != null) _TitleText.text = title;
        if (_MessageText != null) _MessageText.text = message;

        _actions.Clear();
        SetUpButton(0, labelA, actionA);
        SetUpButton(1, labelB, actionB);
        SetUpButton(2, labelC, actionC);

        _Window.SetActive(true);
        transform.SetAsLastSibling(); //draws on top of everything else under the same canvas
    }

    public void Close()
    {
        //hides the popup without running any button's action (like pressing Escape)
        if (_Window != null) _Window.SetActive(false);
        _actions.Clear();
    }

    public bool IsOpen()
    {
        return _Window != null && _Window.activeSelf;
    }

    void SetUpButton(int index, string label, System.Action action)
    {
        //shows a button with its label, or hides it if it has no label
        _actions.Add(action);
        if (index >= _Buttons.Count || _Buttons[index] == null || _Buttons[index]._Button == null)
        {
            if (string.IsNullOrEmpty(label) == false) Debug.LogWarning($"Warning! {gameObject.name} has no button {index} for \"{label}\", it won't show...", this);
            return;
        }

        bool used = string.IsNullOrEmpty(label) == false;
        _Buttons[index]._Button.gameObject.SetActive(used);
        if (used && _Buttons[index]._Label != null) _Buttons[index]._Label.text = label;
    }

    void PressButton(int index)
    {
        //closes the popup first, then runs the button's action. Closing first lets the action open a new popup if it needs to
        System.Action action = index < _actions.Count ? _actions[index] : null;
        Close();
        action?.Invoke();
    }
    #endregion

    #region Tools
    [Button("Build Default Layout"), GUIColor(0.4f, 1f, 0.4f)]
    void BuildDefaultLayout()
    {
        //editor button that makes a plain popup window under this object and fills in the references. Restyle it however you like afterwards
        //this object needs to be under a Canvas. It stretches over the whole canvas so the backing can block clicks behind the popup
        if (_Window != null) { Debug.LogWarning($"Warning! {gameObject.name} already has a window, delete it first to build a new one...", this); return; }
        if (GetComponentInParent<Canvas>() == null) { Debug.LogError($"Error! {gameObject.name} isn't under a Canvas, put it under one first", this); return; }

        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        Stretch(root);

        //window: everything that shows and hides
        RectTransform window = MakeChild("Window", root);
        Stretch(window);
        _Window = window.gameObject;

        //backing: darkens the screen and blocks clicks on the field behind the popup
        RectTransform backing = MakeChild("Backing", window);
        Stretch(backing);
        backing.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        //panel: the box in the middle
        RectTransform panel = MakeChild("Panel", window);
        panel.sizeDelta = _DefaultPanelSize;
        panel.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);

        _TitleText = MakeText("Title", panel, "Title", 36f, new Vector2(0f, 110f), new Vector2(_DefaultPanelSize.x - 40f, 60f));
        _MessageText = MakeText("Message", panel, "Message", 24f, new Vector2(0f, 20f), new Vector2(_DefaultPanelSize.x - 60f, 110f));

        //buttons row along the bottom
        RectTransform row = MakeChild("Buttons", panel);
        row.anchoredPosition = new Vector2(0f, -105f);
        row.sizeDelta = new Vector2(_DefaultPanelSize.x - 40f, 60f);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); //lines the buttons up side by side and centers them
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;

        _Buttons.Clear();
        string[] defaultLabels = { "Save", "Discard", "Cancel" };
        for (int i = 0; i < defaultLabels.Length; i++)
        {
            _Buttons.Add(MakeButton("Button " + i, row, defaultLabels[i]));
        }

        _Window.SetActive(false);
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(_Window, "Build Default Popup"); //Ctrl+Z removes it again, and marks the scene as changed
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    RectTransform MakeChild(string childName, Transform parent)
    {
        //makes an empty UI object under a parent
        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent, false); //false keeps its local position, so it lands where we set it
        return child.GetComponent<RectTransform>();
    }

    void Stretch(RectTransform rect)
    {
        //makes a UI object fill its parent
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI MakeText(string childName, Transform parent, string text, float fontSize, Vector2 position, Vector2 size)
    {
        //makes a centered text object
        RectTransform rect = MakeChild(childName, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return label;
    }

    PopupButtonEntry MakeButton(string childName, Transform parent, string text)
    {
        //makes a button with a text label on it
        RectTransform rect = MakeChild(childName, parent);
        rect.sizeDelta = new Vector2(170f, 56f);

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.3f, 0.3f, 0.36f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background; //the image the button tints when hovered or pressed

        TextMeshProUGUI label = MakeText("Label", rect, text, 24f, Vector2.zero, rect.sizeDelta);

        PopupButtonEntry entry = new PopupButtonEntry();
        entry._Button = button;
        entry._Label = label;
        return entry;
    }

    [Button("Test Show")]
    void TestShow()
    {
        //editor button (play mode only): shows the popup with 3 buttons that just log which one was pressed
        if (Application.isPlaying == false) return;
        Show("Test Popup", "This is what a popup message looks like.", "One", () => Debug.Log("One"), "Two", () => Debug.Log("Two"), "Cancel", null);
    }
    #endregion
}

[System.Serializable]
public class PopupButtonEntry
{
    [Tooltip("The button")]
    public Button _Button;

    [Tooltip("The text on the button, set to the label passed to Show")]
    public TextMeshProUGUI _Label;
}

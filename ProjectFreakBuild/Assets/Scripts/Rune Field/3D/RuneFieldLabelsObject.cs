using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

//power labels for the 3D rune field, drawn on the canvas so they stay readable at any zoom and tilt and never clip into meshes
//runes show "power it has / power it needs" when they're near the middle of the screen, hovered or dragged. The core always shows "left / total"
//labels are made once and reused (a pool), not made and destroyed every frame
//RuneField3DObject adds this to itself if it isn't there, so there's nothing to set up
[DefaultExecutionOrder(110)] //after the field (100), so labels follow where runes were placed this frame
public class RuneFieldLabelsObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far from the camera's focus (the middle of the screen) runes show their label when zoomed all the way in, in field units (a rune is 100 wide)")]
    [SerializeField, Min(0f)] float _RadiusZoomedIn = 500f;

    [Tooltip("The same when zoomed all the way out. Smaller, since far more runes fit on screen")]
    [SerializeField, Min(0f)] float _RadiusZoomedOut = 200f;

    [Tooltip("How far above a rune its label sits, in meters")]
    [SerializeField] float _RuneLabelHeight = 0.6f;

    [Tooltip("How far above the core its label sits, in meters")]
    [SerializeField] float _CoreLabelHeight = 2.4f;

    [Tooltip("Rune label text size")]
    [SerializeField, Min(1f)] float _FontSize = 24f;

    [Tooltip("Core label text size")]
    [SerializeField, Min(1f)] float _CoreFontSize = 34f;

    [Tooltip("Label color for a rune with power, and the core")]
    [SerializeField] Color _PoweredColor = Color.white;

    [Tooltip("Label color for a rune without power")]
    [SerializeField] Color _UnpoweredColor = new Color(1f, 0.55f, 0.45f, 1f);

    [Header("References")]
    [Tooltip("The canvas labels are drawn on. Left empty, the first canvas found in the scene is used")]
    [SerializeField] Canvas _Canvas;

    [Tooltip("The field camera, for its focus and zoom. Left empty, the one in the scene is found on start")]
    [SerializeField] RuneFieldCameraObject _CameraRig;

    //local variables
    RuneField3DObject _field;
    RectTransform _container; //holds every label, kept behind the rest of the canvas so panels draw over labels
    List<TextMeshProUGUI> _pool = new List<TextMeshProUGUI>();
    TextMeshProUGUI _coreLabel;
    int _used; //how many pool labels are shown this frame

    private void Start()
    {
        //finds what it needs once, then builds the label holder on the canvas
        _field = GetComponent<RuneField3DObject>();
        if (_Canvas == null) _Canvas = FindObjectOfType<Canvas>();
        if (_CameraRig == null) _CameraRig = FindObjectOfType<RuneFieldCameraObject>(); //one-time search on start, so there's nothing to drag in
        if (_Canvas == null) { Debug.LogWarning($"Warning! No canvas found for {gameObject.name}, rune labels won't show...", this); enabled = false; return; }

        GameObject holder = new GameObject("Rune Field Labels", typeof(RectTransform));
        holder.transform.SetParent(_Canvas.transform, false);
        holder.transform.SetAsFirstSibling(); //drawn first, so the side panel and popups cover the labels
        _container = holder.GetComponent<RectTransform>();

        _coreLabel = MakeLabel("Core Label", _CoreFontSize);
    }

    private void LateUpdate()
    {
        if (_field == null || _container == null) return;
        Camera viewCamera = RuneFieldPointerObject._Pointer != null ? RuneFieldPointerObject._Pointer.GetCamera() : Camera.main;
        if (viewCamera == null || _field.GetField() == null) { HideAll(); return; }

        _used = 0;
        ShowRuneLabels(viewCamera);
        ShowCoreLabel(viewCamera);

        for (int i = _used; i < _pool.Count; i++)
        {
            if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false); //hides labels not needed this frame
        }
    }

    #region Labels
    void ShowRuneLabels(Camera viewCamera)
    {
        //shows a label on every rune near the camera's focus, plus the hovered and dragged runes wherever they are
        RuneField field = _field.GetField();
        Vector2 focus = _CameraRig != null ? _field.WorldToField(_CameraRig.transform.position) : Vector2.zero;
        float zoom = _CameraRig != null ? _CameraRig.GetZoom() : 0f;
        float radius = Mathf.Lerp(_RadiusZoomedIn, _RadiusZoomedOut, zoom);

        List<PlacedRuneEntry> runes = field.GetData()._Runes;
        for (int i = 0; i < runes.Count; i++)
        {
            int runeID = runes[i]._ID;
            bool always = runeID == _field.GetHoveredRuneID() || runeID == _field.GetDraggingRuneID();
            if (always == false && Vector2.Distance(runes[i]._Position, focus) > radius) continue;

            Rune3DObject view = _field.GetRuneView(runeID);
            if (view == null) continue;

            int power = view.IsPowered() ? view.GetRequiredPower() : 0; //power is all or nothing per rune
            Color color = view.IsPowered() ? _PoweredColor : _UnpoweredColor;
            PlaceLabel(NextLabel(), viewCamera, view.transform.position + _field.transform.up * _RuneLabelHeight, $"{power} / {view.GetRequiredPower()}", color);
        }
    }

    void ShowCoreLabel(Camera viewCamera)
    {
        //the core's power, always shown
        Core3DObject core = _field.GetCore();
        Vector3 center = core != null ? core.transform.position : _field.transform.position;
        RuneField field = _field.GetField();
        PlaceLabel(_coreLabel, viewCamera, center + _field.transform.up * _CoreLabelHeight, $"{field.GetPowerLeft()} / {field.GetMaxPower()}", _PoweredColor);
    }

    void PlaceLabel(TextMeshProUGUI label, Camera viewCamera, Vector3 worldPosition, string text, Color color)
    {
        //puts a label at a spot in the world, as seen on screen. Hidden if the spot is behind the camera
        Vector3 screen = viewCamera.WorldToScreenPoint(worldPosition); //z is the distance in front of the camera
        bool visible = screen.z > 0f;
        if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
        if (visible == false) return;

        label.rectTransform.position = new Vector3(screen.x, screen.y, 0f); //an overlay canvas uses screen pixels as positions
        if (label.text != text) label.text = text; //only when it changed, setting text makes TextMeshPro rebuild the label
        label.color = color;
    }

    TextMeshProUGUI NextLabel()
    {
        //hands out the next free label from the pool, making a new one only when the pool runs out
        if (_used >= _pool.Count) _pool.Add(MakeLabel($"Rune Label {_pool.Count}", _FontSize));
        TextMeshProUGUI label = _pool[_used];
        _used++;
        return label;
    }

    TextMeshProUGUI MakeLabel(string labelName, float fontSize)
    {
        //makes one centered label in the holder. It ignores clicks so it never blocks the field
        GameObject labelObject = new GameObject(labelName, typeof(RectTransform));
        labelObject.transform.SetParent(_container, false);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.rectTransform.sizeDelta = new Vector2(200f, 50f);
        labelObject.SetActive(false);
        return label;
    }

    void HideAll()
    {
        for (int i = 0; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
        if (_coreLabel != null) _coreLabel.gameObject.SetActive(false);
    }
    #endregion
}

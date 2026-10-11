using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

//the shade info window in the rune field's side panel (next to the inventory and the shade slots). Shows the open slot's shade:
//its name, form and level, a render of its form's art (blacked out if the player hasn't seen that form yet), and its stats:
//base (from the form), what the saved runes and nodes add, the total, and what the draft would change (shown apart until it's saved)
//works with the 3D rune field. Setup: an empty object next to the other side panel windows, add this, press Build Default Layout (Tools)
public class ShadeInfoWindow : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The stats shown, top to bottom, and the name each one is shown with")]
    [SerializeField] List<StatLabelEntry> _ShownStats = new List<StatLabelEntry>()
    {
        new StatLabelEntry(DamageType.StatType.Health, "HP"),
        new StatLabelEntry(DamageType.StatType.Strength, "STR"),
        new StatLabelEntry(DamageType.StatType.Defense, "DEF"),
        new StatLabelEntry(DamageType.StatType.Agility, "AGI"),
        new StatLabelEntry(DamageType.StatType.Intellect, "INT"),
        new StatLabelEntry(DamageType.StatType.Spirit, "SPR"),
        new StatLabelEntry(DamageType.StatType.Discipline, "DIS"),
        new StatLabelEntry(DamageType.StatType.Wild, "WILD"),
    };

    [Header("Settings")]
    [Tooltip("Color of what the saved runes and nodes add to a stat")]
    [SerializeField] Color _BonusColor = new Color(0.6f, 0.8f, 1f, 1f);

    [Tooltip("Color of a draft change that raises a stat (not saved yet)")]
    [SerializeField] Color _DraftUpColor = new Color(0.4f, 1f, 0.4f, 1f);

    [Tooltip("Color of a draft change that lowers a stat (not saved yet)")]
    [SerializeField] Color _DraftDownColor = new Color(1f, 0.45f, 0.45f, 1f);

    [Tooltip("Tint on the render while the player hasn't seen this form yet. Black = a blacked out silhouette (temp, the real look is part of the art pass)")]
    [SerializeField] Color _UnseenColor = Color.black;

    [FoldoutGroup("Render")]
    [Tooltip("Optional. A prefab with the render camera and its lights, for the lighting pass. Left empty, a plain camera is made")]
    [SerializeField] GameObject _StagePrefab;

    [FoldoutGroup("Render")]
    [Tooltip("Where the render stage is spawned, in world space. Far away so the field's camera never sees it")]
    [SerializeField] Vector3 _StagePosition = new Vector3(0f, -1000f, 0f);

    [FoldoutGroup("Render")]
    [Tooltip("Width and height of the render, in pixels")]
    [SerializeField, Range(64, 2048)] int _RenderSize = 512;

    [FoldoutGroup("Render")]
    [Tooltip("Which way the camera looks at the shade from, around the stage (Z = in front, Y = up). Only the direction matters")]
    [SerializeField] Vector3 _ViewDirection = new Vector3(0f, 0.3f, 1f);

    [FoldoutGroup("Render")]
    [Tooltip("The render camera's field of view, in degrees")]
    [SerializeField, Range(5f, 90f)] float _FieldOfView = 30f;

    [FoldoutGroup("Render")]
    [Tooltip("Extra room around the shade in the render. 1 = touching the edges")]
    [SerializeField, Min(1f)] float _FramePadding = 1.2f;

    [FoldoutGroup("Render")]
    [Tooltip("How fast the shade turns on the stage, in degrees per second. 0 = it stays still")]
    [SerializeField] float _SpinSpeed = 15f;

    [Header("References")]
    [Tooltip("Shows the shade's name, form and level. Build Default Layout makes one")]
    [SerializeField] TextMeshProUGUI _TitleText;

    [Tooltip("Shows the stat rows. Build Default Layout makes one")]
    [SerializeField] TextMeshProUGUI _StatsText;

    [Tooltip("Shows the render of the shade. Build Default Layout makes one")]
    [SerializeField] RawImage _RenderImage;

    [Tooltip("Optional. Shown over the render while the form hasn't been seen (like a \"?\"). Build Default Layout makes one")]
    [SerializeField] GameObject _UnseenOverlay;

    //local variables
    bool _subscribed; //listening to the field and the Shade Manager
    RuneField3DObject _field3D; //the field listened to, so it can be let go of later
    ShadeManager _shadeManager; //same for the Shade Manager
    Transform _stage; //the render stage far away from the field
    Camera _renderCamera;
    RenderTexture _renderTexture;
    GameObject _art; //the shown form's art, spawned on the stage
    ShadeEvolutionSO _shownEvolution; //the form the art was spawned for

    private void OnEnable()
    {
        //the side panel turns this on when its tab opens. Listens for changes and shows the open shade
        Subscribe();
        Refresh();
        if (_renderCamera != null) _renderCamera.enabled = true;
    }

    private void Start()
    {
        //every Awake has run by Start, so the field and Shade Manager singletons are set even if OnEnable came first
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        //stops listening and stops rendering while the window is closed (a second camera costs, especially on Steam Deck)
        Unsubscribe();
        if (_renderCamera != null) _renderCamera.enabled = false;
    }

    private void OnDestroy()
    {
        //cleans up the stage and the render texture, which aren't children of this object
        if (_stage != null) Destroy(_stage.gameObject);
        if (_renderTexture != null)
        {
            _renderTexture.Release(); //frees its memory on the graphics card
            Destroy(_renderTexture);
        }
    }

    private void Update()
    {
        //turns the shade slowly on the stage
        if (_art != null && _SpinSpeed != 0f) _art.transform.Rotate(Vector3.up, _SpinSpeed * Time.unscaledDeltaTime, Space.World); //unscaled, so it still turns while paused
    }

    #region Listening
    void Subscribe()
    {
        //listens to the 3D field (the draft changed, or a field loaded) and the Shade Manager (a shade evolved)
        if (_subscribed) return;
        _field3D = RuneField3DObject._Field3D;
        _shadeManager = ShadeManager._ShadeManager;
        if (_field3D == null && _shadeManager == null) return; //nothing to listen to yet, Start tries again

        if (_field3D != null) _field3D.OnDraftChanged += Refresh;
        if (_shadeManager != null) _shadeManager.OnShadeEvolved += OnShadeEvolved;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (_subscribed == false) return;
        if (_field3D != null) _field3D.OnDraftChanged -= Refresh;
        if (_shadeManager != null) _shadeManager.OnShadeEvolved -= OnShadeEvolved;
        _subscribed = false;
    }

    void OnShadeEvolved(int slotIndex)
    {
        Refresh();
    }
    #endregion

    #region Display
    [Button("Refresh")]
    public void Refresh()
    {
        //shows the open slot's shade: title, render and stats
        if (isActiveAndEnabled == false) return; //closed: it refreshes when it opens again
        ShadeManager manager = ShadeManager._ShadeManager;
        if (manager == null) { SetText(_TitleText, "No Shade Manager"); SetText(_StatsText, ""); return; }

        int slotIndex = manager.GetShadeSelectionIndex();
        ShadeSO slot = manager.IsValidSlot(slotIndex) ? manager.GetShadeOfIndex(slotIndex) : null;
        if (slot == null) { SetText(_TitleText, "No shade"); SetText(_StatsText, ""); return; }

        ShadeEvolutionSO evolution = slot._CurrentEvolution;
        bool seen = manager.IsEvolutionSeen(evolution);
        string formName = evolution == null ? "No form" : (seen ? evolution.name : "???"); //todo: display names for forms (these are asset names)

        SetText(_TitleText, $"{slot.name}\n<size=70%>{formName}  ·  Lv {slot._Level}</size>"); //<size> is TextMeshPro rich text: a smaller second line
        SetText(_StatsText, BuildStatRows(manager, slotIndex, slot));
        ShowRender(evolution, seen);
    }

    string BuildStatRows(ShadeManager manager, int slotIndex, ShadeSO slot)
    {
        //one line per shown stat: name, base, what the saved runes and nodes add, total, and the draft's change if there is one
        ShadeStats baseStats = slot._CurrentEvolution != null ? slot._CurrentEvolution._BaseStats : null;
        ShadeStats totalStats = manager.GetSlotStats(slotIndex); //the Shade Manager's numbers, which include the "can't go below 1" rule
        Dictionary<DamageType.StatType, int> saved = RuneEffect.AddUpStats(slot._CompiledEffects);

        //the draft is the 3D field's open field. Opening a slot there also selects it on the Shade Manager, so it's always this slot's draft
        Dictionary<DamageType.StatType, int> draft = null;
        RuneField draftField = RuneField3DObject._Field3D != null ? RuneField3DObject._Field3D.GetField() : null;
        if (draftField != null) draft = draftField.GetStatTotals();

        string rows = "";
        for (int i = 0; i < _ShownStats.Count; i++)
        {
            StatLabelEntry entry = _ShownStats[i];
            if (entry == null) continue;

            int baseValue = GetStat(baseStats, entry._Stat);
            int savedBonus = GetValue(saved, entry._Stat);
            int total = totalStats != null ? GetStat(totalStats, entry._Stat) : baseValue + savedBonus;

            string line = $"{entry._Label}   {baseValue}";
            if (savedBonus != 0) line += $"  {Colored(Signed(savedBonus), _BonusColor)}";
            line += $"  =  <b>{total}</b>";

            if (draft != null)
            {
                int change = GetValue(draft, entry._Stat) - savedBonus; //what saving the draft would change
                if (change != 0) line += $"   {Colored("(" + Signed(change) + ")", change > 0 ? _DraftUpColor : _DraftDownColor)}";
            }
            rows += line + "\n";
        }
        return rows;
    }

    void ShowRender(ShadeEvolutionSO evolution, bool seen)
    {
        //spawns the form's tethered art on the stage (if it changed) and points the camera at it. Not seen yet: blacked out
        if (_RenderImage == null) return;
        BuildStage();

        if (evolution != _shownEvolution)
        {
            if (_art != null) Destroy(_art);
            _art = null;
            _shownEvolution = evolution;

            if (evolution != null && evolution._TetheredArt != null)
            {
                _art = Instantiate(evolution._TetheredArt, _stage.position, _stage.rotation, _stage);
                _art.name = $"Render Art ({evolution.name})";
                if (_art.TryGetComponent(out ShadeArtRig rig)) rig.ApplyForm(ShadeFormType.Form.Tethered); //hides the parts this form doesn't use
            }
            FrameCamera();
        }

        _RenderImage.color = seen ? Color.white : _UnseenColor;
        if (_UnseenOverlay != null) _UnseenOverlay.SetActive(seen == false);
    }

    void BuildStage()
    {
        //makes the render stage the first time it's needed: an object far from the field with a camera rendering into a texture the window shows
        if (_stage != null) return;

        _stage = new GameObject("Shade Info Stage").transform; //not under the canvas, it's a normal object in the scene
        _stage.position = _StagePosition;

        if (_StagePrefab != null)
        {
            GameObject stageArt = Instantiate(_StagePrefab, _stage.position, _stage.rotation, _stage);
            _renderCamera = stageArt.GetComponentInChildren<Camera>(true);
            if (_renderCamera == null) Debug.LogWarning($"Warning! Stage Prefab on {gameObject.name} has no Camera, making a plain one...", this);
        }
        if (_renderCamera == null)
        {
            _renderCamera = new GameObject("Render Camera").AddComponent<Camera>();
            _renderCamera.transform.SetParent(_stage, false);
        }
        _renderCamera.fieldOfView = _FieldOfView;

        _renderTexture = new RenderTexture(_RenderSize, _RenderSize, 24); //24 = depth buffer bits, so things in front hide things behind
        _renderTexture.name = "Shade Info Render";
        _renderCamera.targetTexture = _renderTexture; //renders into the texture instead of the screen
        _RenderImage.texture = _renderTexture;
        _renderCamera.enabled = isActiveAndEnabled;
    }

    void FrameCamera()
    {
        //places the camera so the whole shade fits in the render, looking at it from View Direction
        if (_renderCamera == null) return;
        Bounds bounds = GetArtBounds();
        float radius = Mathf.Max(bounds.extents.magnitude, 0.1f); //a sphere around the shade, so it fits however it's turned
        float halfAngle = _FieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = radius * _FramePadding / Mathf.Sin(halfAngle); //how far back the sphere just fits the view

        Vector3 direction = _ViewDirection.sqrMagnitude > 0.0001f ? _ViewDirection.normalized : Vector3.forward;
        _renderCamera.transform.position = bounds.center + _stage.rotation * direction * distance;
        _renderCamera.transform.LookAt(bounds.center);
    }

    Bounds GetArtBounds()
    {
        //a box around every renderer on the art. No art (or no renderers): a small box on the stage
        Bounds bounds = new Bounds(_stage.position, Vector3.one);
        if (_art == null) return bounds;

        Renderer[] renderers = _art.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (i == 0) bounds = renderers[i].bounds;
            else bounds.Encapsulate(renderers[i].bounds); //grows the box to fit this renderer too
        }
        return bounds;
    }
    #endregion

    #region Tools
    int GetStat(ShadeStats stats, DamageType.StatType stat)
    {
        //a stat's value from a set of stats. TypeToStatFinder doesn't know the shade-only stats, so those are read here
        if (stats == null) return 0;
        if (stat == DamageType.StatType.Discipline) return stats._DIS;
        if (stat == DamageType.StatType.Wild) return stats._WILD;
        return stats.TypeToStatFinder(stat);
    }

    int GetValue(Dictionary<DamageType.StatType, int> totals, DamageType.StatType stat)
    {
        //a stat's total from an added-up list, 0 if it isn't in there
        if (totals != null && totals.TryGetValue(stat, out int value)) return value;
        return 0;
    }

    string Signed(int value)
    {
        //a number with its sign: +3, -2
        return value > 0 ? "+" + value : value.ToString();
    }

    string Colored(string text, Color color)
    {
        //wraps text in a TextMeshPro color tag
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>"; //ToHtmlStringRGBA turns a color into hex like FF8800FF
    }

    void SetText(TextMeshProUGUI label, string text)
    {
        if (label != null) label.text = text;
    }

    [Button("Build Default Layout"), GUIColor(0.4f, 1f, 0.4f)]
    void BuildDefaultLayout()
    {
        //editor button that makes a plain layout under this object (title, render, stats) and fills in the references. Restyle it however you like afterwards
        //this object needs to be under a Canvas. It stretches to fill its parent, like the other side panel windows
        if (_TitleText != null || _StatsText != null || _RenderImage != null) { Debug.LogWarning($"Warning! {gameObject.name} already has a layout, delete it (and clear the references) first to build a new one...", this); return; }
        if (GetComponentInParent<Canvas>() == null) { Debug.LogError($"Error! {gameObject.name} isn't under a Canvas, put it under the side panel first", this); return; }

        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        UIBuildTools.Stretch(root);

        //title along the top
        _TitleText = UIBuildTools.MakeText("Title", root, "Shade", 32f, Vector2.zero, Vector2.zero);
        RectTransform title = _TitleText.rectTransform;
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.offsetMin = new Vector2(20f, -100f); //20 in from the left, 100 tall
        title.offsetMax = new Vector2(-20f, -10f); //20 in from the right, 10 down from the top

        //the render, a square under the title
        RectTransform render = UIBuildTools.MakeChild("Render", root);
        render.anchorMin = new Vector2(0.5f, 1f);
        render.anchorMax = new Vector2(0.5f, 1f);
        render.pivot = new Vector2(0.5f, 1f);
        render.anchoredPosition = new Vector2(0f, -110f);
        render.sizeDelta = new Vector2(300f, 300f);
        _RenderImage = render.gameObject.AddComponent<RawImage>();
        _RenderImage.raycastTarget = false; //so it doesn't count as UI under the pointer

        //"?" over the render while the form hasn't been seen
        TextMeshProUGUI unseen = UIBuildTools.MakeText("Unseen", render, "?", 120f, Vector2.zero, render.sizeDelta);
        unseen.raycastTarget = false;
        _UnseenOverlay = unseen.gameObject;
        _UnseenOverlay.SetActive(false);

        //stats fill the rest
        _StatsText = UIBuildTools.MakeText("Stats", root, "", 26f, Vector2.zero, Vector2.zero);
        RectTransform stats = _StatsText.rectTransform;
        UIBuildTools.Stretch(stats);
        stats.offsetMin = new Vector2(30f, 20f);
        stats.offsetMax = new Vector2(-30f, -430f); //starts under the render
        _StatsText.alignment = TextAlignmentOptions.TopLeft;
        _StatsText.richText = true; //colors and bold in the rows
        _TitleText.raycastTarget = false;
        _StatsText.raycastTarget = false;

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(title.gameObject, "Build Shade Info Layout"); //Ctrl+Z removes them again, and marks the scene as changed
        UnityEditor.Undo.RegisterCreatedObjectUndo(render.gameObject, "Build Shade Info Layout");
        UnityEditor.Undo.RegisterCreatedObjectUndo(stats.gameObject, "Build Shade Info Layout");
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    #endregion

    #region Test Tools
    [Button("Forget Current Form (play mode)")]
    void TestForgetForm()
    {
        //play mode: takes the open shade's form off the save slot's seen list, to check the blacked out look
        if (Application.isPlaying == false || ShadeManager._ShadeManager == null || SaveManager._save == null) return;
        ShadeSO slot = ShadeManager._ShadeManager.GetShadeOfIndex(ShadeManager._ShadeManager.GetShadeSelectionIndex());
        SaveSlotSO save = SaveManager._save.GetSaveSlotData();
        if (slot == null || save == null || save._SeenEvolutions == null) return;
        save._SeenEvolutions.Remove(slot._CurrentEvolution);
        Refresh();
    }
    #endregion
}

[System.Serializable]
public class StatLabelEntry
{
    [Tooltip("Which stat")]
    public DamageType.StatType _Stat;

    [Tooltip("The name it's shown with")]
    public string _Label;

    public StatLabelEntry()
    {
        //empty constructor so the inspector's "add" button can make a new entry
    }

    public StatLabelEntry(DamageType.StatType stat, string label)
    {
        //lets the default list be written in one line each
        _Stat = stat;
        _Label = label;
    }
}

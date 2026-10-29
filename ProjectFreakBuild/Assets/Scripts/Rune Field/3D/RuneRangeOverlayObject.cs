using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Sirenix.OdinInspector;

//shows connection ranges on the floor while a rune is being placed (dragged on the field, carried with a gamepad, or dragged out of the inventory list)
//runes and the core light up as the placing rune gets close: fading in from Approach Ratio (150%) and full once a bridge would be made
//the floor look is a decal using RuneRangeField.hlsl. This sends that shader its numbers every frame, and tells each rune / the core how lit up it is (ShowRangeHighlight)
//a bridge's length follows the same rule as RuneField: the bigger of the two runes' reaches, so the overlay always agrees with what dropping the rune does
[DefaultExecutionOrder(110)] //runs after RuneField3DObject (100), so it uses this frame's drag position
public class RuneRangeOverlayObject : MonoBehaviour
{
    //has to match RUNE_RANGE_MAX in RuneRangeField.hlsl
    public const int MaxShownRanges = 32;

    [Header("Settings")]
    [Tooltip("How far out runes start lighting up, as a multiple of the bridge length (1.5 = they start fading in at 150% of the distance a bridge can reach)")]
    [SerializeField, Range(1f, 3f)] float _ApproachRatio = 1.5f;

    [Tooltip("How the light-up builds while approaching. X: 0 = just entered the approach zone, 1 = a bridge would be made. Y: strength (0 to 1)")]
    [SerializeField] AnimationCurve _ApproachCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("How fast runes fade in and out, in full fades per second (6 = about a sixth of a second)")]
    [SerializeField, Min(0.1f)] float _FadeSpeed = 6f;

    [Tooltip("Strength of a rune in range that can't take another bridge (all its bridge slots are used), so it's clear it won't connect. 0 = hidden, 1 = same as the others")]
    [SerializeField, Range(0f, 1f)] float _FullRuneStrength = 0.35f;

    [FoldoutGroup("Decal")]
    [Tooltip("The decal that draws the ranges on the floor. Create Range Decal makes one sized to the field")]
    [SerializeField] DecalProjector _Decal;

    [FoldoutGroup("Decal")]
    [Tooltip("The range overlay material (made from a Decal Shader Graph that uses RuneRangeField.hlsl). Put on the decal by Create Range Decal and whenever this changes")]
    [SerializeField] Material _DecalMaterial;

    [FoldoutGroup("Decal")]
    [Tooltip("How tall the decal's box is, in meters. It has to reach every zone's floor height (raised and lowered zones) without reaching up to dragged runes")]
    [SerializeField, Min(0.1f)] float _DecalDepth = 3f;

    [Header("References")]
    [Tooltip("The 3D rune field this shows ranges for. Left empty, the one on this object (or the one in the scene) is used")]
    [SerializeField] RuneField3DObject _Field;

    [FoldoutGroup("Test Tools")]
    [Tooltip("Shows every rune's and the core's range all the time (at their own reach), so the decal's look can be tuned in play mode without dragging anything")]
    [SerializeField] bool _TestShowEveryRange;

    [Header("Runtime Data")]
    [Tooltip("How many ranges are drawn right now (read only)")]
    [SerializeField, ReadOnly] int _ShownRanges;

    //local variables
    Dictionary<int, float> _intensities = new Dictionary<int, float>(); //rune ID (or RuneField.CoreID) -> how lit up it is right now, 0 to 1
    List<int> _ids = new List<int>(); //reused list of every rune ID plus the core, so no new list is made each frame
    List<int> _staleIDs = new List<int>(); //reused list of IDs that left the field
    Vector4[] _shaderData = new Vector4[MaxShownRanges];
    float[] _shaderIntensity = new float[MaxShownRanges];
    bool _warnedTooMany;

    static readonly int FieldCenter = Shader.PropertyToID("_RuneFieldCenter");
    static readonly int FieldRight = Shader.PropertyToID("_RuneFieldRight");
    static readonly int FieldForward = Shader.PropertyToID("_RuneFieldForward");
    static readonly int RangeData = Shader.PropertyToID("_RuneRangeData");
    static readonly int RangeIntensity = Shader.PropertyToID("_RuneRangeIntensity");
    static readonly int RangeCount = Shader.PropertyToID("_RuneRangeCount");
    static readonly int ApproachRatio = Shader.PropertyToID("_RuneApproachRatio");

    private void OnValidate()
    {
        //keeps the decal on the chosen material when it's swapped in the inspector
        if (_Decal != null && _DecalMaterial != null) _Decal.material = _DecalMaterial;
    }

    private void Awake()
    {
        if (_Field == null) _Field = GetComponent<RuneField3DObject>();

        //sends full size arrays straight away. Unity locks a global array's size the first time it's set, so it has to start at the full 32
        PushToShader(0);
    }

    private void Start()
    {
        //falls back to the scene's field (its Awake has run by now). Checked once here instead of every frame
        if (_Field == null) _Field = RuneField3DObject._Field3D;
        if (_Field == null) Debug.LogError($"Error! No RuneField3DObject found for {gameObject.name}, the range overlay won't show", this);
        if (_Decal == null) Debug.LogWarning($"Warning! No range decal on {gameObject.name} (press Create Range Decal in the Decal foldout). Runes still light up, but nothing is drawn on the floor...", this);
    }

    private void OnDisable()
    {
        //turns everything off so nothing is left lit while this is off
        foreach (int id in _intensities.Keys) ShowHighlight(id, 0f);
        _intensities.Clear();
        PushToShader(0);
    }

    private void LateUpdate()
    {
        UpdateRanges();
    }

    #region Ranges
    void UpdateRanges()
    {
        //fades every rune's (and the core's) light-up toward where it should be, tells it, and sends the lit ones to the floor shader
        int shownCount = 0;
        RuneField field = _Field != null ? _Field.GetField() : null;
        if (field == null) { PushToShader(0); return; }

        bool previewing = _Field.TryGetPlacementPreview(out Vector2 previewPosition, out float previewReach, out int movingID);
        float unitsPerMeter = _Field.GetUnitsPerMeter();

        //every rune on the field plus the core
        _ids.Clear();
        _ids.Add(RuneField.CoreID);
        List<PlacedRuneEntry> runes = field.GetData()._Runes;
        for (int i = 0; i < runes.Count; i++) _ids.Add(runes[i]._ID);

        for (int i = 0; i < _ids.Count; i++)
        {
            int id = _ids[i];

            float target = 0f;
            if (previewing && id != movingID) target = GetTargetIntensity(field, id, previewPosition, previewReach, movingID);
            else if (_TestShowEveryRange && id != movingID) target = 1f;

            _intensities.TryGetValue(id, out float current); //0 for a rune it hasn't seen yet
            float next = Mathf.MoveTowards(current, target, _FadeSpeed * Time.deltaTime);
            _intensities[id] = next;

            //sent while lit too (not only on change), so a rune whose view was respawned mid-drag picks it up again
            if (next != current || next > 0f) ShowHighlight(id, next);
            if (next <= 0f) continue;

            if (shownCount >= MaxShownRanges)
            {
                if (_warnedTooMany == false)
                {
                    Debug.LogWarning($"Warning! More than {MaxShownRanges} ranges lit at once on {gameObject.name}, the extras aren't drawn on the floor...", this);
                    _warnedTooMany = true;
                }
                continue;
            }

            //the range drawn is the length a bridge could be: the bigger of the two reaches (the same rule RuneField uses)
            float reach = field.GetReach(id);
            if (previewing) reach = Mathf.Max(reach, previewReach);

            //z tells the shader if the placing rune is in this range (1) or only approaching it (0), so only real ranges make the perimeter line
            float inRange = previewing == false || IsInRange(field, id, previewPosition, previewReach) ? 1f : 0f;

            Vector2 spot = field.GetPosition(id) / unitsPerMeter; //field units -> meters on the field plane
            _shaderData[shownCount] = new Vector4(spot.x, spot.y, inRange, reach / unitsPerMeter);
            _shaderIntensity[shownCount] = next;
            shownCount++;
        }

        ForgetRemovedRunes();
        PushToShader(shownCount);
    }

    float GetTargetIntensity(RuneField field, int id, Vector2 previewPosition, float previewReach, int movingID)
    {
        //works out how lit up a rune should be from how close the placing rune is to it
        //ratio: 1 = right at the bridge's reach, 1.5 = the outer edge of the approach zone (with the default Approach Ratio)
        float bridgeLength = Mathf.Max(field.GetReach(id), previewReach);
        if (bridgeLength <= 0f) return 0f;

        float ratio = Vector2.Distance(previewPosition, field.GetPosition(id)) / bridgeLength;
        if (ratio >= _ApproachRatio) return 0f;

        float strength = 1f;
        if (ratio > 1f)
        {
            //InverseLerp turns the ratio into 0 at the approach edge and 1 at the bridge's reach
            float progress = Mathf.InverseLerp(_ApproachRatio, 1f, ratio);
            strength = Mathf.Clamp01(_ApproachCurve.Evaluate(progress));
        }

        //a rune with no free bridge slot won't connect, so it's dimmer (unless it's already bridged to the rune being dragged)
        if (field.HasFreeBridgeSlot(id) == false && field.AreConnected(movingID, id) == false) strength *= _FullRuneStrength;
        return strength;
    }

    bool IsInRange(RuneField field, int id, Vector2 previewPosition, float previewReach)
    {
        //true if the placing rune is within bridge length of this rune (dropping here would bridge to it, slots allowing)
        float bridgeLength = Mathf.Max(field.GetReach(id), previewReach);
        return Vector2.Distance(previewPosition, field.GetPosition(id)) <= bridgeLength;
    }

    void ShowHighlight(int id, float intensity)
    {
        //tells a rune or the core how lit up it is. Its view can be missing (a rune just removed), so it's skipped then
        if (_Field == null) return;

        if (id == RuneField.CoreID)
        {
            Core3DObject core = _Field.GetCore();
            if (core != null) core.ShowRangeHighlight(intensity);
            return;
        }

        Rune3DObject view = _Field.GetRuneView(id);
        if (view != null) view.ShowRangeHighlight(intensity);
    }

    void ForgetRemovedRunes()
    {
        //drops runes that left the field (taken off, or a different slot was opened), so the list doesn't keep growing
        _staleIDs.Clear();
        foreach (int id in _intensities.Keys)
        {
            if (_ids.Contains(id) == false) _staleIDs.Add(id);
        }
        for (int i = 0; i < _staleIDs.Count; i++) _intensities.Remove(_staleIDs[i]);
    }

    void PushToShader(int shownCount)
    {
        //sends the ranges to every material using RuneRangeField.hlsl. Global values, so the material doesn't need them as properties
        _ShownRanges = shownCount;
        Transform plane = _Field != null ? _Field.transform : transform;
        Shader.SetGlobalVector(FieldCenter, plane.position);
        Shader.SetGlobalVector(FieldRight, plane.right);
        Shader.SetGlobalVector(FieldForward, plane.forward);
        Shader.SetGlobalVectorArray(RangeData, _shaderData);
        Shader.SetGlobalFloatArray(RangeIntensity, _shaderIntensity);
        Shader.SetGlobalFloat(RangeCount, shownCount);
        Shader.SetGlobalFloat(ApproachRatio, _ApproachRatio);
    }
    #endregion

    #region Tools
#if UNITY_EDITOR
    [FoldoutGroup("Decal"), Button("Create Range Decal"), GUIColor(0.4f, 1f, 0.4f)]
    void CreateRangeDecal()
    {
        //editor button: makes the decal that draws the ranges, centered on the field, pointing down, sized to cover the whole field plus its edge bleed
        RuneField3DObject field = _Field != null ? _Field : GetComponent<RuneField3DObject>();
        if (field == null) field = FindObjectOfType<RuneField3DObject>(); //editor tool, so searching is fine here
        if (field == null) { Debug.LogError($"Error! No RuneField3DObject found for {gameObject.name}, can't size the decal", this); return; }

        RuneFieldSettingsSO settings = RuneFieldSettingsSO.FindInProject();
        if (settings == null) { Debug.LogError($"Error! No Rune Field Settings asset found in the project, can't size the decal for {gameObject.name}", this); return; }

        if (_Decal == null)
        {
            GameObject decalObject = new GameObject("Range Overlay Decal");
            UnityEditor.Undo.RegisterCreatedObjectUndo(decalObject, "Create Range Decal"); //so Ctrl+Z removes it
            decalObject.transform.SetParent(transform, false);
            _Decal = decalObject.AddComponent<DecalProjector>();
        }

        UnityEditor.Undo.RecordObject(_Decal.transform, "Create Range Decal");
        UnityEditor.Undo.RecordObject(_Decal, "Create Range Decal");

        //a decal projects along its own forward (blue arrow). Turning it 90 degrees on X points that straight down at the field
        _Decal.transform.SetPositionAndRotation(field.transform.position, field.transform.rotation * Quaternion.Euler(90f, 0f, 0f));

        float diameter = settings.ToMeters(settings.GetFieldEdge() + settings._EdgeBleed) * 2f;
        _Decal.size = new Vector3(diameter, diameter, _DecalDepth);
        _Decal.pivot = Vector3.zero; //box centered on the field's plane, so it reaches above and below it equally
        if (_DecalMaterial != null) _Decal.material = _DecalMaterial;

        _Field = field;
        UnityEditor.EditorUtility.SetDirty(this);
        if (_DecalMaterial == null) Debug.LogWarning($"Warning! Range decal made on {gameObject.name} with no material yet. Assign Decal Material and it goes onto the decal...", this);
    }
#endif
    #endregion
}

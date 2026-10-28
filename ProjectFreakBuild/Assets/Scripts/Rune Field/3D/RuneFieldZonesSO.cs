using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//the 3D rune field's zone look, in one asset (SO_RuneField_Zones) so changes made in play mode stay after stopping
//holds the floor lift and zone light looks, the strip the walls and cracks are built from, the border shader's property names,
//the stand-in look used until Steve's shader exists, the stand-in smoke, and the zone opening timings (see Zone Visuals Plan)
//the tear material is set here and only here (Tear Material). The Rebuild All Borders button rebuilds the walls straight from this asset
//the game never writes to this asset, it only reads it (the editor buttons below do). Edit it by hand while playing to find the look
[CreateAssetMenu(fileName = "SO_RuneField_Zones", menuName = "Rune Field/Zones", order = 2)]
public class RuneFieldZonesSO : ScriptableObject
{
    //inspector layout (clean-up, Oct 28, 2026): what gets tuned day to day is open (zone look, walls, zone opening), with the wall rebuild right next to the wall settings
    //the strip import, shader property names, smoke particles and stand-in look are in closed foldouts

    [FoldoutGroup("Zone Look", expanded: true)]
    [Tooltip("How far a locked zone sits below the raised floor, in meters. Open and frozen zones sit at the raised level (the field's height). Keep it smaller than the floors' outer skirt and the walls' Bottom Depth")]
    [Min(0f)] public float _LiftHeight = 0.25f;

    [FoldoutGroup("Zone Look")]
    [Tooltip("The zone light on the open zone (the one the player builds in). Neutral. Intensity is in the key light's unit (lux for a directional light)")]
    public ZoneLightEntry _OpenLight = new ZoneLightEntry(new Color(1f, 0.97f, 0.92f, 1f), 30000f);

    [FoldoutGroup("Zone Look")]
    [Tooltip("The zone light on frozen zones (the ones the shade evolved out of). A bit blue, the 'frozen, locked in' feel")]
    public ZoneLightEntry _FrozenLight = new ZoneLightEntry(new Color(0.65f, 0.8f, 1f, 1f), 30000f);

    [FoldoutGroup("Zone Look")]
    [Tooltip("The zone light on locked zones (not reached yet). Muted and darker. Purple for now, might end up blue or green [undecided]")]
    public ZoneLightEntry _LockedLight = new ZoneLightEntry(new Color(0.6f, 0.5f, 0.8f, 1f), 8000f);

    [FoldoutGroup("Walls", expanded: true)]
    [InfoBox("Empty: the walls use the Wall Strip model's own material (from its import settings), or a grey stand-in if there's no strip. Press Use Wall Strip's Material to copy it in here, so this is the one place to change it", InfoMessageType.Info, "@_TearMaterial == null")]
    [Tooltip("The forward border's wall and its cracks (the tear). This is the one place to set it. Empty = the material on the wall strip model, worked out on Rebuild")]
    public Material _TearMaterial;

    [FoldoutGroup("Walls")]
    [Tooltip("The border behind's wall (the smoke). Its cracks are off. Empty = a stand-in, HDRP's default material tinted grey")]
    public Material _SmokeMaterial;

    [FoldoutGroup("Walls")]
    [Tooltip("The tear-to-smoke swap: how far through Border Blend Time the tear wall finishes animating out and the smoke wall starts animating in (0.5 = halfway)")]
    [Range(0.05f, 0.95f)] public float _SwapPoint = 0.5f;

    [FoldoutGroup("Zone Opening", expanded: true)]
    [Tooltip("How long the new zone takes to rise and its floor light to change, in seconds. The zone behind changes to frozen at the same time")]
    [Min(0.01f)] public float _RiseTime = 1.2f;

    [FoldoutGroup("Zone Opening")]
    [Tooltip("The rise's shape over time (X = 0 to 1 of Rise Time, Y = 0 to 1 of the way)")]
    public AnimationCurve _RiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Zone Opening")]
    [Tooltip("How long the old border takes to go from tear to smoke (and the one behind it from smoke to off), in seconds")]
    [Min(0.01f)] public float _BorderBlendTime = 1f;

    [FoldoutGroup("Zone Opening")]
    [Tooltip("The tear-to-smoke blend's shape over time")]
    public AnimationCurve _BorderBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Zone Opening")]
    [Tooltip("How long after the step starts the new border starts lighting up, in seconds")]
    [Min(0f)] public float _RevealDelay = 0.6f;

    [FoldoutGroup("Zone Opening")]
    [Tooltip("How long the light takes to sweep around the new border and out along its cracks, in seconds")]
    [Min(0.01f)] public float _RevealTime = 2f;

    [FoldoutGroup("Zone Opening")]
    [Tooltip("The sweep's shape over time")]
    public AnimationCurve _RevealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Zone Opening")]
    [Tooltip("Pause between steps when catching up more than one zone, in seconds")]
    [Min(0f)] public float _StepGap = 0.3f;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("Smoke particles along the smoky border. Off: the smoke material handles the look on its own")]
    public bool _UseSmokeParticles = false;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("Optional smoke particle prefab (Steve's). Its Simulation Space must be World and it should move its own particles (Velocity over Lifetime). Empty = stand-in grey puffs made by code")]
    public ParticleSystem _SmokePrefab;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("How many smoke particles a fully smoky border makes per second, per meter of ring")]
    [Min(0f)] public float _SmokePerMeter = 1.5f;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("Stand-in smoke: how long a puff lasts, in seconds")]
    [Min(0.01f)] public float _StandInSmokeLifetime = 2.5f;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("Stand-in smoke: how big a puff starts, in meters (it shrinks away over its life)")]
    [Min(0.01f)] public float _StandInSmokeSize = 0.35f;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("Stand-in smoke: how fast puffs rise, in meters per second")]
    [Min(0f)] public float _StandInSmokeRise = 0.4f;

    [FoldoutGroup("Smoke Particles")]
    [Tooltip("Stand-in smoke: puff color")]
    public Color _StandInPuffColor = new Color(0.45f, 0.45f, 0.5f, 1f);

    [FoldoutGroup("Wall Strip Import")]
    [Tooltip("The short straight piece of wall the ring walls are built from (M_ZoneWall_WallEffectCard). Length along X, height along Y, its Y = 0 is the raised floor. Empty = a stand-in card made by code. Saving over it from Maya rebuilds every wall and crack by itself")]
    public Mesh _WallStrip;

    [FoldoutGroup("Wall Strip Import")]
    [Tooltip("Optional narrower strip just for the floor cracks. Empty = the cracks use the wall strip")]
    public Mesh _CrackStrip;

    [FoldoutGroup("Wall Strip Import")]
    [Tooltip("Scales the strips when they're read, if they come in at the wrong size (the Ring Wall's Strip Length readout should say 4 for Steve's 4 m card)")]
    [Min(0.001f)] public float _StripScale = 1f;

    [FoldoutGroup("Wall Strip Import")]
    [Tooltip("Turns the strips around along their length, if they come in backwards")]
    public bool _FlipLength = false;

    [FoldoutGroup("Wall Strip Import")]
    [Tooltip("Turns the strips' front and back around. By default the strip's +Z side faces the core")]
    public bool _FlipDepth = false;

    [FoldoutGroup("Wall Strip Import")]
    [Tooltip("Where Rebuild saves the wall and crack meshes (one asset per ring each). Made if it doesn't exist")]
    public string _MeshFolder = "Assets/Generated/Rune Field Zones";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The height value both wall materials share (the Reference name in Shader Graph). The walls animate it to swap out and in. Each material's own value on the material is its full height")]
    public string _HeightProperty = "_GradiantHeight";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The height value that means 'gone' (the wall fully animated out)")]
    public float _HiddenHeight = 0f;

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The light-up sweep on the tear material (0 = dark, 1 = fully lit), read with the mesh's reveal order (UV2.x). Optional: the height also rises with it")]
    public string _RevealProperty = "_Reveal";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The overcharge pulse (later, always 0 for now)")]
    public string _OverchargeProperty = "_Overcharge";

    [FoldoutGroup("Stand-In Look")]
    [Tooltip("The material color the stand-in tints (_BaseColor for HDRP Lit). Only used where a wall material is empty and HDRP's default material stands in")]
    public string _StandInColorProperty = "_BaseColor";

    [FoldoutGroup("Stand-In Look")]
    [Tooltip("Stand-in color of the tear wall")]
    public Color _StandInTearColor = new Color(0.75f, 0.45f, 1f, 1f);

    [FoldoutGroup("Stand-In Look")]
    [Tooltip("Stand-in color of the smoke wall")]
    public Color _StandInSmokeColor = new Color(0.35f, 0.35f, 0.38f, 1f);

    #region Getters
    public ZoneLightEntry GetLight(RuneFieldType.ZoneState state)
    {
        //the zone light look for a state
        if (state == RuneFieldType.ZoneState.Frozen) return _FrozenLight;
        if (state == RuneFieldType.ZoneState.Locked) return _LockedLight;
        return _OpenLight;
    }

    public float GetDrop(RuneFieldType.ZoneState state)
    {
        //how far below the raised level a zone in this state sits, in meters. Only locked zones drop
        return state == RuneFieldType.ZoneState.Locked ? _LiftHeight : 0f;
    }

    public float GetStepLength()
    {
        //how long one zone opening step takes, in seconds: whichever part finishes last
        return Mathf.Max(_RiseTime, _BorderBlendTime, _RevealDelay + _RevealTime);
    }
    #endregion

    #region Tools
#if UNITY_EDITOR
    [FoldoutGroup("Walls"), Button("Rebuild All Borders"), GUIColor(0.6f, 1f, 0.6f), PropertyTooltip("Rebuilds every ring's wall and cracks in the open scenes. Press after changing anything in Walls or Wall Strip Import")]
    void RebuildAllBorders()
    {
        //editor button: rebuilds the walls of every rune field in the open scenes that uses this asset, so the wall settings and the rebuild are in one place
        //(the same as Rebuild All Borders on the field's zones component). Searching is fine here, it's an editor tool
        RuneFieldZonesObject[] fields = FindObjectsOfType<RuneFieldZonesObject>(true); //true: inactive ones count too
        int rebuilt = 0;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].GetSettings() != this) continue; //a field using a different zones asset
            fields[i].RebuildAllBorders();
            rebuilt++;
        }
        if (rebuilt == 0) Debug.LogWarning($"Warning! No rune field using {name} is open. Open the 3D rune field scene, then press Rebuild All Borders again...", this);
    }

    [FoldoutGroup("Wall Strip Import"), Button("Rebuild All Borders"), PropertyTooltip("Rebuilds every ring's wall and cracks, to see a change to the strip settings")]
    void RebuildAfterStripChange()
    {
        //the same rebuild, repeated here so it's next to the strip settings
        RebuildAllBorders();
    }

    [FoldoutGroup("Walls"), Button("Use Wall Strip's Material"), ShowIf("@_TearMaterial == null")]
    void UseWallStripMaterial()
    {
        //editor button: copies the wall strip model's material into Tear Material, so the tear material is set in one obvious place
        Material found = FindWallStripMaterial();
        if (found == null) { Debug.LogWarning($"Warning! {name}: no Wall Strip set, or its model has no material. Drag the tear material into Tear Material by hand...", this); return; }
        UnityEditor.Undo.RecordObject(this, "Use Wall Strip's Material");
        _TearMaterial = found;
        UnityEditor.EditorUtility.SetDirty(this); //tells the editor the asset changed, so it gets saved
    }

    public Material FindWallStripMaterial()
    {
        //editor only: the material the wall strip model shows its mesh with (set in the model's import settings). Null with no strip, or none found
        //the borders use it when Tear Material is empty
        if (_WallStrip == null) return null;
        GameObject model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GetAssetPath(_WallStrip)); //the FBX loads as a GameObject
        if (model == null) return null;

        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh != _WallStrip) continue;
            MeshRenderer modelRenderer = filters[i].GetComponent<MeshRenderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null) return modelRenderer.sharedMaterial;
        }
        return null;
    }

    public static RuneFieldZonesSO FindInProject()
    {
        //editor only: finds the zones asset in the project, so adding the zones component fills it in by itself. Null if there isn't one
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RuneFieldZonesSO");
        if (guids.Length == 0) return null;
        if (guids.Length > 1) Debug.LogWarning("Warning! More than one Rune Field Zones asset in the project, using the first one found...");

        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
        return UnityEditor.AssetDatabase.LoadAssetAtPath<RuneFieldZonesSO>(path);
    }
#endif
    #endregion
}

[System.Serializable]
public class ZoneLightEntry
{
    [Tooltip("The zone light's color")]
    public Color _Color = Color.white;

    [Tooltip("The zone light's brightness, in the key light's unit (lux for a directional light). The key light is 130000 in the 3D field scene right now")]
    [Min(0f)] public float _Intensity = 30000f;

    public ZoneLightEntry()
    {
        //empty constructor so the inspector can make a new entry
    }

    public ZoneLightEntry(Color color, float intensity)
    {
        //used for the defaults above
        _Color = color;
        _Intensity = intensity;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the 3D rune field's zone look, in one asset (SO_RuneField_Zones) so changes made in play mode stay after stopping
//holds the floor lift and zone light looks, the strip the walls and cracks are built from, the border shader's property names,
//the stand-in look used until Steve's shader exists, the stand-in smoke, and the zone opening timings (see Zone Visuals Plan)
//the code never writes to this asset, it only reads it. Edit it by hand while playing to find the look
[CreateAssetMenu(fileName = "SO_RuneField_Zones", menuName = "Rune Field/Zones", order = 2)]
public class RuneFieldZonesSO : ScriptableObject
{
    [Header("Lift")]
    [Tooltip("How far a locked zone sits below the raised floor, in meters. Open and frozen zones sit at the raised level (the field's height). Keep it smaller than the floors' outer skirt and the walls' Bottom Depth")]
    [Min(0f)] public float _LiftHeight = 0.25f;

    [Header("Zone Lights")]
    [Tooltip("The zone light on the open zone (the one the player builds in). Neutral. Intensity is in the key light's unit (lux for a directional light)")]
    public ZoneLightEntry _OpenLight = new ZoneLightEntry(new Color(1f, 0.97f, 0.92f, 1f), 30000f);

    [Tooltip("The zone light on frozen zones (the ones the shade evolved out of). A bit blue, the 'frozen, locked in' feel")]
    public ZoneLightEntry _FrozenLight = new ZoneLightEntry(new Color(0.65f, 0.8f, 1f, 1f), 30000f);

    [Tooltip("The zone light on locked zones (not reached yet). Muted and darker. Purple for now, might end up blue or green [undecided]")]
    public ZoneLightEntry _LockedLight = new ZoneLightEntry(new Color(0.6f, 0.5f, 0.8f, 1f), 8000f);

    [Header("Strip")]
    [Tooltip("The short straight piece of wall the ring walls are built from (M_ZoneWall_WallEffectCard). Length along X, height along Y, its Y = 0 is the raised floor. Empty = a stand-in card made by code. Saving over it from Maya rebuilds every wall and crack by itself")]
    public Mesh _WallStrip;

    [Tooltip("Optional narrower strip just for the floor cracks. Empty = the cracks use the wall strip")]
    public Mesh _CrackStrip;

    [Tooltip("Scales the strips when they're read, if they come in at the wrong size (the Ring Wall's Strip Length readout should say 4 for Steve's 4 m card)")]
    [Min(0.001f)] public float _StripScale = 1f;

    [Tooltip("Turns the strips around along their length, if they come in backwards")]
    public bool _FlipLength = false;

    [Tooltip("Turns the strips' front and back around. By default the strip's +Z side faces the core")]
    public bool _FlipDepth = false;

    [Tooltip("The material on every wall and crack (MAT_RuneField_Zone_Border_0 once it exists). Empty = HDRP's default material, tinted by the stand-in look")]
    public Material _BorderMaterial;

    [Tooltip("Where Rebuild saves the wall and crack meshes (one asset per ring each). Made if it doesn't exist")]
    public string _MeshFolder = "Assets/Generated/Rune Field Zones";

    [Header("Shader Properties")]
    [Tooltip("The border shader's tear amount (0 to 1). The Reference name in Shader Graph")]
    public string _TearProperty = "_Tear";

    [Tooltip("The border shader's smoke amount (0 to 1, always 0 on cracks)")]
    public string _SmokeProperty = "_Smoke";

    [Tooltip("The border shader's light-up sweep (0 = dark, 1 = fully lit)")]
    public string _RevealProperty = "_Reveal";

    [Tooltip("The border shader's overcharge pulse (later, always 0 for now)")]
    public string _OverchargeProperty = "_Overcharge";

    [Header("Stand-In Look")]
    [Tooltip("Tints the walls and cracks by color while there's no border shader yet, so tear / smoke / off can be seen. Turn off once Steve's shader is on Border Material")]
    public bool _UseStandInLook = true;

    [Tooltip("Hides walls (and cracks) completely while they're off. The stand-in material can't fade out, Steve's shader can")]
    public bool _HideWhenOff = true;

    [Tooltip("The material color the stand-in look tints: _BaseColor for HDRP Lit, _UnlitColor for HDRP Unlit")]
    public string _StandInColorProperty = "_BaseColor";

    [Tooltip("Stand-in color of the bright tear")]
    public Color _StandInTearColor = new Color(0.75f, 0.45f, 1f, 1f);

    [Tooltip("Stand-in color of the smoky border")]
    public Color _StandInSmokeColor = new Color(0.35f, 0.35f, 0.38f, 1f);

    [Tooltip("Stand-in color while off (only seen with Hide When Off unticked)")]
    public Color _StandInOffColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Header("Smoke")]
    [Tooltip("Optional smoke particle prefab (Steve's). Its Simulation Space must be World and it should move its own particles (Velocity over Lifetime). Empty = stand-in grey puffs made by code")]
    public ParticleSystem _SmokePrefab;

    [Tooltip("How many smoke particles a fully smoky border makes per second, per meter of ring")]
    [Min(0f)] public float _SmokePerMeter = 1.5f;

    [Tooltip("Stand-in smoke: how long a puff lasts, in seconds")]
    [Min(0.01f)] public float _StandInSmokeLifetime = 2.5f;

    [Tooltip("Stand-in smoke: how big a puff starts, in meters (it shrinks away over its life)")]
    [Min(0.01f)] public float _StandInSmokeSize = 0.35f;

    [Tooltip("Stand-in smoke: how fast puffs rise, in meters per second")]
    [Min(0f)] public float _StandInSmokeRise = 0.4f;

    [Tooltip("Stand-in smoke: puff color")]
    public Color _StandInPuffColor = new Color(0.45f, 0.45f, 0.5f, 1f);

    [Header("Zone Opening")]
    [Tooltip("How long the new zone takes to rise and its floor light to change, in seconds. The zone behind changes to frozen at the same time")]
    [Min(0.01f)] public float _RiseTime = 1.2f;

    [Tooltip("The rise's shape over time (X = 0 to 1 of Rise Time, Y = 0 to 1 of the way)")]
    public AnimationCurve _RiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("How long the old border takes to go from tear to smoke (and the one behind it from smoke to off), in seconds")]
    [Min(0.01f)] public float _BorderBlendTime = 1f;

    [Tooltip("The tear-to-smoke blend's shape over time")]
    public AnimationCurve _BorderBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("How long after the step starts the new border starts lighting up, in seconds")]
    [Min(0f)] public float _RevealDelay = 0.6f;

    [Tooltip("How long the light takes to sweep around the new border and out along its cracks, in seconds")]
    [Min(0.01f)] public float _RevealTime = 2f;

    [Tooltip("The sweep's shape over time")]
    public AnimationCurve _RevealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Pause between steps when catching up more than one zone, in seconds")]
    [Min(0f)] public float _StepGap = 0.3f;

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

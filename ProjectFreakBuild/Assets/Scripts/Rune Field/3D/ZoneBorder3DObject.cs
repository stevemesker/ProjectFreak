using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//one ring's border on the 3D rune field: its wall of light, the floor cracks leaving it, and its smoke (see Zone Visuals Plan)
//ring k is zone k's outer edge. Only gate rings get one (1 to zone count - 1): the field's edge never glows
//editor: holds the ring wall tool. Rebuild Ring bends copies of the strip around the ring (lean, noise, hand spots, gaps at evolution gates)
//and bakes the wall and its cracks into mesh assets. The wall sits under the field and stays put; the cracks sit under the zone outside the ring and lift with it
//play: swaps the wall between the tear material (forward border) and the smoke material (the border behind), and animates their shared height value
//(through a MaterialPropertyBlock per renderer) so a wall can animate out, swap, and animate back in. Cracks only ever show the tear
[DisallowMultipleComponent]
public class ZoneBorder3DObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Which ring this is: 1 is zone 1's outer edge (1 zone width from the core). Gates on this ring get gaps in the wall")]
    [SerializeField, Range(1, Zone3DObject.MaxZone)] int _Ring = 1;

    [Header("Settings")]
    [FoldoutGroup("Wall Shape"), Tooltip("How far below the raised floor the wall's bottom reaches, in meters. The part of the strip under its Y = 0 is stretched to this, so the wall always goes below a lowered zone. Keep it bigger than the lift height")]
    [SerializeField, Min(0f)] float _BottomDepth = 0.4f;

    [FoldoutGroup("Wall Shape"), Tooltip("Multiplies the strip's height above the floor")]
    [SerializeField, Min(0f)] float _HeightScale = 1f;

    [FoldoutGroup("Wall Shape"), Tooltip("Moves the whole wall out (or in, if negative) from the ring, in meters")]
    [SerializeField] float _RadiusOffset = 0f;

    [FoldoutGroup("Wall Shape"), Tooltip("Where the light-up sweep starts, in degrees around the ring (0 = the field's right, 90 = its forward). It spreads both ways from here and meets on the far side")]
    [SerializeField, Range(0f, 360f)] float _RevealStartAngle = 90f;

    [FoldoutGroup("Lean"), Tooltip("Tilts the wall outward the higher it goes, in degrees (negative tilts it in toward the core)")]
    [SerializeField, Range(-60f, 60f)] float _LeanAngle = 0f;

    [FoldoutGroup("Lean"), Tooltip("The lean's shape up the wall. X = height (0 floor, 1 top), Y = how much of the lean. A straight line is a straight slant, a curve that rises late flares out at the top")]
    [SerializeField] AnimationCurve _LeanCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [FoldoutGroup("Noise"), Tooltip("How much the wall's height wobbles around the ring (0.2 = up to 20% taller or shorter)")]
    [SerializeField, Range(0f, 1f)] float _HeightNoise = 0.15f;

    [FoldoutGroup("Noise"), Tooltip("How far the wall wobbles in and out around the ring, in meters")]
    [SerializeField, Min(0f)] float _WobbleNoise = 0.05f;

    [FoldoutGroup("Noise"), Tooltip("How much the lean wobbles around the ring, in degrees")]
    [SerializeField, Min(0f)] float _LeanNoise = 0f;

    [FoldoutGroup("Noise"), Tooltip("How bumpy the noise is: higher = more, smaller bumps around the ring. The noise loops, so there's no seam where the ring closes")]
    [SerializeField, Range(0.1f, 20f)] float _NoiseBumps = 4f;

    [FoldoutGroup("Noise"), Tooltip("Changes the noise pattern. Give each ring its own so they don't look copied")]
    [SerializeField] int _NoiseSeed = 0;

    [FoldoutGroup("Hand Spots"), Tooltip("Hand-placed changes around the ring. Drag each spot's handle in the scene view while this object is selected")]
    [SerializeField] List<WallSpotEntry> _Spots = new List<WallSpotEntry>();

    [FoldoutGroup("Hand Spots"), Tooltip("How a spot fades out toward its edges. X = 0 at the spot's edge to 1 at its center, Y = how much of the spot")]
    [SerializeField] AnimationCurve _SpotFalloff = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Gate Gaps"), Tooltip("Extra room around each evolution gate where the wall is fully gone, past the gate's own edge, in meters")]
    [SerializeField, Min(0f)] float _GapMargin = 0.3f;

    [FoldoutGroup("Gate Gaps"), Tooltip("How long the slope back up to full height is on each side of a gap, in meters along the ring")]
    [SerializeField, Min(0.01f)] float _GapRampWidth = 1.5f;

    [FoldoutGroup("Gate Gaps"), Tooltip("The slope's shape. X = 0 at the gap to 1 at full height, Y = how much of the wall")]
    [SerializeField] AnimationCurve _GapRampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("References")]
    [Tooltip("Draws the wall. Made by Rebuild Ring if empty (a 'Wall' child)")]
    [SerializeField] MeshRenderer _WallRenderer;

    [Tooltip("Draws all of this ring's cracks as one mesh. It sits under the zone outside the ring so it lifts with that floor. Made by Rebuild Ring if empty")]
    [SerializeField] MeshRenderer _CrackRenderer;

    [Tooltip("This ring's cracks, branches included. Fill Crack List (Tools) finds every crack that starts on this ring")]
    [SerializeField] List<ZoneCrackPath> _Cracks = new List<ZoneCrackPath>();

    [Header("Runtime Data")]
    [Tooltip("The ring's radius the wall was last built at, in meters (read only)")]
    [SerializeField, ReadOnly] float _Radius;

    [Tooltip("How long the strip is, in meters, as last read (read only). Steve's card should say 4")]
    [SerializeField, ReadOnly] float _StripLength;

    [Tooltip("How many strip copies make up the wall (read only)")]
    [SerializeField, ReadOnly] int _CopyCount;

    [Tooltip("The angles of the evolution gates found on this ring at the last build, in degrees (read only)")]
    [SerializeField, ReadOnly] List<float> _GateAngles = new List<float>();

    [Tooltip("Half a gate's width at the last build, in meters (read only)")]
    [SerializeField, ReadOnly] float _GateHalfWidth = 0.5f;

    [Tooltip("The tear material: the one on the wall strip model, found on the last Rebuild (read only). The zones asset's Tear Material overrides it")]
    [SerializeField, ReadOnly] Material _StripMaterial;

    [Tooltip("Which material the wall is showing (read only)")]
    [SerializeField, ReadOnly] RuneFieldType.BorderLook _Look = RuneFieldType.BorderLook.Off;

    [Tooltip("How far up the wall is animated, 0 (gone) to 1 (full) (read only)")]
    [SerializeField, ReadOnly] float _Height;

    //local variables
    RuneFieldTint _wallTint; //sets the shader values on the wall
    RuneFieldTint _crackTint; //and on the cracks
    RuneFieldZonesSO _lookSettings; //the zones asset last used to show a look (for the smoke)
    ParticleSystem _smokeEmitter;
    bool _warnedTear; //already warned about a missing tear material
    bool _warnedSmoke; //already warned about a missing smoke material
    float _smokeToEmit; //smoke particles owed but not made yet (fractions add up over frames)
    const string WallName = "Wall";
    const int MaxSmokePerFrame = 200; //so a long hitch can't burst thousands of puffs at once

    private void Update()
    {
        //makes smoke along the ring while this border is smoky. Real time, so it works while the game is paused
        if (_lookSettings == null || _lookSettings._UseSmokeParticles == false || Application.isPlaying == false) return;
        if (_Look != RuneFieldType.BorderLook.Smoke || _Height <= 0.001f) return;
        ParticleSystem emitter = GetSmokeEmitter();
        if (emitter == null) return;

        _smokeToEmit += _lookSettings._SmokePerMeter * GetCircumference() * _Height * Time.unscaledDeltaTime;
        int count = Mathf.Min(Mathf.FloorToInt(_smokeToEmit), MaxSmokePerFrame);
        _smokeToEmit -= Mathf.FloorToInt(_smokeToEmit);
        for (int i = 0; i < count; i++) EmitSmoke(emitter);
    }

    #region Look
    public void ShowLook(RuneFieldType.BorderLook look, float height, float crackHeight, float reveal, RuneFieldZonesSO lookSettings)
    {
        //shows the wall with the tear or smoke material (or hides it), animated "height" of the way up (0 gone, 1 full)
        //the cracks show the tear material at crackHeight while the wall is the tear, and are hidden otherwise (they never smoke)
        //reveal is the light-up sweep (0 to 1), for a tear material that reads it
        if (lookSettings == null) return;
        _Look = look;
        _Height = Mathf.Clamp01(height);
        _lookSettings = lookSettings;
        if (_wallTint == null || _crackTint == null) MakeTints(lookSettings);

        Material wallMaterial = look == RuneFieldType.BorderLook.Smoke ? GetSmokeMaterial(lookSettings) : GetTearMaterial(lookSettings);
        bool wallOn = look != RuneFieldType.BorderLook.Off && _Height > 0.001f;
        ApplyLook(_wallTint, _WallRenderer, wallOn, wallMaterial, _Height, reveal, look == RuneFieldType.BorderLook.Smoke ? lookSettings._StandInSmokeColor : lookSettings._StandInTearColor, lookSettings);

        bool cracksOn = look == RuneFieldType.BorderLook.Tear && crackHeight > 0.001f;
        ApplyLook(_crackTint, _CrackRenderer, cracksOn, GetTearMaterial(lookSettings), Mathf.Clamp01(crackHeight), reveal, lookSettings._StandInTearColor, lookSettings);
    }

    void MakeTints(RuneFieldZonesSO lookSettings)
    {
        //one tint per renderer, so the wall and the cracks can show different values with the same material
        _wallTint = new RuneFieldTint(new List<Renderer> { _WallRenderer }, lookSettings._StandInColorProperty);
        _crackTint = new RuneFieldTint(new List<Renderer> { _CrackRenderer }, lookSettings._StandInColorProperty);
    }

    void ApplyLook(RuneFieldTint tint, Renderer target, bool on, Material material, float height, float reveal, Color standInColor, RuneFieldZonesSO lookSettings)
    {
        //turns a renderer on or off, swaps its material if needed, and sends its values
        //the height value goes from Hidden Height (0) to the material's own value (1), so each material keeps its full height on the material itself
        if (target == null) return;
        if (target.enabled != on) target.enabled = on;
        if (on == false) return;
        if (material != null && target.sharedMaterial != material) target.sharedMaterial = material; //sharedMaterial swaps without making a copy of the material

        int heightID = Shader.PropertyToID(lookSettings._HeightProperty);
        float fullHeight = material != null && material.HasProperty(heightID) ? material.GetFloat(heightID) : 1f; //the value set on the material

        tint.StartValues();
        tint.PutFloat(heightID, Mathf.Lerp(lookSettings._HiddenHeight, fullHeight, height));
        tint.PutFloat(Shader.PropertyToID(lookSettings._RevealProperty), reveal);
        tint.PutFloat(Shader.PropertyToID(lookSettings._OverchargeProperty), 0f); //todo: the overcharge pulse before a cut on save [Notes for the future]
        if (material == GetDefaultMaterial()) tint.PutTint(standInColor); //only the stand-in gets tinted, a real material keeps its own colors
        tint.ApplyValues();
    }

    Material GetTearMaterial(RuneFieldZonesSO lookSettings)
    {
        //the zones asset's Tear Material, otherwise the strip model's own material, otherwise the stand-in (with a warning, once)
        if (lookSettings._TearMaterial != null) return lookSettings._TearMaterial;
#if UNITY_EDITOR
        if (_StripMaterial == null) _StripMaterial = lookSettings.FindWallStripMaterial(); //editor: picks up a strip set since the last Rebuild
#endif
        if (_StripMaterial != null) return _StripMaterial;
        WarnStandIn(ref _warnedTear, "tear", "Set Tear Material on the zones asset (or press its Use Wall Strip's Material button)");
        return GetDefaultMaterial();
    }

    Material GetSmokeMaterial(RuneFieldZonesSO lookSettings)
    {
        //the zones asset's Smoke Material, otherwise the stand-in (with a warning, once)
        if (lookSettings._SmokeMaterial != null) return lookSettings._SmokeMaterial;
        WarnStandIn(ref _warnedSmoke, "smoke", "Set Smoke Material on the zones asset");
        return GetDefaultMaterial();
    }

    void WarnStandIn(ref bool warned, string lookName, string fix)
    {
        //says once per border (per session) that a wall is falling back to the stand-in material. "ref" lets this change the bool passed in
        if (warned) return;
        warned = true;
        Debug.LogWarning($"Warning! {gameObject.name} has no {lookName} material, showing HDRP's default material instead. {fix}...", this);
    }

    public RuneFieldType.BorderLook GetLook() { return _Look; }
    #endregion

    #region Smoke
    ParticleSystem GetSmokeEmitter()
    {
        //the smoke particle system, made the first time it's needed: Steve's prefab if the zones asset has one, otherwise stand-in puffs
        if (_smokeEmitter != null) return _smokeEmitter;
        if (_lookSettings._SmokePrefab != null)
        {
            _smokeEmitter = Instantiate(_lookSettings._SmokePrefab, transform);
            _smokeEmitter.name = "Smoke";
            _smokeEmitter.Play();
            return _smokeEmitter;
        }
        _smokeEmitter = MakeStandInSmoke();
        return _smokeEmitter;
    }

    ParticleSystem MakeStandInSmoke()
    {
        //grey puffs that rise and shrink away. Its own emission is off: this script places every puff along the ring (EmitSmoke)
        GameObject smokeObject = new GameObject("Smoke (stand-in)");
        smokeObject.transform.SetParent(transform, false);
        ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>(); //adding a particle system also adds its renderer
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); //settings like these can only change while it's stopped

        ParticleSystem.MainModule main = smoke.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World; //puffs stay where they were made, even if the border moves
        main.useUnscaledTime = true; //keeps moving while the game is paused
        main.startLifetime = _lookSettings._StandInSmokeLifetime;
        main.startSize = _lookSettings._StandInSmokeSize;
        main.startSpeed = 0f;
        main.startColor = _lookSettings._StandInPuffColor;
        main.maxParticles = 5000;

        ParticleSystem.EmissionModule emission = smoke.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule size = smoke.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f)); //full size at birth, gone at the end

        ParticleSystemRenderer particleRenderer = smokeObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GetDefaultMaterial(); //opaque stand-in squares, Steve's prefab brings the real look
        smoke.Play();
        return smoke;
    }

    void EmitSmoke(ParticleSystem emitter)
    {
        //one puff at a random spot on the ring, skipping the gate gaps (a spot in a gap is less likely to get one, the slope fades it in)
        float angle = 0f;
        bool found = false;
        for (int tries = 0; tries < 4 && found == false; tries++)
        {
            angle = Random.Range(0f, 360f);
            found = Random.value <= GetGapMask(angle);
        }
        if (found == false) return;

        ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams(); //EmitParams overrides a few start values for one particle
        puff.position = transform.TransformPoint(GetRingPoint(angle, _Radius));
        if (_lookSettings._SmokePrefab == null) puff.velocity = transform.up * _lookSettings._StandInSmokeRise; //the stand-in has nothing else moving it
        emitter.Emit(puff, 1);
    }
    #endregion

    #region Ring Math
    public int GetRing() { return _Ring; }
    public float GetRadius() { return _Radius; }

    public float GetCircumference()
    {
        return 2f * Mathf.PI * _Radius;
    }

    public static Vector3 GetRingPoint(float angleDegrees, float radius)
    {
        //a point on a circle in this border's space (around its up), at the floor. 0 degrees = its right (+X), 90 = its forward (+Z)
        float radians = angleDegrees * Mathf.Deg2Rad; //Mathf.Cos / Sin work in radians
        return new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
    }

    public float GetAngle(Vector3 worldPosition)
    {
        //the angle around this border of a spot in the world, in degrees (0 to 360). Atan2 turns a direction into an angle
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float angle = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
        return angle < 0f ? angle + 360f : angle;
    }

    float GetArcDistance(float angleA, float angleB, float radius)
    {
        //how far apart two angles are along the ring, in meters (the short way round). DeltaAngle handles going past 360
        return Mathf.Abs(Mathf.DeltaAngle(angleA, angleB)) * Mathf.Deg2Rad * radius;
    }

    public float GetGapMask(float angle)
    {
        //1 = normal wall, 0 = in a gate gap. Across the gate plus the margin it's 0, then it slopes back up over the ramp width
        float mask = 1f;
        float fullGap = _GateHalfWidth + _GapMargin;
        for (int i = 0; i < _GateAngles.Count; i++)
        {
            float distance = GetArcDistance(angle, _GateAngles[i], _Radius);
            float gateMask = distance <= fullGap ? 0f : _GapRampCurve.Evaluate(Mathf.Clamp01((distance - fullGap) / _GapRampWidth));
            mask = Mathf.Min(mask, gateMask);
        }
        return mask;
    }

    public float GetRevealMeters(float angle)
    {
        //how far the light-up sweep has to travel to reach this angle, in meters: it starts at Reveal Start Angle and spreads both ways
        return GetArcDistance(angle, _RevealStartAngle, _Radius);
    }
    #endregion

    #region Building
    [FoldoutGroup("Ring Tools", true, 10), Button("Rebuild Ring"), GUIColor(0.6f, 1f, 0.6f)]
    public void RebuildRing()
    {
        //editor button: rebuilds the wall and this ring's cracks and saves them as mesh assets. Press it after changing any setting, a gate, the zone width, or a crack
        //the zones component's Rebuild All Borders does every ring, and saving over the strip from Maya rebuilds them all by itself
#if UNITY_EDITOR
        if (Application.isPlaying) { Debug.LogWarning($"Warning! {gameObject.name}: rebuild in edit mode, not while playing...", this); return; }
        RuneFieldZonesObject zones = GetComponentInParent<RuneFieldZonesObject>();
        if (zones == null) { Debug.LogError($"Error! {gameObject.name} isn't under the rune field's zones component, it can't find the zone settings", this); return; }
        RuneFieldSettingsSO fieldSettings = zones.GetFieldSettings();
        RuneFieldZonesSO lookSettings = zones.GetSettings();
        if (fieldSettings == null || lookSettings == null) { Debug.LogError($"Error! {gameObject.name} can't find the Rune Field Settings or the zones asset, not rebuilding", this); return; }

        ZoneStripBuilder wallStrip = ZoneStripBuilder.FromMesh(lookSettings._WallStrip, lookSettings._StripScale, lookSettings._FlipLength, lookSettings._FlipDepth, out string problem);
        if (wallStrip == null) { Debug.LogError($"Error! {gameObject.name} can't use the wall strip: {problem}", this); return; }
        ZoneStripBuilder crackStrip = wallStrip;
        if (lookSettings._CrackStrip != null)
        {
            crackStrip = ZoneStripBuilder.FromMesh(lookSettings._CrackStrip, lookSettings._StripScale, lookSettings._FlipLength, lookSettings._FlipDepth, out string crackProblem);
            if (crackStrip == null) { Debug.LogWarning($"Warning! {gameObject.name} can't use the crack strip ({crackProblem}), using the wall strip for cracks...", this); crackStrip = wallStrip; }
        }
        if (wallStrip._EndsMatch == false) Debug.LogWarning($"Warning! The wall strip's two end edges don't match (same vertex heights and depths), so its copies can't be welded. The wall is built with seams...", this);

        UnityEditor.Undo.RecordObject(this, "Rebuild Ring");
        _Radius = fieldSettings.ToMeters(fieldSettings.GetRingRadius(_Ring)) + _RadiusOffset;
        _StripLength = wallStrip._Length;
        _GateHalfWidth = fieldSettings.ToMeters(fieldSettings._RuneSize) * 0.5f; //nodes are rune sized
        _StripMaterial = lookSettings.FindWallStripMaterial();
        FindGates(fieldSettings);
        FillCrackList();

        //the sweep's longest distance (the far side of the ring, or the tip of the longest crack), so its order fits 0 to 1 and walls and cracks light at the same speed
        //cracks snap twice, so a branch listed before its parent still lands on the parent's final spot
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < _Cracks.Count; i++) _Cracks[i].SnapStart();
        }
        float maxReveal = Mathf.PI * _Radius;
        for (int i = 0; i < _Cracks.Count; i++) maxReveal = Mathf.Max(maxReveal, _Cracks[i].GetStartRevealMeters() + _Cracks[i].GetLength());

        MakeRenderers(zones, lookSettings);
        Mesh wall = ZoneStripBuilder.SaveMesh(BuildWallMesh(wallStrip, maxReveal), lookSettings._MeshFolder, $"M_RuneField_ZoneWall_Ring{_Ring}");
        SetMesh(_WallRenderer, wall);

        if (_CrackRenderer != null)
        {
            Mesh cracks = _Cracks.Count > 0 ? ZoneStripBuilder.SaveMesh(BuildCrackMesh(crackStrip, maxReveal), lookSettings._MeshFolder, $"M_RuneField_ZoneCracks_Ring{_Ring}") : null;
            SetMesh(_CrackRenderer, cracks);
        }
        UnityEditor.AssetDatabase.SaveAssets();
        _wallTint = null; //the renderers may be new, so the tints get made again
        _crackTint = null;
#endif
    }

    void FindGates(RuneFieldSettingsSO fieldSettings)
    {
        //finds every evolution gate sitting on this ring (from the field's node list) and keeps their angles for the gaps
        _GateAngles.Clear();
        RuneField3DObject field = GetComponentInParent<RuneField3DObject>();
        if (field == null) { Debug.LogWarning($"Warning! {gameObject.name} isn't under the 3D rune field, so it can't find the gates. Building with no gaps...", this); return; }

        List<Node3DObject> nodes = field.GetNodeObjects();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null || nodes[i].IsGate() == false) continue;
            float distance = field.WorldToField(nodes[i].transform.position).magnitude;
            if (fieldSettings.IsOnRing(distance, out int ring) == false || ring != _Ring) continue;
            _GateAngles.Add(GetAngle(nodes[i].transform.position));
        }
    }

    Mesh BuildWallMesh(ZoneStripBuilder strip, float maxReveal)
    {
        //bends copies of the strip around the ring: along the strip = around the ring, depth = radius, height stays height
        //the copies are stretched a little so a whole number of them closes the circle exactly
        float circumference = GetCircumference();
        _CopyCount = Mathf.Max(3, Mathf.RoundToInt(circumference / strip._Length));
        strip.Layout(_CopyCount, circumference / (_CopyCount * strip._Length));

        System.Random random = new System.Random(_NoiseSeed); //a seeded random always gives the same numbers, so the same seed gives the same wall
        Vector2 heightOffset = new Vector2((float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);
        Vector2 wobbleOffset = new Vector2((float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);
        Vector2 leanOffset = new Vector2((float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);

        int count = strip._SourceIndex.Count;
        List<Vector3> positions = new List<Vector3>(count);
        List<Vector3> normals = new List<Vector3>(count);
        List<Vector2> uv0 = new List<Vector2>(count);
        List<Vector2> uv2 = new List<Vector2>(count);
        List<Vector2> uv3 = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            int source = strip._SourceIndex[i];
            Vector3 stripVertex = strip._Vertices[source];
            float angle = strip._Along[i] / _Radius * Mathf.Rad2Deg;
            float radians = angle * Mathf.Deg2Rad;

            float gap = GetGapMask(angle);
            GetSpotEffect(angle, out float spotHeight, out float spotPush, out float spotLean);

            //height: the part above the floor is scaled (noise, spots, gate gaps), the part below is stretched down to Bottom Depth
            float heightMultiplier = Mathf.Max(0f, _HeightScale * (1f + _HeightNoise * SampleLoopNoise(radians, heightOffset) + spotHeight)) * gap;
            float height;
            if (stripVertex.y > 0f) height = stripVertex.y * heightMultiplier;
            else height = strip._MinY < 0f ? stripVertex.y / strip._MinY * -_BottomDepth : stripVertex.y;

            //radius: the ring, the strip's depth (its +Z faces the core), wobble, spots and lean (more lean the higher up, shaped by the curve)
            float above01 = strip._MaxY > 0f ? Mathf.Max(0f, stripVertex.y) / strip._MaxY : 0f;
            float lean = _LeanAngle + _LeanNoise * SampleLoopNoise(radians, leanOffset) + spotLean;
            float leanOut = Mathf.Tan(lean * Mathf.Deg2Rad) * strip._MaxY * heightMultiplier * _LeanCurve.Evaluate(above01);
            float radius = _Radius - stripVertex.z + _WobbleNoise * SampleLoopNoise(radians, wobbleOffset) + spotPush + leanOut;

            Vector3 radial = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)); //straight out from the core
            Vector3 tangent = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians)); //along the ring
            Vector3 stripNormal = strip._Normals[source];

            positions.Add(radial * radius + Vector3.up * height);
            normals.Add((tangent * stripNormal.x + Vector3.up * stripNormal.y - radial * stripNormal.z).normalized);
            uv0.Add(strip.GetLayoutUV0(i));
            uv2.Add(new Vector2(GetRevealMeters(angle) / maxReveal, strip.GetHeight01(source))); //x = reveal order, y = height up the strip
            uv3.Add(new Vector2(gap, 0f)); //x = gate gap mask, y = distance along a crack (0 on walls)
        }

        return ZoneStripBuilder.MakeMesh($"M_RuneField_ZoneWall_Ring{_Ring}", positions, normals, uv0, uv2, uv3, strip._LayoutTriangles);
    }

    Mesh BuildCrackMesh(ZoneStripBuilder strip, float maxReveal)
    {
        //every crack on this ring in one mesh, in the crack renderer's space (so it lifts with the zone it sits on)
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv0 = new List<Vector2>();
        List<Vector2> uv2 = new List<Vector2>();
        List<Vector2> uv3 = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < _Cracks.Count; i++)
        {
            if (_Cracks[i] != null) _Cracks[i].AppendMesh(strip, _CrackRenderer.transform, maxReveal, positions, normals, uv0, uv2, uv3, triangles);
        }
        return ZoneStripBuilder.MakeMesh($"M_RuneField_ZoneCracks_Ring{_Ring}", positions, normals, uv0, uv2, uv3, triangles);
    }

    float SampleLoopNoise(float radians, Vector2 offset)
    {
        //noise from -1 to 1 that loops around the ring: it's read around a circle in the noise, so the end meets the start with no seam
        return Mathf.PerlinNoise(offset.x + Mathf.Cos(radians) * _NoiseBumps, offset.y + Mathf.Sin(radians) * _NoiseBumps) * 2f - 1f;
    }

    void GetSpotEffect(float angle, out float height, out float push, out float lean)
    {
        //adds up every hand spot reaching this angle, each faded by Spot Falloff toward its edge
        height = 0f;
        push = 0f;
        lean = 0f;
        for (int i = 0; i < _Spots.Count; i++)
        {
            WallSpotEntry spot = _Spots[i];
            if (spot == null || spot._Width <= 0f) continue;
            float distance = GetArcDistance(angle, spot._Angle, _Radius);
            if (distance >= spot._Width) continue;

            float weight = _SpotFalloff.Evaluate(1f - distance / spot._Width);
            height += spot._HeightBoost * weight;
            push += spot._Push * weight;
            lean += spot._Lean * weight;
        }
    }

#if UNITY_EDITOR
    void MakeRenderers(RuneFieldZonesObject zones, RuneFieldZonesSO lookSettings)
    {
        //makes the wall renderer (a child) and the crack renderer (under the zone outside this ring) if they're missing, and gives both the border material
        if (_WallRenderer == null) _WallRenderer = MakeRendererObject(WallName, transform);

        if (_CrackRenderer == null)
        {
            Zone3DObject outerZone = zones.GetZone(_Ring + 1);
            if (outerZone == null) Debug.LogWarning($"Warning! No Zone {_Ring + 1} found for {gameObject.name}'s cracks to sit on. Press Set Up Zones first. Skipping the cracks...", this);
            else
            {
                _CrackRenderer = MakeRendererObject($"Ring {_Ring} Cracks", outerZone.transform);
                _CrackRenderer.gameObject.AddComponent<ZoneTintSkipObject>(); //the glow isn't tinted by the zone light
            }
        }

        SetMaterial(_WallRenderer, GetTearMaterial(lookSettings)); //the look swaps it while showing, this is just what shows in the scene by default
        SetMaterial(_CrackRenderer, GetTearMaterial(lookSettings));
    }

    MeshRenderer MakeRendererObject(string objectName, Transform parent)
    {
        //an empty child with a mesh filter and renderer, no offset, turn or scale. Shadows off: it's light, not rock
        GameObject made = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        made.transform.SetParent(parent, false);
        MeshRenderer madeRenderer = made.GetComponent<MeshRenderer>();
        madeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        UnityEditor.Undo.RegisterCreatedObjectUndo(made, "Rebuild Ring");
        return madeRenderer;
    }

    void SetMaterial(MeshRenderer target, Material material)
    {
        if (target == null || target.sharedMaterial == material) return;
        UnityEditor.Undo.RecordObject(target, "Rebuild Ring");
        target.sharedMaterial = material;
    }

    void SetMesh(MeshRenderer target, Mesh mesh)
    {
        //puts a mesh on a renderer's mesh filter
        if (target == null) return;
        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter == null) filter = UnityEditor.Undo.AddComponent<MeshFilter>(target.gameObject);
        UnityEditor.Undo.RecordObject(filter, "Rebuild Ring");
        filter.sharedMesh = mesh;
    }
#endif

    Material GetDefaultMaterial()
    {
        //the render pipeline's default material, for stand-ins
        UnityEngine.Rendering.RenderPipelineAsset pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        return pipeline != null ? pipeline.defaultMaterial : null;
    }
    #endregion

    #region Cracks
    public void FillCrackList()
    {
        //Rebuild Ring and Add Crack call this (it used to be its own button too). Finds every crack that starts on this ring (or branches off one that does) and drops empty entries. Searching is fine here, it's an editor tool
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Fill Crack List");
#endif
        _Cracks.Clear();
        ZoneCrackPath[] found = FindObjectsOfType<ZoneCrackPath>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i].GetBorder() == this) _Cracks.Add(found[i]);
        }
    }

    [FoldoutGroup("Ring Tools", true, 10), Button("Add Crack")]
    void AddCrack(float angle = 0f)
    {
        //editor button: makes a new crack leaving this ring at an angle (degrees), heading straight out, under the zone outside the ring. Then drag its points in the scene
#if UNITY_EDITOR
        RuneFieldZonesObject zones = GetComponentInParent<RuneFieldZonesObject>();
        Zone3DObject outerZone = zones != null ? zones.GetZone(_Ring + 1) : null;
        if (outerZone == null) { Debug.LogWarning($"Warning! No Zone {_Ring + 1} found to put {gameObject.name}'s crack on. Press Set Up Zones first...", this); return; }

        float radius = _Radius > 0f ? _Radius : 1f;
        GameObject crackObject = new GameObject($"Ring {_Ring} Crack");
        crackObject.transform.SetParent(outerZone.transform, false);
        UnityEditor.Undo.RegisterCreatedObjectUndo(crackObject, "Add Crack");

        ZoneCrackPath crack = crackObject.AddComponent<ZoneCrackPath>();
        Vector3 start = transform.TransformPoint(GetRingPoint(angle, radius));
        Vector3 outward = transform.TransformDirection(GetRingPoint(angle, 1f));
        crack.Setup(this, start, outward);
        UnityEditor.Selection.activeGameObject = crackObject; //selects it, so its handles show straight away
        FillCrackList();
#endif
    }
    #endregion

    #region Hand Spots
    public int GetSpotCount() { return _Spots.Count; }

    public Vector3 GetSpotWorldPosition(int index)
    {
        //where a hand spot's scene handle sits: on the ring at the spot's angle
        if (index < 0 || index >= _Spots.Count || _Spots[index] == null) return transform.position;
        return transform.TransformPoint(GetRingPoint(_Spots[index]._Angle, GetHandleRadius()));
    }

    public void SetSpotAngleFromWorld(int index, Vector3 worldPosition)
    {
        //moves a hand spot to the angle a dragged handle was let go at
        if (index < 0 || index >= _Spots.Count || _Spots[index] == null) return;
        _Spots[index]._Angle = GetAngle(worldPosition);
    }

    public float GetHandleRadius()
    {
        //the ring's radius for the scene handles: the last build's, or worked out from the settings if it's never been built
        if (_Radius > 0f) return _Radius;
        RuneFieldZonesObject zones = GetComponentInParent<RuneFieldZonesObject>();
        RuneFieldSettingsSO fieldSettings = zones != null ? zones.GetFieldSettings() : null;
        return fieldSettings != null ? fieldSettings.ToMeters(fieldSettings.GetRingRadius(_Ring)) + _RadiusOffset : 1f;
    }

    public float GetSpotWidth(int index)
    {
        return index >= 0 && index < _Spots.Count && _Spots[index] != null ? _Spots[index]._Width : 0f;
    }
    #endregion

    #region Setup
    public void SetRing(int ring)
    {
        //used by the zones component when it makes borders
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Set Ring");
#endif
        _Ring = Mathf.Clamp(ring, 1, Zone3DObject.MaxZone);
    }
    #endregion

    #region Debugging
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        //selected: the ring (white), each gate gap (red) and its slopes (orange), and each hand spot's reach (cyan)
        float radius = GetHandleRadius();
        Vector3 normal = transform.up;
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.DrawWireDisc(transform.position, normal, radius);

        float fullGap = _GateHalfWidth + _GapMargin;
        for (int i = 0; i < _GateAngles.Count; i++)
        {
            DrawArc(_GateAngles[i], fullGap, radius, Color.red);
            DrawArc(_GateAngles[i], fullGap + _GapRampWidth, radius * 1.02f, new Color(1f, 0.6f, 0f, 1f));
        }
        for (int i = 0; i < _Spots.Count; i++)
        {
            if (_Spots[i] != null) DrawArc(_Spots[i]._Angle, _Spots[i]._Width, radius * 0.98f, Color.cyan);
        }
    }

    void DrawArc(float centerAngle, float halfWidthMeters, float radius, Color color)
    {
        //an arc on the ring covering halfWidthMeters each side of an angle
        float halfAngle = halfWidthMeters / radius * Mathf.Rad2Deg;
        Vector3 from = transform.TransformDirection(GetRingPoint(centerAngle - halfAngle, 1f));
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawWireArc(transform.position, -transform.up, from, halfAngle * 2f, radius); //-up: angles in this script go from right toward forward
    }
#endif
    #endregion
}

[System.Serializable]
public class WallSpotEntry
{
    [Tooltip("Where the spot is around the ring, in degrees (0 = the field's right, 90 = its forward). Drag its handle in the scene view")]
    [Range(0f, 360f)] public float _Angle = 0f;

    [Tooltip("How far the spot reaches each side, in meters along the ring")]
    [Min(0.01f)] public float _Width = 2f;

    [Tooltip("Makes the wall taller (or shorter, if negative) here. 0.5 = 50% taller at the spot's center")]
    [Range(-1f, 3f)] public float _HeightBoost = 0.5f;

    [Tooltip("Pushes the wall out (or in, if negative) here, in meters")]
    public float _Push = 0f;

    [Tooltip("Extra lean here, in degrees")]
    [Range(-60f, 60f)] public float _Lean = 0f;
}

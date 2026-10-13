using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Sirenix.OdinInspector;

//the 3D rune field's zone look (see Zone Visuals Plan). Lives on the field object, next to RuneField3DObject
//everything comes from one number, the open zone: zones below it are frozen (raised, blue-ish), it's open (raised, neutral), zones past it are locked (lowered, muted)
//every time a slot's field loads it compares the open zone to the one the player last saw on that slot, shows it, and saves the new one
//when the shade evolved since the player last looked, the zone opening plays: one step per new zone (it rises, the tints change, the old tear turns to smoke, the new border lights up)
//it also holds the setup tools: making the zones and stand-in floors, the borders, the zone lights, the light layer names, and previewing any open zone in edit mode
[RequireComponent(typeof(RuneField3DObject))]
public class RuneFieldZonesObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The zone look asset (SO_RuneField_Zones): lift height and the zone light colors. Changes made while playing stay after stopping")]
    [SerializeField] RuneFieldZonesSO _Settings;

    [Header("Settings")]
    [Tooltip("Editor play mode only: shows the zones asset's values every frame, so changes to the light colors and lift height show up right away while tuning")]
    [SerializeField] bool _LiveTuning = true;

    [Tooltip("How far the stand-in floors' rock face goes down from their outer edge, in meters. Keep it bigger than the lift height so no gap shows")]
    [SerializeField, Min(0f)] float _StandInSkirtDepth = 0.5f;

    [Tooltip("How many steps the stand-in floors use around the circle. More = rounder")]
    [SerializeField, Range(12, 256)] int _StandInSegments = 128;

    [Header("References")]
    [Tooltip("Every zone (Zone3DObject). Order doesn't matter, each zone has its own number. Set Up Zones makes them and fills this in")]
    [InfoBox("$GetZoneProblems", InfoMessageType.Warning, "HasZoneProblems")] //"$" shows the text the function returns
    [SerializeField] List<Zone3DObject> _Zones = new List<Zone3DObject>();

    [Tooltip("Every ring border (ZoneBorder3DObject): the walls of light and their cracks. Set Up Borders makes them and fills this in")]
    [SerializeField] List<ZoneBorder3DObject> _Borders = new List<ZoneBorder3DObject>();

    [Tooltip("The scene's main directional light (the one with shadows). The zone lights copy it and point the same way. Left empty, Set Up Zone Lighting finds it")]
    [SerializeField] Light _KeyLight;

    [Tooltip("Material for the stand-in floors. Empty = HDRP's default material")]
    [SerializeField] Material _StandInMaterial;

    [Header("Runtime Data")]
    [Tooltip("The open zone being shown (read only)")]
    [SerializeField, ReadOnly] int _ShownOpenZone;

    [Tooltip("True while the zone opening plays (read only)")]
    [SerializeField, ReadOnly] bool _PlayingOpening;

    [FoldoutGroup("Tools")]
    [Tooltip("Edit mode: shows the field as if this zone were open, without pressing play. 1 = a Bound shade, the last zone = every zone open")]
    [SerializeField, Range(1, Zone3DObject.MaxZone), OnValueChanged("PreviewOpenZone")] int _PreviewOpenZone = 1;

    //local variables
    RuneField3DObject _field;
    Coroutine _opening; //the zone opening playing right now, or null
    int _openingSlot = -1; //which slot it's playing for
    int _openingTarget; //the open zone it ends on
    const string ZoneHolderName = "Zone Floors";
    const string BorderHolderName = "Zone Borders";
    const string LightHolderName = "Zone Lights";

    private void Reset()
    {
        //runs when the component is first added: finds the zones asset and any zones already under the field, so there's nothing to drag in
#if UNITY_EDITOR
        _Settings = RuneFieldZonesSO.FindInProject();
        FillZoneList();
#endif
    }

    private void Start()
    {
        //checks references, puts every zone's renderers on its light layer, then listens for the field loading a slot
        //the field may have loaded its first slot already (its Start can run before this one), so that's caught here too
        _field = GetComponent<RuneField3DObject>();
        if (_field == null) _field = RuneField3DObject._Field3D;
        if (_field == null) { Debug.LogError($"Error! No 3D rune field found for {gameObject.name}, the zones won't show their states", this); return; }
        if (FindOrCreateSettings() == false) return; //in the editor it finds or makes SO_RuneField_Zones
        if (HasZoneProblems()) Debug.LogWarning($"Warning! {gameObject.name}: {GetZoneProblems()} Showing the zones that are set up...", this);

        ApplyZoneLayers();
        _field.OnFieldLoaded += OnFieldLoaded;
        if (_field.GetField() != null) OnFieldLoaded();
    }

    private void Update()
    {
        //live tuning: shows the asset's current values every frame (4 zones, cheap). Editor only, never in a build
        //not while the zone opening plays, it sets the values itself
#if UNITY_EDITOR
        if (_LiveTuning && _ShownOpenZone > 0 && _opening == null) ShowOpenZone(_ShownOpenZone);
#endif
    }

    private void OnDisable()
    {
        //a disabled object's coroutines stop by themselves. This clears the opening's bookkeeping too. The seen zone wasn't saved, so it plays again next time
        StopOpening();
    }

    private void OnDestroy()
    {
        //stops listening so nothing calls into a destroyed object
        if (_field != null) _field.OnFieldLoaded -= OnFieldLoaded;
    }

    #region Zone States
    void OnFieldLoaded()
    {
        //a slot's field was opened (or reloaded after a save / evolve / reset). Compares its open zone to the one the player last saw on that slot:
        //same or never seen: snap, and save it. Behind: the zone opening plays, and saves it when it finishes. Ahead (the slot was reset): snap, and save it
        RuneField rules = _field.GetField();
        if (rules == null) return;
        int openZone = rules.GetOpenZone();
        int slotIndex = _field.GetSlotIndex();

        if (_opening != null)
        {
            if (slotIndex == _openingSlot && openZone == _openingTarget) return; //the same opening is already playing (a save that evolves reloads the field twice)
            StopOpening(); //something else loaded (another slot): this opening is dropped and plays again next time
        }

        ShadeManager manager = ShadeManager._ShadeManager;
        int seenZone = manager != null ? manager.GetSeenOpenZone(slotIndex) : 0; //no manager (testing the scene on its own): counts as never seen

        if (seenZone > 0 && seenZone < openZone) { PlayZoneOpening(slotIndex, seenZone, openZone); return; }
        ShowOpenZone(openZone);
        if (manager != null && seenZone != openZone) manager.SetSeenOpenZone(slotIndex, openZone);
    }

    void PlayZoneOpening(int slotIndex, int fromZone, int toZone)
    {
        //shows the zone the player last saw, then plays one step per zone up to the real open zone. Input is never blocked while it plays
        if (_Settings == null) { ShowOpenZone(toZone); return; }
        ShowOpenZone(fromZone);
        _openingSlot = slotIndex;
        _openingTarget = toZone;
        _PlayingOpening = true;
        _opening = StartCoroutine(PlayOpeningSteps(slotIndex, fromZone, toZone));
    }

    IEnumerator PlayOpeningSteps(int slotIndex, int fromZone, int toZone)
    {
        //the steps one after another (catching up more than one zone is rare: it needs gates planned ahead), then snaps to the end look and saves the seen zone
        for (int zone = fromZone + 1; zone <= toZone; zone++)
        {
            yield return PlayOpeningStep(zone); //yielding another coroutine's IEnumerator runs it here and waits for it to finish
            if (zone < toZone && _Settings._StepGap > 0f) yield return new WaitForSecondsRealtime(_Settings._StepGap);
        }

        ShowOpenZone(toZone);
        _opening = null;
        _openingSlot = -1;
        _PlayingOpening = false;
        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.SetSeenOpenZone(slotIndex, toZone);
    }

    IEnumerator PlayOpeningStep(int zone)
    {
        //one zone opening, all on real time (works while the game is paused):
        //1. the zone rises and its light goes from locked to open, while the zone behind goes from open to frozen
        //2. the border behind goes from tear to smoke, and the one behind that from smoke to off (its cracks fade with the tear)
        //3. after Reveal Delay, the new border lights up: the sweep runs around the ring and out along its cracks (the field's edge has no border, so it skips this)
        Zone3DObject opening = GetZone(zone);
        Zone3DObject behind = GetZone(zone - 1);
        ZoneBorder3DObject newBorder = GetBorder(zone);
        ZoneBorder3DObject oldBorder = GetBorder(zone - 1);
        ZoneBorder3DObject olderBorder = GetBorder(zone - 2);
        ZoneLightEntry locked = _Settings._LockedLight;
        ZoneLightEntry open = _Settings._OpenLight;
        ZoneLightEntry frozen = _Settings._FrozenLight;

        float length = _Settings.GetStepLength();
        float time = 0f;
        while (true)
        {
            float rise = _Settings._RiseCurve.Evaluate(Mathf.Clamp01(time / _Settings._RiseTime));
            float blend = _Settings._BorderBlendCurve.Evaluate(Mathf.Clamp01(time / _Settings._BorderBlendTime));
            float reveal = _Settings._RevealCurve.Evaluate(Mathf.Clamp01((time - _Settings._RevealDelay) / _Settings._RevealTime));

            if (opening != null) opening.ApplyLook(Mathf.Lerp(_Settings._LiftHeight, 0f, rise), Color.Lerp(locked._Color, open._Color, rise), Mathf.Lerp(locked._Intensity, open._Intensity, rise));
            if (behind != null) behind.ApplyLook(0f, Color.Lerp(open._Color, frozen._Color, rise), Mathf.Lerp(open._Intensity, frozen._Intensity, rise));
            if (oldBorder != null) oldBorder.ShowLook(1f - blend, blend, 1f, _Settings);
            if (olderBorder != null) olderBorder.ShowLook(0f, 1f - blend, 1f, _Settings);
            if (newBorder != null) newBorder.ShowLook(1f, 0f, reveal, _Settings);

            if (time >= length) yield break; //yield break ends the coroutine
            yield return null; //waits one frame
            time += Time.unscaledDeltaTime;
        }
    }

    void StopOpening()
    {
        //stops the zone opening if one is playing
        if (_opening != null) StopCoroutine(_opening);
        _opening = null;
        _openingSlot = -1;
        _PlayingOpening = false;
    }

    public void ShowOpenZone(int openZone)
    {
        //snaps every zone and border to its look for this open zone. Border N (the forward one) is the tear, border N - 1 smokes, the rest are off
        if (_Settings == null) return;
        for (int i = 0; i < _Zones.Count; i++)
        {
            if (_Zones[i] == null) continue;
            _Zones[i].ShowState(GetZoneState(_Zones[i].GetZoneNumber(), openZone), _Settings);
        }
        for (int i = 0; i < _Borders.Count; i++)
        {
            if (_Borders[i] == null) continue;
            int ring = _Borders[i].GetRing();
            _Borders[i].ShowLook(ring == openZone ? 1f : 0f, ring == openZone - 1 ? 1f : 0f, 1f, _Settings);
        }
        _ShownOpenZone = openZone;
    }

    public RuneFieldType.ZoneState GetZoneState(int zone, int openZone)
    {
        //below the open zone: frozen. The open zone: open. Past it: locked
        if (zone < openZone) return RuneFieldType.ZoneState.Frozen;
        if (zone == openZone) return RuneFieldType.ZoneState.Open;
        return RuneFieldType.ZoneState.Locked;
    }

    public int GetShownOpenZone() { return _ShownOpenZone; }
    #endregion

    #region Light Layers
    [FoldoutGroup("Tools"), Button("Apply Zone Layers")]
    public void ApplyZoneLayers()
    {
        //puts every renderer under each zone on that zone's light layer (also done on start, so kit pieces never need setting by hand)
        for (int i = 0; i < _Zones.Count; i++)
        {
            if (_Zones[i] != null) _Zones[i].ApplyZoneLayer();
        }
    }
    #endregion

    #region Tools
    public RuneFieldSettingsSO GetFieldSettings()
    {
        //the shared rune field numbers (zone count and width): the Shade Manager's in play mode, otherwise (editor only) the asset found in the project
        if (ShadeManager._ShadeManager != null && ShadeManager._ShadeManager.GetRuneFieldSettings() != null) return ShadeManager._ShadeManager.GetRuneFieldSettings();
#if UNITY_EDITOR
        return RuneFieldSettingsSO.FindInProject();
#else
        return null;
#endif
    }

    void PreviewOpenZone()
    {
        //the preview slider: shows every zone as if this zone were open. In edit mode Ctrl+Z puts it back
        if (FindOrCreateSettings() == false) return;
        ShowOpenZone(_PreviewOpenZone);
    }

    public RuneFieldZonesSO GetSettings()
    {
        //the zones asset (in the editor it's found or made if Settings is empty)
        FindOrCreateSettings();
        return _Settings;
    }

    bool FindOrCreateSettings()
    {
        //makes sure Settings is filled in: the asset already in the project, or (editor only) a new SO_RuneField_Zones made next to SO_RuneField_Settings
        //so there's no asset to make by hand. False (and logs why) if there's still none
        if (_Settings != null) return true;
#if UNITY_EDITOR
        RuneFieldZonesSO settings = RuneFieldZonesSO.FindInProject();
        if (settings == null) settings = CreateSettingsAsset();
        if (settings != null)
        {
            UnityEditor.Undo.RecordObject(this, "Find Zones Settings");
            _Settings = settings;
            return true;
        }
#endif
        Debug.LogWarning($"Warning! No zones settings asset on {gameObject.name}. Make one (Create > Rune Field > Zones) and drag it into Settings...", this);
        return false;
    }

#if UNITY_EDITOR
    RuneFieldZonesSO CreateSettingsAsset()
    {
        //editor only: makes SO_RuneField_Zones with the default values, in the same folder as SO_RuneField_Settings (or the Assets folder if that isn't found)
        string folder = "Assets";
        RuneFieldSettingsSO fieldSettings = RuneFieldSettingsSO.FindInProject();
        if (fieldSettings != null) folder = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(fieldSettings)).Replace('\\', '/'); //the folder part of the asset's path, with forward slashes like Unity uses

        RuneFieldZonesSO created = ScriptableObject.CreateInstance<RuneFieldZonesSO>(); //a new asset in memory, saved to disk on the next line
        string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/SO_RuneField_Zones.asset"); //adds a number if the name is taken
        UnityEditor.AssetDatabase.CreateAsset(created, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorGUIUtility.PingObject(created); //flashes the new asset in the Project window so it's easy to find
        return created;
    }
#endif

    [FoldoutGroup("Tools"), Button("Set Up Zones"), GUIColor(0.6f, 1f, 0.6f)]
    void SetUpZones()
    {
        //editor button: makes any missing zone (Zone 1 to the field's zone count) under a 'Zone Floors' object, gives zones with nothing to draw a stand-in floor,
        //and fills in the zone list. Safe to press again: existing zones and floors are kept
        FindOrCreateSettings();
        RuneFieldSettingsSO fieldSettings = GetFieldSettings();
        if (fieldSettings == null) { Debug.LogError($"Error! No Rune Field Settings asset found, {gameObject.name} can't size the zones", this); return; }
        if (fieldSettings._ZoneCount > Zone3DObject.MaxZone) Debug.LogWarning($"Warning! The field has {fieldSettings._ZoneCount} zones but light layers only cover {Zone3DObject.MaxZone}. Setting up the first {Zone3DObject.MaxZone}...", this);

        FillZoneList();
        int zoneCount = Mathf.Min(fieldSettings._ZoneCount, Zone3DObject.MaxZone);
        for (int zone = 1; zone <= zoneCount; zone++)
        {
            Zone3DObject found = GetZone(zone);
            if (found == null) found = MakeZone(zone);
            if (found.HasRenderers() == false) BuildStandIn(found, fieldSettings);
        }
        ApplyZoneLayers();
        PreviewOpenZone();
    }

    [FoldoutGroup("Tools"), Button("Rebuild Stand-In Floors")]
    void RebuildStandInFloors()
    {
        //editor button: remakes every zone's stand-in floor (after changing the zone width, skirt depth or material). Real floors are left alone
        RuneFieldSettingsSO fieldSettings = GetFieldSettings();
        if (fieldSettings == null) { Debug.LogError($"Error! No Rune Field Settings asset found, {gameObject.name} can't size the zones", this); return; }
        for (int i = 0; i < _Zones.Count; i++)
        {
            if (_Zones[i] != null) BuildStandIn(_Zones[i], fieldSettings);
        }
        ApplyZoneLayers();
    }

    void BuildStandIn(Zone3DObject zone, RuneFieldSettingsSO fieldSettings)
    {
        //gives one zone a stand-in floor sized from the zone width: zone N runs from ring N - 1 to ring N
        int number = zone.GetZoneNumber();
        float inner = fieldSettings.ToMeters(fieldSettings.GetRingRadius(number - 1));
        float outer = fieldSettings.ToMeters(fieldSettings.GetRingRadius(number));
        zone.BuildStandInFloor(inner, outer, _StandInSkirtDepth, _StandInSegments, GetStandInMaterial());
    }

    Material GetStandInMaterial()
    {
        //the stand-in material, or the render pipeline's default one
        if (_StandInMaterial != null) return _StandInMaterial;
        UnityEngine.Rendering.RenderPipelineAsset pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        return pipeline != null ? pipeline.defaultMaterial : null;
    }

    Zone3DObject MakeZone(int zone)
    {
        //makes an empty zone object under 'Zone Floors', at the field's center
        GameObject zoneObject = new GameObject($"Zone {zone}");
        zoneObject.transform.SetParent(GetHolder(ZoneHolderName), false); //false: no offset, turn or scale, so it sits on the field's center
        Zone3DObject made = zoneObject.AddComponent<Zone3DObject>();
        made.SetZoneNumber(zone);
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(zoneObject, "Set Up Zones");
        UnityEditor.Undo.RecordObject(this, "Set Up Zones");
#endif
        _Zones.Add(made);
        return made;
    }

    Transform GetHolder(string holderName)
    {
        //an empty object under the field with this name (made if it doesn't exist), with no offset, turn or scale
        Transform holder = transform.Find(holderName);
        if (holder != null) return holder;

        GameObject made = new GameObject(holderName);
        made.transform.SetParent(transform, false);
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(made, "Make Holder");
#endif
        return made.transform;
    }

    public Zone3DObject GetZone(int zone)
    {
        //the zone in the list with this number, or null
        for (int i = 0; i < _Zones.Count; i++)
        {
            if (_Zones[i] != null && _Zones[i].GetZoneNumber() == zone) return _Zones[i];
        }
        return null;
    }

    [FoldoutGroup("Tools"), Button("Set Up Borders"), GUIColor(0.6f, 1f, 0.6f)]
    void SetUpBorders()
    {
        //editor button: sets up the zones first (their floors carry the cracks), then makes any missing ring border (ring 1 to the last gate ring)
        //under a 'Zone Borders' object and builds every one. Safe to press again
        SetUpZones();
        RuneFieldSettingsSO fieldSettings = GetFieldSettings();
        if (fieldSettings == null) return; //SetUpZones already said why

        FillBorderList();
        int lastRing = Mathf.Min(fieldSettings.GetLastGateRing(), Zone3DObject.MaxZone - 1); //the field's edge never glows, so it gets no border
        for (int ring = 1; ring <= lastRing; ring++)
        {
            ZoneBorder3DObject border = GetBorder(ring);
            if (border == null) border = MakeBorder(ring);
            border.RebuildRing();
        }
        PreviewOpenZone();
    }

    [FoldoutGroup("Tools"), Button("Rebuild All Borders")]
    public void RebuildAllBorders()
    {
        //editor button: rebuilds every border's wall and cracks (after changing the strip, the zone width or a gate)
        for (int i = 0; i < _Borders.Count; i++)
        {
            if (_Borders[i] != null) _Borders[i].RebuildRing();
        }
        PreviewOpenZone();
    }

    ZoneBorder3DObject MakeBorder(int ring)
    {
        //makes an empty border under 'Zone Borders', at the field's center. It stays put when the zones lift
        GameObject borderObject = new GameObject($"Ring {ring} Border");
        borderObject.transform.SetParent(GetHolder(BorderHolderName), false);
        ZoneBorder3DObject made = borderObject.AddComponent<ZoneBorder3DObject>();
        made.SetRing(ring);
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(borderObject, "Set Up Borders");
        UnityEditor.Undo.RecordObject(this, "Set Up Borders");
#endif
        _Borders.Add(made);
        return made;
    }

    public ZoneBorder3DObject GetBorder(int ring)
    {
        //the border in the list for this ring, or null (the field's edge and ring 0 never have one)
        for (int i = 0; i < _Borders.Count; i++)
        {
            if (_Borders[i] != null && _Borders[i].GetRing() == ring) return _Borders[i];
        }
        return null;
    }

    [FoldoutGroup("Tools"), Button("Fill Border List")]
    void FillBorderList()
    {
        //adds every ZoneBorder3DObject under the field that isn't in the list yet, and drops empty entries
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Fill Border List");
#endif
        _Borders.RemoveAll(border => border == null); //removes every entry that's empty
        ZoneBorder3DObject[] found = GetComponentsInChildren<ZoneBorder3DObject>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (_Borders.Contains(found[i]) == false) _Borders.Add(found[i]);
        }
    }

    [FoldoutGroup("Tools"), Button("Fill Zone List")]
    void FillZoneList()
    {
        //adds every Zone3DObject under the field that isn't in the list yet, and drops empty entries
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Fill Zone List");
#endif
        _Zones.RemoveAll(zone => zone == null); //"zone => zone == null" is a small function: remove every entry that's empty
        Zone3DObject[] found = GetComponentsInChildren<Zone3DObject>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (_Zones.Contains(found[i]) == false) _Zones.Add(found[i]);
        }
    }

    [FoldoutGroup("Tools"), Button("Set Up Zone Lighting"), GUIColor(0.6f, 1f, 0.6f)]
    void SetUpZoneLighting()
    {
        //editor button: gives every zone a shadowless light that only lights its own layer, under a 'Zone Lights' object
        //each one starts as a copy of the key light (so HDRP's settings and unit carry over), then shadows, sky and fog are turned off on it
        //pressing it again keeps the lights and turns them to match the key light (do that after moving the key light)
        if (FindKeyLight() == false) return;
        Transform holder = GetHolder(LightHolderName);

        for (int i = 0; i < _Zones.Count; i++)
        {
            Zone3DObject zone = _Zones[i];
            if (zone == null) continue;

            Light zoneLight = zone.GetZoneLight();
            if (zoneLight == null)
            {
                zoneLight = MakeZoneLight(zone, holder);
                zone.SetZoneLight(zoneLight);
            }
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(zoneLight.transform, "Set Up Zone Lighting");
#endif
            zoneLight.transform.rotation = _KeyLight.transform.rotation;
        }
        PreviewOpenZone(); //sets every light's color and brightness from the asset
    }

    bool FindKeyLight()
    {
        //uses Key Light, or finds the scene's directional light with shadows (a one-time search, editor tool only). False (and logs why) if there isn't one
        if (_KeyLight != null) return true;
        Light[] lights = FindObjectsOfType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type != LightType.Directional || lights[i].shadows == LightShadows.None) continue;
            if (lights[i].transform.IsChildOf(transform)) continue; //not one of the zone lights
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Find Key Light");
#endif
            _KeyLight = lights[i];
            return true;
        }
        Debug.LogError($"Error! No key light on {gameObject.name} and no directional light with shadows in the scene. Add one (or drag it into Key Light), then press Set Up Zone Lighting again", this);
        return false;
    }

    Light MakeZoneLight(Zone3DObject zone, Transform holder)
    {
        //copies the key light, then makes the copy shadowless and only on this zone's light layer
        GameObject copy = Instantiate(_KeyLight.gameObject, holder);
        copy.name = $"Zone {zone.GetZoneNumber()} Light";
        for (int i = copy.transform.childCount - 1; i >= 0; i--) DestroyImmediate(copy.transform.GetChild(i).gameObject); //the copy doesn't need the key light's children. Backwards, so removing doesn't skip any

        Light zoneLight = copy.GetComponent<Light>();
        zoneLight.shadows = LightShadows.None; //shadows are the expensive part, only the key light casts them
        zoneLight.useColorTemperature = false; //the color comes straight from the zones asset, not filtered by the key light's temperature
        if (copy.TryGetComponent(out HDAdditionalLightData lightData))
        {
            lightData.lightlayersMask = (LightLayerEnum)zone.GetLightLayerMask(); //only lights renderers on this zone's layer
            lightData.interactsWithSky = false; //no second sun in the sky
            lightData.affectsVolumetric = false; //doesn't light the fog, so the tint stays on the floor
        }
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(copy, "Set Up Zone Lighting");
#endif
        return zoneLight;
    }

    [FoldoutGroup("Tools"), Button("Name Light Layers")]
    void NameLightLayers()
    {
        //editor button: names HDRP's Light Layers 1 to the zone count 'Zone 1', 'Zone 2'... (Project Settings > Graphics > HDRP Global Settings)
        //only layers still on their default name are renamed, so a name set by hand is never overwritten
#if UNITY_EDITOR
        //HDRP 14 keeps its global settings class internal (scripts outside HDRP can't use it), so this edits the settings asset the way the inspector does:
        //a SerializedObject reads and writes an asset's saved fields by name, whatever their protection level
        RenderPipelineGlobalSettings global = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
        if (global == null) { Debug.LogError($"Error! No HDRP Global Settings found, {gameObject.name} can't name the light layers. Rename Light Layers 1 to 4 by hand (Project Settings > Graphics > HDRP Global Settings)", this); return; }
        RuneFieldSettingsSO fieldSettings = GetFieldSettings();
        int zoneCount = fieldSettings != null ? fieldSettings._ZoneCount : 4;

        UnityEditor.SerializedObject settingsData = new UnityEditor.SerializedObject(global);
        for (int layer = 1; layer <= Zone3DObject.MaxZone; layer++)
        {
            UnityEditor.SerializedProperty layerName = settingsData.FindProperty($"lightLayerName{layer}"); //the field's saved name in the asset
            if (layerName == null) { Debug.LogWarning($"Warning! Couldn't find Light Layer {layer}'s name in the HDRP Global Settings, rename it by hand...", this); continue; }
            layerName.stringValue = PickLayerName(layerName.stringValue, layer, zoneCount);
        }
        settingsData.ApplyModifiedProperties(); //writes the changes back (with undo) and marks the asset changed
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    string PickLayerName(string current, int layer, int zoneCount)
    {
        //the name a light layer should get: 'Zone N' if it's a zone layer still on its default name, otherwise what it already has
        if (layer > zoneCount) return current;
        string wanted = $"Zone {layer}";
        if (current == wanted) return current;
        if (current == $"Light Layer {layer}") return wanted;
        Debug.LogWarning($"Warning! Light Layer {layer} is already named '{current}', leaving it. Zone {layer} uses that layer...", this);
        return current;
    }

    bool HasZoneProblems()
    {
        return GetZoneProblems() != "";
    }

    string GetZoneProblems()
    {
        //checks the zone list: empty entries, or two zones with the same number. Shown as a warning in the inspector
        if (_Zones.Count == 0) return "No zones yet. Press Set Up Zones (Tools).";
        List<int> seen = new List<int>();
        for (int i = 0; i < _Zones.Count; i++)
        {
            if (_Zones[i] == null) return "The zone list has an empty entry. Press Fill Zone List (Tools).";
            int number = _Zones[i].GetZoneNumber();
            if (seen.Contains(number)) return $"Two zones are set to zone {number}. Each zone needs its own number.";
            seen.Add(number);
        }
        return "";
    }
    #endregion

    #region Test Tools
    [FoldoutGroup("Test Tools"), Button("Replay From Seen Zone (play mode)")]
    void TestReplayFromSeenZone(int seenZone = 1)
    {
        //play mode: pretends the player last saw this zone open on the open slot, then reloads the field, as if the shade evolved while the field was closed
        //so the zone opening plays from that zone up to the real open zone
        if (Application.isPlaying == false || _field == null || ShadeManager._ShadeManager == null) { Debug.LogWarning("Warning! Press play first..."); return; }
        int slotIndex = _field.GetSlotIndex();
        ShadeManager._ShadeManager.SetSeenOpenZone(slotIndex, seenZone);
        _field.LoadRuneField(slotIndex); //throws away unsaved changes on the draft, like any reload
    }
    #endregion
}

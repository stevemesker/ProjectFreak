using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Sirenix.OdinInspector;

//one zone of the 3D rune field: zone 1 is the disc around the core, the rest are rings outward
//everything under this object is that zone's floor (Steve's floor mesh, kit rocks, later its cracks and decals): it all lifts and lowers together
//its renderers go on the zone's own HDRP light layer, and its shadowless zone light (on that layer only) gives it the open / frozen / locked tint
//RuneFieldZonesObject (on the field) tells each zone its state. See Zone Visuals Plan
[DisallowMultipleComponent]
public class Zone3DObject : MonoBehaviour
{
    public const int MaxZone = 7; //HDRP has 8 light layers (0 to 7) and layer 0 is the default one, so 7 zones at most

    [Header("Data")]
    [Tooltip("Which zone this is: 1 is the disc around the core, then rings outward. It's also the zone's light layer (Zone 1 = Light Layer 1)")]
    [SerializeField, Range(1, MaxZone)] int _ZoneNumber = 1;

    [Header("References")]
    [Tooltip("This zone's shadowless light. It only lights this zone's light layer. Set Up Zone Lighting (on the field's zones component) makes and fills these")]
    [SerializeField] Light _ZoneLight;

    [Header("Runtime Data")]
    [Tooltip("The state this zone is showing (read only)")]
    [SerializeField, ReadOnly] RuneFieldType.ZoneState _State = RuneFieldType.ZoneState.Open;

    //local variables
    const uint LightLayerBits = 0xFF; //a renderer's rendering layer mask: the first 8 bits are its light layers, the rest are decal layers (left alone)
    const uint DefaultLightLayer = 1; //bit 0: the default light layer, which the key light and everything else lights
    const string StandInName = "Stand-In Floor";

    #region Zone State
    public void ShowState(RuneFieldType.ZoneState state, RuneFieldZonesSO settings)
    {
        //snaps the zone to a state's look: its height and its zone light
        if (settings == null) return;
        ZoneLightEntry look = settings.GetLight(state);
        _State = state;
        ApplyLook(settings.GetDrop(state), look._Color, look._Intensity);
    }

    public void ApplyLook(float drop, Color lightColor, float lightIntensity)
    {
        //sets the zone's height (drop = meters below the raised level) and its light. The transitions (build step 5) will blend these values over time
        //only the height along the parent's up changes, so the zone keeps any sideways placement
        RecordForUndo(transform, "Zone Look");
        Vector3 local = transform.localPosition;
        transform.localPosition = new Vector3(local.x, -drop, local.z);

        if (_ZoneLight == null) return;
        RecordForUndo(_ZoneLight, "Zone Look");
        _ZoneLight.color = lightColor;

        //HDRP keeps the brightness on its own light data (in the light's unit). Without it (not HDRP) the plain Light's intensity is used
        if (_ZoneLight.TryGetComponent(out HDAdditionalLightData lightData))
        {
            RecordForUndo(lightData, "Zone Look");
            lightData.intensity = lightIntensity;
        }
        else _ZoneLight.intensity = lightIntensity;
    }
    #endregion

    #region Light Layers
    public uint GetLightLayerMask()
    {
        //this zone's light layer as a bit (Zone 1 = bit 1, Zone 2 = bit 2...). "1u << n" moves the 1 left n bits
        return 1u << _ZoneNumber;
    }

    public int ApplyZoneLayer()
    {
        //puts every renderer under this zone on the default light layer plus this zone's layer, so the key light still lights it and only this zone's light tints it
        //renderers under a ZoneTintSkipObject get the default layer only. Decal layers (the higher bits) are kept as they are
        //returns how many renderers changed
        int changed = 0;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            uint wanted = DefaultLightLayer;
            if (IsSkipped(renderers[i].transform) == false) wanted |= GetLightLayerMask(); //"|=" adds the zone's bit to the mask
            uint mask = (renderers[i].renderingLayerMask & ~LightLayerBits) | wanted; //"& ~" clears the light layer bits, then the wanted ones go in
            if (renderers[i].renderingLayerMask == mask) continue;

            RecordForUndo(renderers[i], "Apply Zone Layers");
            renderers[i].renderingLayerMask = mask;
            changed++;
        }
        return changed;
    }

    bool IsSkipped(Transform target)
    {
        //true if the target, or anything between it and this zone (the zone included), has the skip marker. Markers above the zone don't count
        Transform current = target;
        while (current != null)
        {
            if (current.TryGetComponent(out ZoneTintSkipObject marker)) return true;
            if (current == transform) return false; //reached the zone, stop looking
            current = current.parent;
        }
        return false;
    }
    #endregion

    #region Stand-In Floor
    public void BuildStandInFloor(float innerRadius, float outerRadius, float skirtDepth, int segments, Material material)
    {
        //makes (or remakes) a plain floor child until Steve's floor mesh is in: a disc (inner radius 0) or a ring with its top at this zone's height
        //plus a rock face going down from the outer edge, which shows when the next zone out is lowered. Sizes in meters
        //the mesh is made by code and saved inside the scene. Delete the Stand-In Floor child once the real floor is under the zone
        if (outerRadius <= innerRadius) { Debug.LogWarning($"Warning! {gameObject.name}'s stand-in floor has no width ({innerRadius} to {outerRadius} m), not building it...", this); return; }

        Transform old = transform.Find(StandInName);
        if (old != null) DestroyForTools(old.gameObject);

        GameObject floor = new GameObject(StandInName, typeof(MeshFilter), typeof(MeshRenderer));
        floor.transform.SetParent(transform, false); //false keeps it at the zone's center with no offset
        floor.GetComponent<MeshFilter>().sharedMesh = BuildFloorMesh(innerRadius, outerRadius, skirtDepth, Mathf.Max(3, segments));
        if (material != null) floor.GetComponent<MeshRenderer>().sharedMaterial = material;
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(floor, "Build Stand-In Floor"); //Ctrl+Z removes it again
#endif
    }

    Mesh BuildFloorMesh(float innerRadius, float outerRadius, float skirtDepth, int segments)
    {
        //the stand-in floor's mesh: a flat top (disc or ring) and an outer skirt. Each part gets its own vertices so the edge between them stays sharp
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        bool disc = innerRadius <= 0f;

        //top: around the circle, an inner and an outer vertex per step (the disc uses one center vertex instead)
        //triangles are listed clockwise as seen from above, which is the side Unity draws
        int center = 0;
        if (disc) { vertices.Add(Vector3.zero); normals.Add(Vector3.up); uvs.Add(new Vector2(0.5f, 0.5f)); }
        int topStart = vertices.Count;
        for (int i = 0; i <= segments; i++) //"<=": the last step repeats the first spot, so the texture wraps cleanly
        {
            Vector3 direction = GetCircleDirection(i, segments);
            if (disc == false) AddVertex(vertices, normals, uvs, direction * innerRadius, Vector3.up, PlanarUV(direction * innerRadius, outerRadius));
            AddVertex(vertices, normals, uvs, direction * outerRadius, Vector3.up, PlanarUV(direction * outerRadius, outerRadius));
        }
        for (int i = 0; i < segments; i++)
        {
            if (disc)
            {
                int outer0 = topStart + i;
                triangles.Add(center); triangles.Add(outer0 + 1); triangles.Add(outer0);
                continue;
            }
            int inner0 = topStart + i * 2;
            int outerA = inner0 + 1;
            int inner1 = inner0 + 2;
            int outerB = inner0 + 3;
            triangles.Add(inner0); triangles.Add(inner1); triangles.Add(outerB);
            triangles.Add(inner0); triangles.Add(outerB); triangles.Add(outerA);
        }

        //outer skirt: a top and a bottom vertex per step, facing outward
        int skirtStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            Vector3 direction = GetCircleDirection(i, segments);
            float u = (float)i / segments;
            AddVertex(vertices, normals, uvs, direction * outerRadius, direction, new Vector2(u, 1f));
            AddVertex(vertices, normals, uvs, direction * outerRadius + Vector3.down * skirtDepth, direction, new Vector2(u, 0f));
        }
        for (int i = 0; i < segments; i++)
        {
            int top0 = skirtStart + i * 2;
            int bottom0 = top0 + 1;
            int top1 = top0 + 2;
            int bottom1 = top0 + 3;
            triangles.Add(top0); triangles.Add(top1); triangles.Add(bottom1);
            triangles.Add(top0); triangles.Add(bottom1); triangles.Add(bottom0);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Stand-In Floor (Zone {_ZoneNumber})";
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    Vector3 GetCircleDirection(int step, int segments)
    {
        //the flat direction for one step around the circle (step 0 points along +X)
        float angle = (float)step / segments * Mathf.PI * 2f; //Mathf.Cos / Sin work in radians, a full circle is 2 x PI
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    Vector2 PlanarUV(Vector3 position, float outerRadius)
    {
        //a top-down UV: the whole floor fits 0 to 1
        return new Vector2(position.x / (outerRadius * 2f) + 0.5f, position.z / (outerRadius * 2f) + 0.5f);
    }

    void AddVertex(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, Vector3 position, Vector3 normal, Vector2 uv)
    {
        vertices.Add(position);
        normals.Add(normal);
        uvs.Add(uv);
    }

    public bool HasRenderers()
    {
        //true if anything under this zone draws (a stand-in or a real floor), so setup doesn't build a stand-in over a real floor
        return GetComponentInChildren<Renderer>(true) != null;
    }
    #endregion

    #region Getters And Setters
    public int GetZoneNumber() { return _ZoneNumber; }
    public Light GetZoneLight() { return _ZoneLight; }
    public RuneFieldType.ZoneState GetState() { return _State; }

    public void SetZoneNumber(int zoneNumber)
    {
        //used by the zones component when it makes zones
        RecordForUndo(this, "Set Zone Number");
        _ZoneNumber = Mathf.Clamp(zoneNumber, 1, MaxZone);
    }

    public void SetZoneLight(Light zoneLight)
    {
        //used by Set Up Zone Lighting
        RecordForUndo(this, "Set Zone Light");
        _ZoneLight = zoneLight;
    }
    #endregion

    #region Tools
    void RecordForUndo(Object target, string label)
    {
        //outside play mode, lets Ctrl+Z undo a tool's change and marks the scene as changed so it gets saved. Does nothing while playing
#if UNITY_EDITOR
        if (Application.isPlaying == false && target != null) UnityEditor.Undo.RecordObject(target, label);
#endif
    }

    void DestroyForTools(GameObject target)
    {
        //removes an object in edit mode (undoable) or play mode
#if UNITY_EDITOR
        if (Application.isPlaying == false) { UnityEditor.Undo.DestroyObjectImmediate(target); return; }
#endif
        Destroy(target);
    }

    [FoldoutGroup("Tools"), Button("Apply Zone Layer")]
    void ApplyZoneLayerButton()
    {
        //editor button: puts this zone's renderers on its light layer now (the field's zones component does every zone at once)
        ApplyZoneLayer();
    }
    #endregion
}

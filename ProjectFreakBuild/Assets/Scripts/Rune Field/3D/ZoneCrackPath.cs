using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//one glowing floor crack on the 3D rune field: a smaller version of the ring wall, built along a path of points instead of a circle (see Zone Visuals Plan)
//it starts on a ring (or on another crack, as a branch), is tall where it leaves and collapses to nothing at its tip
//it belongs to the ring it starts from (a branch to its parent's ring), so it follows that ring's tear / off look. Its ring's Rebuild Ring bakes all of them into one mesh
//keep crack objects under the zone outside their ring (Add Crack on the border puts them there), so they lift with that floor
[DisallowMultipleComponent]
public class ZoneCrackPath : MonoBehaviour
{
    const int MaxBranchDepth = 32; //guards against a crack set as its own parent (directly or through others), which would loop forever

    [Header("Data")]
    [Tooltip("The ring this crack leaves from. Ignored for a branch (it uses its parent's ring)")]
    [SerializeField] ZoneBorder3DObject _StartBorder;

    [Tooltip("Optional: makes this a branch. It starts on this crack (at Branch At) instead of on the ring")]
    [SerializeField] ZoneCrackPath _ParentCrack;

    [Tooltip("Where along the parent crack the branch starts (0 = the parent's start, 1 = its tip)")]
    [SerializeField, Range(0f, 1f), ShowIf("@_ParentCrack != null")] float _BranchAt = 0.5f;

    [Tooltip("The crack's points, from its start to its tip, relative to this object. Drag them in the scene view while this object is selected. The first one snaps onto the ring (or the parent crack)")]
    [SerializeField] List<Vector3> _Points = new List<Vector3>();

    [Header("Settings")]
    [Tooltip("The crack's height compared to the strip (0.5 = half the strip's height)")]
    [SerializeField, Min(0f)] float _HeightScale = 0.5f;

    [Tooltip("The crack's thickness compared to the strip")]
    [SerializeField, Min(0f)] float _WidthScale = 1f;

    [Tooltip("Height along the crack. X = 0 at its start to 1 at its tip, Y = how much of its height. Full at the start, falling to 0 at the tip")]
    [SerializeField] AnimationCurve _HeightCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [FoldoutGroup("Noise"), Tooltip("How much the height wobbles along the crack (0.2 = up to 20%)")]
    [SerializeField, Range(0f, 1f)] float _HeightNoise = 0.2f;

    [FoldoutGroup("Noise"), Tooltip("How far it wobbles side to side, in meters")]
    [SerializeField, Min(0f)] float _WobbleNoise = 0.03f;

    [FoldoutGroup("Noise"), Tooltip("How bumpy the noise is, in bumps per meter")]
    [SerializeField, Range(0.05f, 10f)] float _NoiseBumps = 1.5f;

    [FoldoutGroup("Noise"), Tooltip("Changes the noise pattern, so no two cracks look copied")]
    [SerializeField] int _NoiseSeed = 0;

    #region Path
    public ZoneBorder3DObject GetBorder()
    {
        //the ring this crack belongs to: its own, or (for a branch) the first one found going up the parents. Null if none, or if the parents loop
        ZoneCrackPath current = this;
        for (int depth = 0; depth < MaxBranchDepth; depth++)
        {
            if (current._ParentCrack == null) return current._StartBorder;
            current = current._ParentCrack;
        }
        Debug.LogWarning($"Warning! {gameObject.name}'s parent cracks loop back on themselves, it isn't on any ring...", this);
        return null;
    }

    public void SnapStart()
    {
        //moves the first point onto the ring (keeping its angle around the core) or onto the parent crack at Branch At. The point keeps its own height
        if (_Points.Count == 0) return;
        Vector3 target;
        if (_ParentCrack != null && _ParentCrack != this) target = _ParentCrack.GetWorldPointAt(_BranchAt * _ParentCrack.GetLength());
        else if (_StartBorder != null)
        {
            float angle = _StartBorder.GetAngle(transform.TransformPoint(_Points[0]));
            target = _StartBorder.transform.TransformPoint(ZoneBorder3DObject.GetRingPoint(angle, _StartBorder.GetHandleRadius()));
        }
        else return;

        Vector3 local = transform.InverseTransformPoint(target);
        local.y = _Points[0].y;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Snap Crack Start");
#endif
        _Points[0] = local;
    }

    public bool IsStartSnapped()
    {
        //true if the first point is placed for you (on a ring or a parent crack), so its handle isn't draggable
        return _ParentCrack != null || _StartBorder != null;
    }

    public float GetLength()
    {
        //the crack's length along its points, in meters
        float length = 0f;
        for (int i = 1; i < _Points.Count; i++) length += Vector3.Distance(transform.TransformPoint(_Points[i - 1]), transform.TransformPoint(_Points[i]));
        return length;
    }

    public Vector3 GetWorldPointAt(float distance)
    {
        //the spot this far along the crack (meters from its start), in the world. Past the ends it stays at the end
        if (_Points.Count == 0) return transform.position;
        float left = distance;
        for (int i = 1; i < _Points.Count; i++)
        {
            Vector3 from = transform.TransformPoint(_Points[i - 1]);
            Vector3 to = transform.TransformPoint(_Points[i]);
            float piece = Vector3.Distance(from, to);
            if (left <= piece && piece > 0f) return Vector3.Lerp(from, to, left / piece);
            left -= piece;
        }
        return transform.TransformPoint(_Points[_Points.Count - 1]);
    }

    public float GetStartRevealMeters()
    {
        //how far the light-up sweep has travelled when it reaches this crack's start, in meters: around the ring to where it leaves,
        //or (a branch) to its parent's start then along the parent to the branch point. So cracks light up as the sweep passes them
        ZoneCrackPath current = this;
        float meters = 0f;
        for (int depth = 0; depth < MaxBranchDepth; depth++)
        {
            if (current._ParentCrack == null)
            {
                if (current._StartBorder == null || current._Points.Count == 0) return meters;
                return meters + current._StartBorder.GetRevealMeters(current._StartBorder.GetAngle(current.transform.TransformPoint(current._Points[0])));
            }
            meters += current._BranchAt * current._ParentCrack.GetLength();
            current = current._ParentCrack;
        }
        return meters;
    }
    #endregion

    #region Building
    public void AppendMesh(ZoneStripBuilder strip, Transform space, float maxReveal, List<Vector3> positions, List<Vector3> normals, List<Vector2> uv0, List<Vector2> uv2, List<Vector2> uv3, List<int> triangles)
    {
        //adds this crack to the ring's crack mesh: copies of the strip laid along the path, height falling off along Height Curve
        //"space" is the crack renderer's transform: the mesh is built relative to it
        float length = GetLength();
        if (_Points.Count < 2 || length <= 0.001f) { Debug.LogWarning($"Warning! {gameObject.name} needs at least two points apart from each other, skipping it...", this); return; }

        int copies = Mathf.Max(1, Mathf.RoundToInt(length / strip._Length));
        strip.Layout(copies, length / (copies * strip._Length)); //stretched a little so the copies fill the path exactly

        //the path in the renderer's space, plus how far along each point is
        List<Vector3> path = new List<Vector3>();
        List<float> pathDistance = new List<float>();
        for (int i = 0; i < _Points.Count; i++)
        {
            path.Add(space.InverseTransformPoint(transform.TransformPoint(_Points[i])));
            pathDistance.Add(i == 0 ? 0f : pathDistance[i - 1] + Vector3.Distance(path[i - 1], path[i]));
        }

        System.Random random = new System.Random(_NoiseSeed);
        Vector2 heightOffset = new Vector2((float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);
        Vector2 wobbleOffset = new Vector2((float)random.NextDouble() * 1000f, (float)random.NextDouble() * 1000f);
        float startReveal = GetStartRevealMeters();
        int firstVertex = positions.Count; //this crack's vertices come after the cracks already added

        for (int i = 0; i < strip._SourceIndex.Count; i++)
        {
            int source = strip._SourceIndex[i];
            Vector3 stripVertex = strip._Vertices[source];
            float along = Mathf.Clamp(strip._Along[i], 0f, pathDistance[pathDistance.Count - 1]);
            float along01 = along / pathDistance[pathDistance.Count - 1];
            GetPathPoint(path, pathDistance, along, out Vector3 point, out Vector3 forward);

            //side = sideways across the path. Cross gives the direction at right angles to both: this order keeps the strip facing the same way as on the ring
            Vector3 side = Vector3.Cross(forward, Vector3.up).normalized;
            float noiseHeight = Mathf.PerlinNoise(heightOffset.x + along * _NoiseBumps, heightOffset.y) * 2f - 1f;
            float noiseSide = Mathf.PerlinNoise(wobbleOffset.x + along * _NoiseBumps, wobbleOffset.y) * 2f - 1f;

            //above the floor: scaled by Height Scale, the curve and noise. Below the floor: Height Scale only, so it stays in its trench
            float heightMultiplier = _HeightScale * Mathf.Max(0f, _HeightCurve.Evaluate(along01) * (1f + _HeightNoise * noiseHeight));
            float height = stripVertex.y > 0f ? stripVertex.y * heightMultiplier : stripVertex.y * _HeightScale;
            float sideways = stripVertex.z * _WidthScale + _WobbleNoise * noiseSide;

            Vector3 stripNormal = strip._Normals[source];
            positions.Add(point + side * sideways + Vector3.up * height);
            normals.Add((forward * stripNormal.x + Vector3.up * stripNormal.y + side * stripNormal.z).normalized);
            uv0.Add(strip.GetLayoutUV0(i));
            uv2.Add(new Vector2((startReveal + along) / maxReveal, strip.GetHeight01(source))); //x = reveal order (carries on from the ring), y = height up the strip
            uv3.Add(new Vector2(1f, along01)); //x = never in a gate gap, y = distance along the crack
        }

        for (int i = 0; i < strip._LayoutTriangles.Count; i++) triangles.Add(firstVertex + strip._LayoutTriangles[i]);
    }

    void GetPathPoint(List<Vector3> path, List<float> pathDistance, float distance, out Vector3 point, out Vector3 forward)
    {
        //the spot this far along the path and the flat direction it's heading there
        for (int i = 1; i < path.Count; i++)
        {
            if (distance > pathDistance[i] && i < path.Count - 1) continue;
            float piece = pathDistance[i] - pathDistance[i - 1];
            point = Vector3.Lerp(path[i - 1], path[i], piece > 0f ? (distance - pathDistance[i - 1]) / piece : 0f);
            forward = Vector3.ProjectOnPlane(path[i] - path[i - 1], Vector3.up).normalized; //flattened, so a sloped crack still stands upright
            if (forward == Vector3.zero) forward = Vector3.forward;
            return;
        }
        point = path[0];
        forward = Vector3.forward;
    }
    #endregion

    #region Editing
    public void Setup(ZoneBorder3DObject border, Vector3 worldStart, Vector3 worldOutward)
    {
        //used by the border's Add Crack: a three-point crack heading out from the ring
        _StartBorder = border;
        _Points.Clear();
        Vector3 outward = worldOutward.normalized;
        for (int i = 0; i < 3; i++) _Points.Add(transform.InverseTransformPoint(worldStart + outward * i));
        SnapStart();
    }

    public int GetPointCount() { return _Points.Count; }

    public Vector3 GetWorldPoint(int index)
    {
        return index >= 0 && index < _Points.Count ? transform.TransformPoint(_Points[index]) : transform.position;
    }

    public void SetWorldPoint(int index, Vector3 worldPosition)
    {
        //moves one point (from its scene handle), keeping its height
        if (index < 0 || index >= _Points.Count) return;
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        local.y = _Points[index].y;
        _Points[index] = local;
    }

    [Button("Add Point")]
    void AddPoint()
    {
        //adds a point past the tip, carrying on the way the crack was heading (1 m further)
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Add Crack Point");
#endif
        if (_Points.Count == 0) { _Points.Add(Vector3.zero); return; }
        Vector3 last = _Points[_Points.Count - 1];
        Vector3 heading = _Points.Count > 1 ? (last - _Points[_Points.Count - 2]).normalized : Vector3.forward;
        _Points.Add(last + heading);
    }

    [Button("Remove Last Point")]
    void RemoveLastPoint()
    {
        //takes the tip point off. A crack keeps at least two points
        if (_Points.Count <= 2) { Debug.LogWarning($"Warning! {gameObject.name} needs at least two points, not removing one...", this); return; }
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Remove Crack Point");
#endif
        _Points.RemoveAt(_Points.Count - 1);
    }

    [Button("Rebuild My Ring"), GUIColor(0.6f, 1f, 0.6f)]
    void RebuildMyRing()
    {
        //rebuilds the ring this crack belongs to (all of its cracks are one mesh)
        ZoneBorder3DObject border = GetBorder();
        if (border == null) { Debug.LogWarning($"Warning! {gameObject.name} has no Start Border (or parent crack on a ring), nothing to rebuild...", this); return; }
        border.RebuildRing();
    }
    #endregion

    #region Debugging
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //always: the crack's path as a line, so cracks can be found while placing floor art
        Gizmos.color = new Color(1f, 0.6f, 1f, 0.8f);
        for (int i = 1; i < _Points.Count; i++) Gizmos.DrawLine(transform.TransformPoint(_Points[i - 1]), transform.TransformPoint(_Points[i]));
    }
#endif
    #endregion
}

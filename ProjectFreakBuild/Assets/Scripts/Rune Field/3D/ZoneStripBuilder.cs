using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//shared mesh code for the zone walls and floor cracks (see Zone Visuals Plan): reads the strip Steve models (or makes a stand-in card),
//lays copies of it end to end and welds the seams between them
//it only works out WHICH strip vertex ends up where along the length (Layout). The ring wall bends that along a circle, a crack along its path
//a plain C# class like RuneFieldTint, not a component: "ZoneStripBuilder.FromMesh(...)" makes one
public class ZoneStripBuilder
{
    const float EdgeTolerance = 0.001f; //how close (meters) a vertex has to be to count as on an end edge, or as matching another vertex

    //the strip, read once
    public Vector3[] _Vertices;
    public Vector3[] _Normals;
    public Vector2[] _UVs;
    public int[] _Triangles;
    public float _MinX, _MaxX, _MinY, _MaxY;
    public float _Length; //along X, in meters
    public bool _EndsMatch; //true if every start-edge vertex has a matching end-edge vertex, so the copies can be welded
    int[] _matchingEnd; //for each strip vertex: the end-edge vertex at the same height and depth if it's on the start edge, otherwise -1

    //the layout from the last Layout call: one entry per vertex in the finished mesh
    public List<int> _SourceIndex = new List<int>(); //which strip vertex it's a copy of
    public List<int> _Copy = new List<int>(); //which copy it belongs to (0, 1, 2...)
    public List<float> _Along = new List<float>(); //how far along the whole run it sits, in meters
    public List<int> _LayoutTriangles = new List<int>(); //the triangles, pointing at those vertices

    #region Reading
    public static ZoneStripBuilder FromMesh(Mesh mesh, float scale, bool flipLength, bool flipDepth, out string problem)
    {
        //reads a strip mesh, or makes the stand-in card if mesh is empty. Returns null (with problem filled in) if the mesh can't be used
        problem = "";
        ZoneStripBuilder strip = new ZoneStripBuilder();
        if (mesh == null) strip.MakeStandIn();
        else if (strip.ReadMesh(mesh, out problem) == false) return null;

        strip.Adjust(scale, flipLength, flipDepth);
        strip.MeasureAndMatchEnds();
        if (strip._Length <= EdgeTolerance) { problem = "the strip has no length along X"; return null; }
        return strip;
    }

    bool ReadMesh(Mesh mesh, out string problem)
    {
        //copies the mesh's data. Unity hands back empty arrays if the mesh can't be read (Read/Write off in its import settings, in play mode)
        problem = "";
        _Vertices = mesh.vertices;
        if (_Vertices.Length == 0) { problem = $"{mesh.name} has no vertices this tool can read. Rebuild in edit mode, or tick Read/Write in the strip's import settings (Model tab)"; return false; }

        _Triangles = mesh.triangles; //every submesh's triangles in one list
        _Normals = mesh.normals;
        if (_Normals.Length != _Vertices.Length) //no normals saved with the mesh: face the strip's back (-Z) as a stand-in
        {
            _Normals = new Vector3[_Vertices.Length];
            for (int i = 0; i < _Normals.Length; i++) _Normals[i] = Vector3.back;
        }

        _UVs = mesh.uv;
        if (_UVs.Length != _Vertices.Length) //no UVs: made from the bounds (U along the length, V up)
        {
            Bounds bounds = mesh.bounds;
            _UVs = new Vector2[_Vertices.Length];
            for (int i = 0; i < _UVs.Length; i++)
            {
                _UVs[i] = new Vector2(Mathf.InverseLerp(bounds.min.x, bounds.max.x, _Vertices[i].x), Mathf.InverseLerp(bounds.min.y, bounds.max.y, _Vertices[i].y));
            }
        }
        return true;
    }

    void MakeStandIn()
    {
        //a flat two-sided card until Steve's strip is in: 3 m long with a vertical edge every 25 cm, from 0.4 m under the floor to 1 m up
        const float length = 3f;
        const int columns = 12;
        float[] rows = { -0.4f, 0f, 0.35f, 0.7f, 1f };

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int side = 0; side < 2; side++) //side 0 faces -Z, side 1 faces +Z (the same card from the back)
        {
            int start = vertices.Count;
            Vector3 normal = side == 0 ? Vector3.back : Vector3.forward;
            for (int column = 0; column <= columns; column++)
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    float u = (float)column / columns;
                    vertices.Add(new Vector3(u * length, rows[row], 0f));
                    normals.Add(normal);
                    uvs.Add(new Vector2(u, (float)row / (rows.Length - 1)));
                }
            }

            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows.Length - 1; row++)
                {
                    int bottomLeft = start + column * rows.Length + row;
                    int topLeft = bottomLeft + 1;
                    int bottomRight = bottomLeft + rows.Length;
                    int topRight = bottomRight + 1;
                    if (side == 0) AddQuad(triangles, bottomLeft, topLeft, topRight, bottomRight);
                    else AddQuad(triangles, bottomLeft, bottomRight, topRight, topLeft); //listed the other way round, so it faces the other way
                }
            }
        }

        _Vertices = vertices.ToArray();
        _Normals = normals.ToArray();
        _UVs = uvs.ToArray();
        _Triangles = triangles.ToArray();
    }

    void AddQuad(List<int> triangles, int a, int b, int c, int d)
    {
        //two triangles making the four-sided face a b c d
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
        triangles.Add(a); triangles.Add(c); triangles.Add(d);
    }

    void Adjust(float scale, bool flipLength, bool flipDepth)
    {
        //applies the strip scale and flips. A flip mirrors the mesh, which turns its faces inside out, so one flip also reverses every triangle
        for (int i = 0; i < _Vertices.Length; i++)
        {
            Vector3 vertex = _Vertices[i] * scale;
            Vector3 normal = _Normals[i];
            if (flipLength) { vertex.x = -vertex.x; normal.x = -normal.x; }
            if (flipDepth) { vertex.z = -vertex.z; normal.z = -normal.z; }
            _Vertices[i] = vertex;
            _Normals[i] = normal;
        }

        if (flipLength == flipDepth) return; //no flips, or two (which cancel out)
        for (int i = 0; i < _Triangles.Length; i += 3)
        {
            int swap = _Triangles[i + 1];
            _Triangles[i + 1] = _Triangles[i + 2];
            _Triangles[i + 2] = swap;
        }
    }

    void MeasureAndMatchEnds()
    {
        //finds the strip's bounds, then pairs each start-edge vertex (smallest X) with the end-edge vertex at the same height and depth
        _MinX = float.MaxValue; _MaxX = float.MinValue; _MinY = float.MaxValue; _MaxY = float.MinValue;
        for (int i = 0; i < _Vertices.Length; i++)
        {
            _MinX = Mathf.Min(_MinX, _Vertices[i].x);
            _MaxX = Mathf.Max(_MaxX, _Vertices[i].x);
            _MinY = Mathf.Min(_MinY, _Vertices[i].y);
            _MaxY = Mathf.Max(_MaxY, _Vertices[i].y);
        }
        _Length = _MaxX - _MinX;

        _matchingEnd = new int[_Vertices.Length];
        _EndsMatch = true;
        for (int i = 0; i < _Vertices.Length; i++)
        {
            _matchingEnd[i] = -1;
            if (Mathf.Abs(_Vertices[i].x - _MinX) > EdgeTolerance) continue; //not on the start edge
            _matchingEnd[i] = FindEndMatch(i);
            if (_matchingEnd[i] == -1) _EndsMatch = false;
        }
    }

    int FindEndMatch(int startVertex)
    {
        //the end-edge vertex with the same height, depth and V as this start-edge vertex (and a normal facing the same way, so the two sides of a card stay apart). -1 if none
        Vector3 start = _Vertices[startVertex];
        for (int i = 0; i < _Vertices.Length; i++)
        {
            Vector3 end = _Vertices[i];
            if (Mathf.Abs(end.x - _MaxX) > EdgeTolerance) continue;
            if (Mathf.Abs(end.y - start.y) > EdgeTolerance || Mathf.Abs(end.z - start.z) > EdgeTolerance) continue;
            if (Mathf.Abs(_UVs[i].y - _UVs[startVertex].y) > EdgeTolerance) continue;
            if (Vector3.Dot(_Normals[i], _Normals[startVertex]) < 0.5f) continue;
            return i;
        }
        return -1;
    }

    public float GetHeight01(int stripVertex)
    {
        //how far up the strip a vertex sits: 0 at its bottom, 1 at its top
        return Mathf.InverseLerp(_MinY, _MaxY, _Vertices[stripVertex].y);
    }
    #endregion

    #region Layout
    public void Layout(int copies, float stretch)
    {
        //lays "copies" strips end to end, each stretched by "stretch" along its length, welding each start edge onto the end edge before it
        //fills the layout lists. The first copy's start and the last copy's end are never welded (a ring keeps that seam so its UVs can count up)
        _SourceIndex.Clear();
        _Copy.Clear();
        _Along.Clear();
        _LayoutTriangles.Clear();

        int count = _Vertices.Length;
        int[] map = new int[copies * count]; //strip vertex v of copy c is finished vertex map[c * count + v]
        for (int copy = 0; copy < copies; copy++)
        {
            for (int v = 0; v < count; v++)
            {
                if (copy > 0 && _EndsMatch && _matchingEnd[v] != -1)
                {
                    map[copy * count + v] = map[(copy - 1) * count + _matchingEnd[v]]; //welded: reuses the previous copy's end vertex
                    continue;
                }
                map[copy * count + v] = _SourceIndex.Count;
                _SourceIndex.Add(v);
                _Copy.Add(copy);
                _Along.Add((copy * _Length + (_Vertices[v].x - _MinX)) * stretch);
            }

            for (int t = 0; t < _Triangles.Length; t++) _LayoutTriangles.Add(map[copy * count + _Triangles[t]]);
        }
    }

    public Vector2 GetLayoutUV0(int layoutVertex)
    {
        //the strip's UV with U carried on from copy to copy (copy 2 runs 2 to 3), so a scrolling texture flows the whole way with no seam
        Vector2 uv = _UVs[_SourceIndex[layoutVertex]];
        return new Vector2(uv.x + _Copy[layoutVertex], uv.y);
    }
    #endregion

    #region Mesh
    public static Mesh MakeMesh(string meshName, List<Vector3> positions, List<Vector3> normals, List<Vector2> uv0, List<Vector2> uv2, List<Vector2> uv3, List<int> triangles)
    {
        //puts the lists into a new mesh. UV channels 2 and 3 are what Shader Graph's UV node calls UV2 and UV3
        Mesh mesh = new Mesh();
        mesh.name = meshName;
        if (positions.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //meshes past 65535 vertices need the bigger index type
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(2, uv2);
        mesh.SetUVs(3, uv3);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        if (positions.Count > 0) mesh.RecalculateTangents(); //for normal maps on the border shader
        return mesh;
    }

#if UNITY_EDITOR
    public static Mesh SaveMesh(Mesh built, string folder, string assetName)
    {
        //editor only: saves a built mesh as an asset in folder (made if missing). An existing asset is overwritten in place, so everything pointing at it keeps pointing at it
        string cleanFolder = string.IsNullOrWhiteSpace(folder) ? "Assets/Generated/Rune Field Zones" : folder.Trim().TrimEnd('/');
        MakeFolder(cleanFolder);
        string path = $"{cleanFolder}/{assetName}.asset";
        built.name = assetName;

        Mesh existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            UnityEditor.AssetDatabase.CreateAsset(built, path);
            return built;
        }
        UnityEditor.EditorUtility.CopySerialized(built, existing); //copies all the mesh data into the existing asset
        UnityEditor.EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(built); //the copy in memory isn't needed any more
        return existing;
    }

    static void MakeFolder(string folder)
    {
        //makes a folder path under Assets one level at a time ("Assets/Generated/Rune Field Zones")
        if (UnityEditor.AssetDatabase.IsValidFolder(folder)) return;
        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        MakeFolder(parent); //makes the parent first (a function calling itself like this is called recursion)
        UnityEditor.AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
#endif
    #endregion
}

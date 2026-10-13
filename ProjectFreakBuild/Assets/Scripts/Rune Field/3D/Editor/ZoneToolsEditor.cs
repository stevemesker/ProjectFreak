using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//editor-only helpers for the rune field zone tools (see Zone Visuals Plan). Lives in an Editor folder, so it's never in a build
//1. scene handles: drag a border's hand spots around its ring, and a crack's points across the floor, while that object is selected
//2. auto rebuild: when the wall or crack strip is saved over (reimported from Maya), every border in the open scenes rebuilds by itself
//[InitializeOnLoad] runs the static constructor when the editor loads scripts, which hooks both of these up
[InitializeOnLoad]
public static class ZoneToolsEditor
{
    static ZoneToolsEditor()
    {
        //hooks into the scene view's drawing. "-=" first so it never gets hooked twice
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    #region Scene Handles
    static void OnSceneGUI(SceneView sceneView)
    {
        //draws handles for whichever zone tool object is selected
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;
        if (selected.TryGetComponent(out ZoneBorder3DObject border)) DrawSpotHandles(border);
        if (selected.TryGetComponent(out ZoneCrackPath crack)) DrawCrackHandles(crack);
    }

    static void DrawSpotHandles(ZoneBorder3DObject border)
    {
        //a cyan dot per hand spot, dragged around the ring. Letting go sets the spot's angle (press Rebuild Ring to see it on the wall)
        Handles.color = Color.cyan;
        for (int i = 0; i < border.GetSpotCount(); i++)
        {
            Vector3 position = border.GetSpotWorldPosition(i);
            float size = HandleUtility.GetHandleSize(position) * 0.12f; //keeps the dot the same size on screen at any zoom

            EditorGUI.BeginChangeCheck(); //watches for the handle being dragged
            Vector3 moved = Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.SphereHandleCap);
            Handles.Label(position + border.transform.up * size * 2f, $"Spot {i}");
            if (EditorGUI.EndChangeCheck() == false) continue;

            Undo.RecordObject(border, "Move Wall Spot");
            border.SetSpotAngleFromWorld(i, moved);
        }
    }

    static void DrawCrackHandles(ZoneCrackPath crack)
    {
        //a dot per crack point, dragged across the floor (their height stays). A snapped start point shows as a ring and can't be dragged
        for (int i = 0; i < crack.GetPointCount(); i++)
        {
            Vector3 position = crack.GetWorldPoint(i);
            float size = HandleUtility.GetHandleSize(position) * 0.1f;
            if (i == 0 && crack.IsStartSnapped())
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(position, crack.transform.up, size);
                continue;
            }

            Handles.color = new Color(1f, 0.6f, 1f, 1f);
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck() == false) continue;

            Undo.RecordObject(crack, "Move Crack Point");
            crack.SetWorldPoint(i, ProjectOntoFloor(moved, position, crack.transform.up));
        }
    }

    static Vector3 ProjectOntoFloor(Vector3 moved, Vector3 original, Vector3 up)
    {
        //a free move handle follows the mouse in 3D. This takes out any up / down movement so the point slides across the floor
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane floor = new Plane(up, original);
        if (floor.Raycast(ray, out float distance)) return ray.GetPoint(distance);
        return Vector3.ProjectOnPlane(moved - original, up) + original;
    }
    #endregion
}

//watches for the strip being reimported (saved over from Maya) and rebuilds every border in the open scenes
//AssetPostprocessor is Unity's hook into importing: OnPostprocessAllAssets runs after any assets finish importing
public class ZoneStripImportWatcher : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        //only acts if one of the imported files is the zones asset's wall or crack strip
        RuneFieldZonesSO settings = RuneFieldZonesSO.FindInProject();
        if (settings == null) return;
        string wallPath = settings._WallStrip != null ? AssetDatabase.GetAssetPath(settings._WallStrip) : "";
        string crackPath = settings._CrackStrip != null ? AssetDatabase.GetAssetPath(settings._CrackStrip) : "";

        for (int i = 0; i < imported.Length; i++)
        {
            if (imported[i] != wallPath && imported[i] != crackPath) continue;
            EditorApplication.delayCall -= RebuildAllBorders; //rebuilding saves assets, which Unity doesn't allow in the middle of an import, so it waits until right after
            EditorApplication.delayCall += RebuildAllBorders;
            return;
        }
    }

    static void RebuildAllBorders()
    {
        //rebuilds every border in every open scene (edit mode only)
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ZoneBorder3DObject[] borders = Object.FindObjectsOfType<ZoneBorder3DObject>(true);
        for (int i = 0; i < borders.Length; i++) borders[i].RebuildRing();
    }
}

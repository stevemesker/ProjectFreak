using UnityEditor;
using UnityEngine;

public class DividerCreator 
{
    [MenuItem("GameObject/Create Divider", false, 0)]
    private static void CreateDivider()
    {
        GameObject divider = new GameObject("<===== Divider =====>");

        // Set position to world origin
        divider.transform.position = Vector3.zero;

        // Put it into the currently active scene
        GameObjectUtility.SetParentAndAlign(divider, Selection.activeGameObject);

        // Register with Undo
        Undo.RegisterCreatedObjectUndo(divider, "Create Divider");

        // Select the new object
        Selection.activeGameObject = divider;
    }
}

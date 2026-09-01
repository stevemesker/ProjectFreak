using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways] // Ensures the script runs in Edit Mode immediately
public class Unselectable : MonoBehaviour
{
    private void OnEnable()
    {
#if UNITY_EDITOR
        // Prevents the object from being clicked/selected in the Scene view
        SceneVisibilityManager.instance.DisablePicking(gameObject, false);
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        // Restores default selection capabilities if the script or object is disabled
        SceneVisibilityManager.instance.EnablePicking(gameObject, false);
#endif
    }
}

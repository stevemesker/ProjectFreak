using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLocationObject : MonoBehaviour
{
    public SceneLocationSO _LocationData;

    private void Awake()
    {
        if (_LocationData == null)
        {
            Debug.LogWarning("Warning! Location data has not been assigned for game object " + gameObject.name + "!");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "SO_NewLocationWarp", menuName ="ScriptableObjects/Environment/LocationChange")]

public class SceneLocationSO : ScriptableObject
{
    public string _Name;
    public string _Scene;

    public bool _IsLinked = true;
    [ShowIf("_IsLinked")]
    public SceneLocationSO _Link;
    [HideIf("_IsLinked")]
    public Vector3 _LinkLocation;

}

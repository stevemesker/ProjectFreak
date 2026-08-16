using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroy : MonoBehaviour
{
    private void OnEnable()
    {
        DontDestroyOnLoad(gameObject);
    }
    private void OnDisable()
    {
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
    }
}

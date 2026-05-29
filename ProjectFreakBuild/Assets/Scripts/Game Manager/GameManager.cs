using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    /////////////////////////////////////////////////////////////////////////////
    //Manager Script that handles core high level data management
    /////////////////////////////////////////////////////////////////////////////
    public static GameManager _GameManager;
    [SerializeField] ShadeManager shade;

    #region initializing
    private void Awake()
    {
        if (_GameManager != null)
        {
            Debug.LogError("Warning! duplicate game managers have been detected in script " + this + " in game manager " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        _GameManager = this;
    }
    #endregion

    #region Shade Data
    public List<ShadeSO> getShadeList()
    {
        return (shade._ShadeSlots);
    }
    #endregion
}

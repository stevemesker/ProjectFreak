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
    [SerializeField] int PlayerLevel;

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
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Shade Data
    public List<ShadeSO> getShadeList()
    {
        return (shade._ShadeSlots);
    }

    public ShadeManager getShadeManager()
    {
        return GetComponent<ShadeManager>();
    }
    #endregion

    #region Player Data
    public int GetPlayerCurrentLevel()
    {
        //gets the player's current level
        return PlayerLevel;
    }

    public void SetPlayerCurrentLevel(int NewLevel)
    {
        //sets player level to a specific level
        PlayerLevel = NewLevel;
    }

    public void IncrementPlayerLevel(int LevelAdd)
    {
        //increments the player's current level by an amount
        PlayerLevel += LevelAdd;
    }
    #endregion
}

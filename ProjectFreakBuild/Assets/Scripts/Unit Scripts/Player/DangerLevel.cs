using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DangerLevel
{
    [Tooltip("List of the danger levels and what percentage the unit's health must be compared to its max health in order to enter this level. Must be in descending order from smallest to largest danger level percentages")]
    public List<DangerSetting> _DangerLevels;

    public DangerType getCurrentDangerType(int currentHealth, int maxHealth)
    {
        //function that figures out what danger type the unit falls under given their current vs max health
        float temp = ((float)currentHealth/(float)maxHealth*100);
        DangerType tempType = DangerType.None;

        for(int i = 0; i < _DangerLevels.Count; i++)
        {
            if (temp > _DangerLevels[i]._Percentage)
            {
                tempType = _DangerLevels[i]._Type;
                i = _DangerLevels.Count;
            }
        }
        Debug.Log($"Current health percentage is {temp} | Multiplier will be in the {tempType} category");
        return tempType;
    }

    public int getCurrentDangerIndex(DangerType type)
    {
        for (int i = 0; i < _DangerLevels.Count; i++)
        {
            if (_DangerLevels[i]._Type == type) return i;
        } 
        return 0;
    }
}

[System.Serializable]
public class DangerSetting
{
    [Tooltip("What Danger Level the character enters when health reaches the _Percentage amount or below")]
    public DangerType _Type;
    [Tooltip("What health percentage the character must be at most to enter the Danger Type")]
    public float _Percentage;
    [Tooltip("How much the camera shake is dampened by being in this Danger Level")]
    public float _ShakeDampenMultiplier;
}

public enum DangerType
{
    None,
    Small,
    Medium,
    Large
}

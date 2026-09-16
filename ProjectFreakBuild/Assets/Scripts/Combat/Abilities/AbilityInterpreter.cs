using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityInterpreter : MonoBehaviour
{
    //function that manages the timing and execution of abilities
    Coroutine _currentTimer;
    int _abilityStepCount;
    int _currentAbilityStep;
    AbilitySO _CurrentAbility;

    public void InitializeAbility(AbilitySO ability)
    {
        //if (_CurrentAbility != null) return;
        Debug.Log($"Now starting ability {ability._AbilityName}");
        _CurrentAbility = ability;
        _abilityStepCount = _CurrentAbility.GetAbilityStepCount();
        _currentAbilityStep = 0;
        
        ExecuteAbility();
    }

    public void ExecuteAbility()
    {
        _CurrentAbility.InvokeAbility(_currentAbilityStep,gameObject, this);
        if (_CurrentAbility._steps[0].Timing > 0)
        {
            Debug.Log($"Ability {_CurrentAbility._AbilityName} has a wait timer");
            ActivateAdvancementTimer(_CurrentAbility._steps[0].Timing);
        }
        else
        {
            Debug.Log($"Ability {_CurrentAbility._AbilityName} does not have a wait timer");
            AdvanceAbilityStep();
        }
    }

    #region state machine
    public void AdvanceAbilityStep()
    {
        print($"now upping ability step index to {_currentAbilityStep + 1} out of {_abilityStepCount}");
        _currentAbilityStep++;
        if (_currentAbilityStep >= _abilityStepCount)
        {
            EndAbility();
        }
        else
        {
            ExecuteAbility();
        }
    }

    public void EndAbility()
    {
        Debug.Log($"Now ending ability {_CurrentAbility._AbilityName}");
        _abilityStepCount = 0;
        _currentAbilityStep = 0;
        _CurrentAbility = null;
    }

    public void InterruptAbility()
    {
        //function for completely stopping the ability from happening
        if (_CurrentAbility != null) Debug.Log($"{_CurrentAbility._AbilityName} has been canceled");
        _CurrentAbility = null;
        _abilityStepCount = 0;
        _currentAbilityStep = 0;
        if (_currentTimer != null)
        {
            StopCoroutine(_currentTimer);
            _currentTimer = null;
        }
    }
    #endregion

    #region Ability Function Calls
    public void ActivateAdvancementTimer(float waitTime)
    {
        _currentTimer = StartCoroutine(AdvancementTimer(waitTime));
    }

    IEnumerator AdvancementTimer (float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        AdvanceAbilityStep();
    }
    #endregion

    #region Debugging
    public void AbilIntLog(string message)
    {
        Debug.Log($"{gameObject.name} ability message log: " + message);
    }
    #endregion
}

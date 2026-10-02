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
            ActivateAdvancementTimer(_CurrentAbility._steps[0].Timing);
        }
        else
        {
            AdvanceAbilityStep();
        }
    }

    #region state machine
    public void AdvanceAbilityStep()
    {
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
        _abilityStepCount = 0;
        _currentAbilityStep = 0;
        _CurrentAbility = null;
    }

    public void InterruptAbility()
    {
        //function for completely stopping the ability from happening
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

    public void SummonUnit(Vector3 position, Quaternion rotation)
    {
        GameObject instancedUnit = Instantiate(_CurrentAbility._steps[_currentAbilityStep]._StepObject, position, rotation);
        if (instancedUnit.TryGetComponent<ISummonUnit>(out ISummonUnit summonedUnit))
        {
            //todo: set up the summoned unit here
        }
    }
    #endregion

    #region Debugging
    public void AbilIntLog(string message)
    {
        Debug.Log($"{gameObject.name} ability message log: " + message);
    }
    #endregion
}

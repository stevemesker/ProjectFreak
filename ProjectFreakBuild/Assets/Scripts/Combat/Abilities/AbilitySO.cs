using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SO_Ability_", menuName = "Ability", order = 0)]
public class AbilitySO : ScriptableObject
{
    [System.Serializable]
    public class AbilityEvent : UnityEngine.Events.UnityEvent<GameObject> { }

    [Header("Ability Information")]
    public string _AbilityName;

    [Header("Ability Stats")]
    public float _AbilityCooldown = 0.5f;

    [Header("Instructions")]
    [SerializeReference]
    public List<AbilityStep> _steps;

    public void InvokeAbility (int index, GameObject source, AbilityInterpreter interpreter)
    {
        Debug.Log($"Now launching ability step {index} out of {_steps.Count-1}");
        _steps[index].InvokeStep(source, interpreter);
    }

    public int GetAbilityStepCount()
    {
        return _steps.Count;
    }
}

[System.Serializable]
public class AbilityStep
{
    [SerializeField] string DevNotes;
    public float Timing;
    [Tooltip("Used for any object spawning for this step")]
    public GameObject _StepObject;
    [SerializeReference]
    public List<AbilityFunction> _Functions;

    public void InvokeStep(GameObject source, AbilityInterpreter interpreter)
    {
        foreach(AbilityFunction function in _Functions)
        {
            function.ActivateAbility(source, interpreter);
        }
    }
}

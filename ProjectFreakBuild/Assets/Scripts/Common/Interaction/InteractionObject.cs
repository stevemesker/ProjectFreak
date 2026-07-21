using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InteractionObject : MonoBehaviour, IInteractable
{
    public UnityEvent ActivateObject;

    public bool CanInteract()
    {
        return true;
    }

    public void Interact(GameObject Origin)
    {
        ActivateObject?.Invoke();
    }
}

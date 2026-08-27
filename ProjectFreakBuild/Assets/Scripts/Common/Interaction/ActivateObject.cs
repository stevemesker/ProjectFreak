using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ActivateObject : MonoBehaviour
{
    // Script used so that the player can interact with objects that have the IActivateInteraction interface
    [SerializeField] float _ActivateVolumeRadius;
    [SerializeField] float _ActivateOffset;
    public bool CanActivate = true;


    [SerializeField] private HashSet<GameObject> hitList;
    private PlayerInput pInput;

    #region Initialization
    private void Awake()
    {
        pInput = new PlayerInput();
    }

    private void OnEnable()
    {
        pInput.Player.Interact.performed += Activate;
        pInput.Enable();
    }

    private void OnDisable()
    {
        pInput.Player.Interact.performed -= Activate;
        pInput.Disable();
    }
    #endregion

    public void Activate(InputAction.CallbackContext context)
    {
        if (CanActivate == false) return;
        
        hitList = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(
            transform.position + (transform.forward * _ActivateOffset),
            _ActivateVolumeRadius
            );

        IInteractable interactor = interactTest(hits);
        if (interactor == null) return;
        interactor.Interact(gameObject);
    }

    public void SetInteractBusy(bool set)
    {
        CanActivate = set;
    }

    IInteractable interactTest(Collider[] hits)
    {
        //function that finds available interactable objects

        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            //print(hit.name);
            if (!hit.TryGetComponent(out IInteractable interactable))
                continue;
            if (!interactable.CanInteract())
                continue;

            float distance = Vector3.Distance(
                transform.position,
                hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }
            return closest;
    }
}

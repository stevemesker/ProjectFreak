using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class EvolutionNode : MonoBehaviour, iEvolutionNode
{
    [Header("<=====Active State=====>")]
    public bool _ActivationState;
    public bool _LockedOut;

    [Header("<=====Pointers=====>")]
    public GameObject PluggedInNode; //element gameobject that is plugged into this node

    [Header("<=====GateKeeper Settings=====>")]
    [SerializeField, Tooltip("List of nodes that must be activated before this one can")] 
    List<EvolutionNode> unlocks;
    [SerializeField, Tooltip("List of nodes that will be locked out as long as this node is active")] 
    List<EvolutionNode> Lockouts;

    [Header("<---Events--->")]
    [SerializeField] public UnityEvent ActivationEvent;
    [SerializeField] public UnityEvent DeactivationEvent;

    [Header("<---may delete--->")]
    public int nodeID;//used to tell which node a specific one is for saving out data later
    public List<EvolutionNode> connectedNodes;
    public bool NodeEnabled;
    public bool Nodelocked;
    
    [SerializeField] private List<GameObject> nodeStateBackground; //which background states need to be activated based on node's current activation state

    public bool isPlugged()
    {
        if (PluggedInNode == null) return false;
        return true;
    }
    public void PlugElement(GameObject ElementToPlug)
    {
        PluggedInNode = ElementToPlug;
    }

    public void UnplugElement ()
    {
        PluggedInNode = null;
        _ActivationState = false;
        if (Lockouts.Count > 0) for (int i = 0; i < Lockouts.Count; i++)
            {
                Lockouts[i].SetLockoutNodeState(false);
            }
        DeactivationEvent?.Invoke();
    }

    public void SetLockoutNodeState(bool State)
    {
        _LockedOut = State;
    }


    public void ActivatePluggedNode()
    {
        if (CanActivate() == false) return;
        _ActivationState = true;
        if (Lockouts.Count > 0) for (int i = 0; i < Lockouts.Count; i++)
            {
                Lockouts[i].SetLockoutNodeState(true);
            }
        ActivationEvent?.Invoke();
    }

    bool CanActivate()
    {
        //attached element power check
        if (PluggedInNode.GetComponent<ElementItem>().CurrentPower <= 0) return false;

        //lockout check
        if (_LockedOut) return false;
        
        //unlock list gate
        if (unlocks.Count > 0)
            for (int i = 0; i<unlocks.Count; i++)
            {
                if (unlocks[i]._ActivationState == false) return false;
            }

        return true;
    }

    #region StateActivation
    [Button ("Activate node")]
    public void ActivateNode()
    {
        //function that increases whatever stat when the appropriate consumable is used to activate it
        if (NodeEnabled == false || Nodelocked == true)return;

        //the node does things here
        print("the node is doin stuff");
    }

    [Button("Enable node")]
    public void EnableNode()
    {
        //function that enables a node to be activated
        if (Nodelocked) return;
        NodeEnabled = true;
        SetState(1);
    }

    [Button("Lock node")]
    public void LockNode()
    {
        //function that locks out a node from ever being used. Typically stops someone from evolving a shade within its evolutionary group
        Nodelocked = true;
        NodeEnabled = false;
        SetState(2);
    }
    [Button("Hide node")]
    public void HideNode()
    {
        //function that changes a node from known to hidden. Not sure I'll ever need this but it's here... Maybe for resetting when chosing a new shade
        NodeEnabled = false;
        Nodelocked = false;
        SetState(0);
    }

    private void SetState(int state)
    {
        switch (state)
        {
            case 0:
                nodeStateBackground[0].SetActive(true);
                nodeStateBackground[1].SetActive(false);
                nodeStateBackground[2].SetActive(false);
                break;
            case 1:
                nodeStateBackground[0].SetActive(false);
                nodeStateBackground[1].SetActive(true);
                nodeStateBackground[2].SetActive(false);
                break;
            case 2:
                nodeStateBackground[0].SetActive(false);
                nodeStateBackground[1].SetActive(false);
                nodeStateBackground[2].SetActive(true);
                break;
            default:
                Debug.LogError("ERROR! Node " + gameObject.name + " with ID " + nodeID + " is trying to update state but stae number " + state + " is not accounted for in the switch statement...");
                nodeStateBackground[0].SetActive(false);
                nodeStateBackground[1].SetActive(false);
                nodeStateBackground[2].SetActive(true);
                break;
        }
    }
    #endregion
}

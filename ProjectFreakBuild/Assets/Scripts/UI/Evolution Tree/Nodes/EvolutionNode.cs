using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

//an ability node on the 2D rune field. It's a view now: RuneField decides if the node is on, this shows it and fires the events
//its unlock and lockout lists are still where the rules come from when the layout is built from the scene (see RuneFieldManager)
public class EvolutionNode : MonoBehaviour
{
    [Header("<=====Active State=====>")]
    [Tooltip("Is this node turned on right now (read only, set by the rune field)")]
    [ReadOnly] public bool _ActivationState;
    [Tooltip("Is an active node locking this one out right now (read only, set by the rune field)")]
    [ReadOnly] public bool _LockedOut;

    [Header("<=====GateKeeper Settings=====>")]
    [SerializeField, Tooltip("List of nodes that must be activated before this one can")]
    List<EvolutionNode> unlocks;
    [SerializeField, Tooltip("List of nodes that will be locked out as long as this node is active")]
    List<EvolutionNode> Lockouts;

    [Header("<---Events--->")]
    [Tooltip("Runs when the node turns on while the player is editing the field (not when a saved field loads)")]
    [SerializeField] public UnityEvent ActivationEvent;
    [Tooltip("Runs when the node turns off while the player is editing the field (not when a saved field loads)")]
    [SerializeField] public UnityEvent DeactivationEvent;

    [Header("<---may delete--->")]
    public int nodeID;//used to tell which node a specific one is for saving out data later
    public List<EvolutionNode> connectedNodes;
    public bool NodeEnabled;
    public bool Nodelocked;

    [SerializeField] private List<GameObject> nodeStateBackground; //which background states need to be activated based on node's current activation state

    //local variables
    const int EnabledBackground = 1; //index of the "Enabled" background, shown while the node is on
    const int LockedBackground = 2; //index of the "Locked" background, shown while the node is locked out

    #region Rules
    public List<EvolutionNode> GetUnlockNodes()
    {
        return unlocks;
    }

    public List<EvolutionNode> GetLockoutNodes()
    {
        return Lockouts;
    }
    #endregion

    #region Display
    public void ShowState(bool active, bool lockedOut, bool fireEvents)
    {
        //function the rune field calls after every change to show if this node is on or locked out
        //events only fire when the state actually changes, and only if fireEvents is true (it's false when a saved field loads)
        bool wasActive = _ActivationState;
        _ActivationState = active;
        _LockedOut = lockedOut;

        if (active) ShowBackground(EnabledBackground);
        else if (lockedOut) ShowBackground(LockedBackground);
        else ShowBackground(-1); //plain look, no state background

        if (fireEvents == false || active == wasActive) return;
        if (active) ActivationEvent?.Invoke();
        else DeactivationEvent?.Invoke();
    }

    void ShowBackground(int index)
    {
        //turns on one state background and turns the rest off. -1 turns them all off
        for (int i = 0; i < nodeStateBackground.Count; i++)
        {
            if (nodeStateBackground[i] == null) continue;
            nodeStateBackground[i].SetActive(i == index);
        }
    }
    #endregion

    #region StateActivation
    [Button ("Activate node")]
    public void ActivateNode()
    {
        //function that increases whatever stat when the appropriate consumable is used to activate it
        if (NodeEnabled == false || Nodelocked == true)return;

        //the node does things here
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

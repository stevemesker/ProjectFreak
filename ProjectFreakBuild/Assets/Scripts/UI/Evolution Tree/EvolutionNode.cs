using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EvolutionNode : MonoBehaviour
{
    public int nodeID;//used to tell which node a specific one is for saving out data later
    public List<EvolutionNode> connectedNodes;
    public bool NodeEnabled;
    public bool Nodelocked;
    

    [SerializeField] private List<GameObject> nodeStateBackground; //which background states need to be activated based on node's current activation state

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
}

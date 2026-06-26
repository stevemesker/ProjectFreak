using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RuneFieldManager : MonoBehaviour
{
    [SerializeField] GameObject CorePointer;
    public List<GameObject> ListOfNodes;
    [SerializeField]private List<GameObject> ListOfRunes;
    [SerializeField] GameObject elementPrefab;
    

    #region rune field editing
    public void addRuneList(GameObject rune)
    {
        ListOfRunes.Add(rune);
    }

    public void removeRuneList(GameObject rune)
    {
        ListOfRunes.Remove(rune);
    }

    public void ResetRunePower()
    {
        //function that resets the power on the field
        for (int i = 0; i < ListOfRunes.Count; i++)
        {
            ListOfRunes[i].GetComponent<ElementItem>().CurrentPower = 0;
        }
    }

    public void ResetRuneChecked()
    {
        //function that resets the checked state of all runes
        for (int i = 0; i < ListOfRunes.Count; i++)
        {
            ListOfRunes[i].GetComponent<ElementItem>().CheckedReset();
        }
    }
    #endregion
    
    public void UpdateScaler()
    {
        //function that gets called from the scaling script to deal with unity's awful way of handling colliders and such
        //probably don't have to do this if I used a 2d collider but I don't feel like refactoring stuff, sue me

        if (GetComponent<RectTransform>().localScale.x < 1) 
        { 
            for (int i = 0; i < ListOfRunes.Count; i++)
            {
                ListOfRunes[i].GetComponent<SphereCollider>().radius = (ListOfRunes[i].GetComponent<RectTransform>().rect.width * GetComponent<RectTransform>().localScale.x) / 2;
            }

            for (int i = 0; i < ListOfNodes.Count; i++)
            {
                ListOfNodes[i].GetComponent<SphereCollider>().radius = (ListOfNodes[i].GetComponent<RectTransform>().rect.width * GetComponent<RectTransform>().localScale.x) / 2;
            }
            CorePointer.GetComponent<SphereCollider>().radius = (CorePointer.GetComponent<RectTransform>().rect.width * GetComponent<RectTransform>().localScale.x) / 2;
        }
        //Debug.LogWarning(ListOfRunes[0].GetComponent<RectTransform>().rect.width);
        //Debug.LogWarning(GetComponent<RectTransform>().localScale.x);
    }

    

    #region rune field saving
    public void loadRuneField(int index)
    {
        //shade slot manager under shade selection object notifies of loading
        print("Now loading shade of slot " + index);
        ClearRuneField();

        ShadeManager SM = GameManager._GameManager.getShadeManager();
        SM.setShadeSelection(index); //changes the current selected shade slot
        LoadRuneFieldFromPackage(SM.getCurrentShade());
    }

    [Button("Save Current Field")]
    public void SaveRuneSlot()
    {
        if (GameManager._GameManager == null) return;
        GameManager._GameManager.getShadeManager().saveCurrentShadeRuneFieldPackage(SaveRuneFieldPackage());
    }

    public RuneFieldPackage SaveRuneFieldPackage()
    {
        RuneFieldPackage temp = new RuneFieldPackage();
        List<RunePackage> runes = new List<RunePackage>();
        List<NodePackage> nodes = new List<NodePackage>();
        CorePackage Core = new CorePackage();

        //helper local variables
        int index = new int();
        ElementItem currentElement = new ElementItem();

        //<---Core List Build--->//
        Core._CoreCurrentPower = CorePointer.GetComponent<CoreNode>().CoreNodeCurrentPower;
        Core._ConnectionIndexRef = new List<int>();
        for (int i = 0; i < CorePointer.GetComponent<CoreNode>().connectionNodes.Count; i++)
        {
            //Core._ConnectionIndexRef.Add(CorePointer.GetComponent<CoreNode>().connectionNodes[i]);
            print(CorePointer.GetComponent<CoreNode>().connectionNodes[i].name);
            index = ListOfRunes.IndexOf(CorePointer.GetComponent<CoreNode>().connectionNodes[i]);
            Core._ConnectionIndexRef.Add(index);
        }

        //<---Rune List Build--->//
        for (int i = 0; i < ListOfRunes.Count; i++)
        {
            RunePackage tempElement = new RunePackage();
            currentElement = ListOfRunes[i].GetComponent<ElementItem>();

            //get element data SO
            tempElement._elementDataPointer = currentElement.getElementSOAttachment();
            //get element position without zoom scale factor
            tempElement._elementPosition = (ListOfRunes[i].GetComponent<RectTransform>().position - gameObject.GetComponent<RectTransform>().position) / gameObject.GetComponent<RectTransform>().localScale.x;
            //get current power
            tempElement._currentPower = currentElement.CurrentPower;
            //get connection index
            tempElement._connectionIndexRef = new List<int>();
            if (currentElement.connectionsCurrent.Count > 0)
            {
                for (int x = 0; x < currentElement.connectionsCurrent.Count; x++)
                {
                    index = ListOfRunes.IndexOf(currentElement.connectionsCurrent[x]);
                    if (index != -1)
                        tempElement._connectionIndexRef.Add(index);
                }
                if (tempElement._connectionIndexRef.Count == 0) tempElement._connectionIndexRef.Add(-1);
            }
            else
                tempElement._connectionIndexRef.Add(-1);

            runes.Add(tempElement);
        }

        //<---Node List Build--->//
        NodePackage tempNode = new NodePackage();

        for (int j = 0; j < ListOfNodes.Count; j++)
        {
            if (ListOfNodes[j].GetComponent<EvolutionNode>() != null)
                if (ListOfNodes[j].GetComponent<EvolutionNode>().PluggedInNode != null)
                {
                    tempNode._NodeIndex = j;
                    tempNode._ElementIndex = ListOfRunes.IndexOf(ListOfNodes[j].GetComponent<EvolutionNode>().PluggedInNode);
                    tempNode._IsPowered = ListOfNodes[j].GetComponent<EvolutionNode>()._ActivationState;
                    nodes.Add(tempNode);
                }
        }

        Debug.LogWarning(nodes.Count + " activated nodes have been added.");

        temp._Runes = runes;
        temp._Nodes = nodes;
        temp._Core = Core;
        return temp;
    }

    public void LoadRuneFieldFromPackage(ShadeSO currentShade)
    {
        if (GameManager._GameManager == null) return;

        RuneFieldPackage currentPackage = currentShade._RuneFieldPackage;
        ElementItem currentElement = new ElementItem();

        //<---set core node stats--->//
        CorePointer.GetComponent<CoreNode>().CoreNodeMaxPower = currentShade._shadeStats._LVL;
        CorePointer.GetComponent<CoreNode>().CoreNodeCurrentPower = currentPackage._Core._CoreCurrentPower;

        //spawn elements
        for (int i = 0; i < currentPackage._Runes.Count; i++)
        {
            GameObject temp = Instantiate(elementPrefab, gameObject.GetComponent<RectTransform>().position + currentPackage._Runes[i]._elementPosition * gameObject.GetComponent<RectTransform>().localScale.x, Quaternion.identity, transform);
            ListOfRunes.Add(temp);
            currentElement = temp.GetComponent<ElementItem>();
            currentElement.setElementSOAttachment(currentPackage._Runes[i]._elementDataPointer);
            currentElement.CurrentPower = currentPackage._Runes[i]._currentPower;
        }



        //go through each element
        for (int i = 0; i < currentPackage._Runes.Count; i++)
        {
            //current element is the current element we have in our spawned list
            currentElement = ListOfRunes[i].GetComponent<ElementItem>();

            //check to make sure our saved package says this element has attachments to other elements
            if (currentPackage._Runes[i]._connectionIndexRef[0] > -1)
            {
                //since we have at least 1 connection that isn't to core node, go through and connect this node to that indexed node
                for (int j = 0; j < currentPackage._Runes[i]._connectionIndexRef.Count; j++)
                {
                    currentElement.connectionsCurrent.Add(ListOfRunes[currentPackage._Runes[i]._connectionIndexRef[j]]);
                }

                currentElement.LoadReconnect();
            }
        }

        //<--Connect core to nodes-->//
        for (int i = 0; i < currentPackage._Core._ConnectionIndexRef.Count; i++)
        {
            CorePointer.GetComponent<CoreNode>().connectionNodes.Add(ListOfRunes[currentPackage._Core._ConnectionIndexRef[i]]);
        }
        CorePointer.GetComponent<IBridgeable>().LoadReconnect();
    }

    [Button("Test Clear")]
    public void ClearRuneField()
    {
        for (int i = 0; i < ListOfRunes.Count; i++)
        {
            ListOfRunes[i].GetComponent<IConnectable>().ClearConnection();
            Destroy(ListOfRunes[i]);
        }
        ListOfRunes.Clear();

        CorePointer.GetComponent<CoreNode>().ResetPower();

        for (int j = 0; j < ListOfNodes.Count; j++)
        {
            ListOfNodes[j].GetComponent<iEvolutionNode>().resetNode();
        }
    }

    #endregion
}



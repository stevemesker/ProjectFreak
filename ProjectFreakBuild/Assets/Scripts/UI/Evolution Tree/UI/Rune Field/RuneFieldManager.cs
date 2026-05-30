using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RuneFieldManager : MonoBehaviour
{
    public List<GameObject> ListOfNodes;
    [SerializeField]private List<GameObject> ListOfRunes;

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
        }
        //Debug.LogWarning(ListOfRunes[0].GetComponent<RectTransform>().rect.width);
        //Debug.LogWarning(GetComponent<RectTransform>().localScale.x);
    }

    #region rune field saving

    [Button("Test Save")]
    public RuneFieldPackage SaveRuneFieldPackage()
    {
        RuneFieldPackage temp = new RuneFieldPackage();
        List<RunePackage> runes = new List<RunePackage>();
        List<NodePackage> nodes = new List<NodePackage>();

        //helper local variables
        int index = new int();
        ElementItem currentElement = new ElementItem();

        //<---Rune List Build--->//
        for (int i = 0; i < ListOfRunes.Count; i++)
        {
            RunePackage tempElement = new RunePackage();
            currentElement = ListOfRunes[i].GetComponent<ElementItem>();

            //get element data SO
            tempElement._elementDataPointer = currentElement.getElementSOAttachment();
            //get element position without zoom scale factor
            tempElement._elementPosition = (gameObject.GetComponent<RectTransform>().position - ListOfRunes[i].GetComponent<RectTransform>().position) / gameObject.GetComponent<RectTransform>().localScale.x;
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
            }
            else
                tempElement._connectionIndexRef.Add(-1);

            Debug.LogWarning("Saved Node =====" + i + "=====");
            Debug.LogWarning("Rune Data Name " + tempElement._elementDataPointer.name);
            Debug.LogWarning("Rune position " + tempElement._elementPosition);
            Debug.LogWarning("Rune Power " + tempElement._currentPower);
            Debug.LogWarning("First Connection Index " + tempElement._connectionIndexRef[0]);

            runes.Add(tempElement);
        }

        temp._Runes = runes;
        temp._Nodes = nodes;
        return temp;
    }

    

    #endregion
}



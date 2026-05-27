using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneFieldManager : MonoBehaviour
{
    public List<GameObject> ListOfNodes;
    [SerializeField]private List<GameObject> ListOfRunes;
    
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
}

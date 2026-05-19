using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneFieldManager : MonoBehaviour
{
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
}

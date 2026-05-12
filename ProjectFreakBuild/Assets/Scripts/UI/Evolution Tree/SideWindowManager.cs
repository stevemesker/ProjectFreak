using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideWindowManager : MonoBehaviour
{
    [Header("<======Window Nav======>")]
    public List<GameObject> windowsList;
    [Header("<---Nav private Variables--->")]
    [SerializeField] private GameObject CurrentActivatedWindow;
    [SerializeField] private GameObject ControlledPanel;

    public void CloseWindow(GameObject window)
    {
        float offset = ControlledPanel.GetComponent<RectTransform>().rect.width; //gets the offset for closing

        print("Button has selected " + window.name);

        if (CurrentActivatedWindow == window)
        {
            GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, GetComponent<RectTransform>().anchoredPosition.y);
            CurrentActivatedWindow.SetActive(false);
            CurrentActivatedWindow = null;
            return;
        }
        //if (windowsList.Contains(window) == false) { Debug.LogWarning("Warning! Button is toggling a button that has not been added to the list!"); return; }

        if (CurrentActivatedWindow != null)
        {
            CurrentActivatedWindow.SetActive(false);
        }
        else
        {
            GetComponent<RectTransform>().anchoredPosition = new Vector2(0, GetComponent<RectTransform>().anchoredPosition.y);
        }

        print("Opening " + window.name);
        window.SetActive(true);
        CurrentActivatedWindow = window;

        /*
        if (window == CurrentActivatedWindow) 
        { 
            print("Closing Whole Window...");
            GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, GetComponent<RectTransform>().anchoredPosition.y);
            CurrentActivatedWindow = null;
            CurrentActivatedWindow.SetActive(false);
            return; 
        }
        if (CurrentActivatedWindow != null)
        {
            CurrentActivatedWindow.SetActive(false);
        }
        else
        {
            GetComponent<RectTransform>().anchoredPosition = new Vector2(0, GetComponent<RectTransform>().anchoredPosition.y);
        }

        print("Opening " + window.name);
        window.SetActive(true);
        CurrentActivatedWindow = window;*/
    }
}

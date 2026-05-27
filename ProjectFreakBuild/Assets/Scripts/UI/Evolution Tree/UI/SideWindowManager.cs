using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideWindowManager : MonoBehaviour
{
    
    [Header("<======Window Nav======>")]
    [SerializeField] private GameObject CurrentActivatedWindow;
    [SerializeField] private GameObject ControlledPanel;

    public void CloseWindow(GameObject window)
    {
        //opens/closes/ switches side pannel windows
        float offset = ControlledPanel.GetComponent<RectTransform>().rect.width; //gets the offset for closing

        print("Button has selected " + window.name);

        if (CurrentActivatedWindow == window)
        {
            GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, GetComponent<RectTransform>().anchoredPosition.y);
            CurrentActivatedWindow.SetActive(false);
            CurrentActivatedWindow = null;
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
        CurrentActivatedWindow = window;
    }
}

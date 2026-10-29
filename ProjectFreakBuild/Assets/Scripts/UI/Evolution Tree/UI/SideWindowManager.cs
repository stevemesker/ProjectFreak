using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SideWindowManager : MonoBehaviour
{
    
    [Header("<======Window Nav======>")]
    [SerializeField] private GameObject CurrentActivatedWindow;
    [SerializeField] private GameObject ControlledPanel;

    [Tooltip("The window Toggle Side Panel opens the first time (before any window was opened). Left empty, the inventory list's window is used")]
    [SerializeField] GameObject _DefaultWindow;

    //local variables
    GameObject _lastWindow; //the last window opened, so toggling the panel back open shows it again

    public void CloseWindow(GameObject window)
    {
        //opens/closes/ switches side pannel windows
        float offset = ControlledPanel.GetComponent<RectTransform>().rect.width; //gets the offset for closing

        //print("Button has selected " + window.name);

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

        //print("Opening " + window.name);
        window.SetActive(true);
        CurrentActivatedWindow = window;
        _lastWindow = window;
    }

    public void ToggleSidePanel()
    {
        //opens the side panel on the last window used (or the default one), or closes it if it's open. The 3D rune field calls this for gamepad Y and Tab
        if (CurrentActivatedWindow != null) { CloseWindow(CurrentActivatedWindow); return; } //CloseWindow on the open window closes the panel

        GameObject window = _lastWindow != null ? _lastWindow : _DefaultWindow;
        if (window == null)
        {
            IngredientListWindow inventory = GetComponentInChildren<IngredientListWindow>(true); //true includes inactive objects, since closed windows are turned off
            if (inventory != null) window = inventory.gameObject;
        }
        if (window == null) { Debug.LogWarning($"Warning! {gameObject.name} has no window to open (no Default Window and no inventory list under it)...", this); return; }
        CloseWindow(window);
    }

    public bool IsOpen()
    {
        //true while a window is showing
        return CurrentActivatedWindow != null;
    }

    public Selectable GetFirstSelectable()
    {
        //what gamepad focus should start on: the first item in the open window (like the first rune in the list), otherwise the first tab
        Selectable found = FindSelectable(CurrentActivatedWindow);
        if (found == null) found = FindSelectable(gameObject);
        return found;
    }

    public bool Contains(GameObject target)
    {
        //true if target is part of the side panel (so focus on it counts as the panel having focus)
        return target != null && target.transform.IsChildOf(transform);
    }

    Selectable FindSelectable(GameObject root)
    {
        //the first button-like thing under root that's showing and can be pressed. Scrollbars are skipped, focus should land on content
        if (root == null) return null;
        Selectable[] all = root.GetComponentsInChildren<Selectable>(); //in hierarchy order, so the top of a list comes first
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is Scrollbar) continue;
            if (all[i].IsActive() && all[i].IsInteractable()) return all[i];
        }
        return null;
    }
}

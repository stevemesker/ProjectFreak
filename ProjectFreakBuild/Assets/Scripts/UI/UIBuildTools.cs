using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//small helpers for "Build Default Layout" style editor buttons that make UI objects from code (ConfirmPopup, ShadeInfoWindow)
//static: called as UIBuildTools.MakeChild(...) without needing an object first
public static class UIBuildTools
{
    public static RectTransform MakeChild(string childName, Transform parent)
    {
        //makes an empty UI object under a parent
        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent, false); //false keeps its local position, so it lands where we set it
        return child.GetComponent<RectTransform>();
    }

    public static void Stretch(RectTransform rect)
    {
        //makes a UI object fill its parent
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static TextMeshProUGUI MakeText(string childName, Transform parent, string text, float fontSize, Vector2 position, Vector2 size)
    {
        //makes a centered white text object
        RectTransform rect = MakeChild(childName, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return label;
    }
}

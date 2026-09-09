using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RadialButton : MonoBehaviour
{
    public AbilitySO _AbilityActivation;

    public void OnSelection(ColorPaletteSO _colorPalette)
    {
        gameObject.GetComponent<Image>().color = _colorPalette._SecondaryColor;
    }

    public void OnDeselction(ColorPaletteSO _colorPalette)
    {
        gameObject.GetComponent<Image>().color = _colorPalette._PrimaryColor;
    }

    public void Activation()
    {
        Debug.Log($"Now activating {_AbilityActivation.name}");
    }
}

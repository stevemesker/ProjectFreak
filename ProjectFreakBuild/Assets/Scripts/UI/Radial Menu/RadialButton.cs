using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RadialButton : MonoBehaviour
{
    public AbilitySO _AbilityActivation;
    [SerializeField] GameObject _IconPointer;

    public void OnSelection(ColorPaletteSO _colorPalette)
    {
        gameObject.GetComponent<Image>().color = _colorPalette._SecondaryColor;
    }

    public void OnDeselction(ColorPaletteSO _colorPalette)
    {
        gameObject.GetComponent<Image>().color = _colorPalette._PrimaryColor;
    }

    public void Activation(GameObject Source)
    {
        Debug.Log($"Now activating {_AbilityActivation.name}");
        Source.GetComponent<AbilityInterpreter>().InitializeAbility(_AbilityActivation);
        //_AbilityActivation.InvokeAbility(Source);
    }

    public void SetUpDial(AbilitySO buttonData, float spacingCompensation)
    {
        _AbilityActivation = buttonData;
        _IconPointer.transform.localEulerAngles = new Vector3(0, 0, _IconPointer.transform.localEulerAngles.z + spacingCompensation);
    }

    public void SetUpDialIcon(int buttonCount, int index)
    {
        GameObject imageObj = _IconPointer.transform.GetChild(0).gameObject;
        if (buttonCount < 2) imageObj.SetActive(false);
        else imageObj.SetActive(true);
        _IconPointer.transform.localEulerAngles = new Vector3(0, 0, 180/(float)buttonCount * -1);
        imageObj.GetComponent<Image>().sprite = _AbilityActivation._AbilitySprite;
    }
}

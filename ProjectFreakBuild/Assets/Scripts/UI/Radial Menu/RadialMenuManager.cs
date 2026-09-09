using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RadialMenuManager : MonoBehaviour
{
    [SerializeField] GameObject _DialPrefab;
    [SerializeField, Range(1,12)] int _DialButtonCount;
    [SerializeField, Range(0, 180)] float _DialButtonSpacing;
    [SerializeField] GameObject _radialButtonHolder;
    [SerializeField] float _minimumSelectionDistance;
    [SerializeField] ColorPaletteSO _colorPalette;

    [Header("RuntimeData")]
    [SerializeField] List<GameObject> _SpawnedButtons;
    [SerializeField] int _ButtonCount;
    [SerializeField] Vector2 _SelectionVector;
    [SerializeField] GameObject _LastSelectedButton;

    //local variables
    private PlayerInput pInput;

    private void OnEnable()
    {
        enableRadial();
    }
    private void OnDisable()
    {
        DisableRadial();
    }

    #region Initialize
    private void enableRadial()
    {
        pInput = new PlayerInput();
        pInput.Enable();
        _SelectionVector = Vector2.zero;

        pInput.Player.Look.performed += GetControllerVector;
        pInput.Player.Look.canceled += GetControllerVector;

        pInput.Player.Point.performed += GetMouseVector;
        pInput.Player.Point.canceled += GetMouseVector;
    }

    private void DisableRadial()
    {
        pInput.Player.Look.performed -= GetControllerVector;
        pInput.Player.Look.canceled -= GetControllerVector;

        pInput.Player.Point.performed -= GetMouseVector;
        pInput.Player.Point.canceled -= GetMouseVector;
        pInput.Disable();
    }

    #endregion

    #region Selection

    void GetControllerVector(InputAction.CallbackContext context)
    {
        _SelectionVector = context.ReadValue<Vector2>();
        DebugSelectionVector(_SelectionVector);
        print("beep");
        CalculateBottonVector();
    }

    void GetMouseVector(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = context.ReadValue<Vector2>();

        Vector2 centerPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            _radialButtonHolder.transform.position
        );

        _SelectionVector = (mousePosition - centerPosition).normalized;
        DebugSelectionVector(_SelectionVector);
        print("boop");
        CalculateBottonVector();
    }

    GameObject GetButtonFromVector(Vector2 inputVector)
    {
        if (Vector2.Distance(Vector2.zero, _SelectionVector) < _minimumSelectionDistance) return null;
        float angle = Mathf.Atan2(inputVector.y, inputVector.x) * Mathf.Rad2Deg;
        float anglePerButton = 360 / (float)_ButtonCount;

        angle -= (90 - (anglePerButton));
        if (angle < 0) angle += 360f;

        int buttonIndex = Mathf.FloorToInt(angle / anglePerButton);

        return _SpawnedButtons[buttonIndex];
    }

    void CalculateBottonVector()
    {
        if (_SpawnedButtons.Count == 0) return;
        GameObject selectedButton = GetButtonFromVector(_SelectionVector);
        if (selectedButton == null)
        {
            //print("No selection");
            if (_LastSelectedButton != null)
            {
                print($"{_LastSelectedButton.name} will deactivate...");
                _LastSelectedButton.GetComponent<RadialButton>().OnDeselction(_colorPalette);
                _LastSelectedButton = null;
            }
            return;
        }
        if (_LastSelectedButton != selectedButton)
        {
            if (_LastSelectedButton != null)
            {
                print($"{_LastSelectedButton.name} will deactivate...");
                _LastSelectedButton.GetComponent<RadialButton>().OnDeselction(_colorPalette);
            }
            print(selectedButton.name + " will now activate");
            selectedButton.GetComponent<RadialButton>().OnSelection(_colorPalette);
            _LastSelectedButton = selectedButton;
            return;
        }
        
    }

    #endregion

    #region Button Build
    public void UpdateRadialButtonSetUp(int ButtonCount)
    {
        if (_SpawnedButtons == null) _SpawnedButtons = new List<GameObject>();
        _ButtonCount = ButtonCount;
        if (_SpawnedButtons.Count > ButtonCount) RemoveExtraButtons(ButtonCount);

        UpdateOldRadialButtons(_SpawnedButtons.Count);

        for (int i = _SpawnedButtons.Count; i < ButtonCount; i++)
        {
            _SpawnedButtons.Add(Instantiate(_DialPrefab, transform.position, Quaternion.identity, _radialButtonHolder.transform));
            _SpawnedButtons[i].name += $" : {i}";
            SetUpRadialDialButton(i);
        }
        
        CalculateBottonVector();
    }

    void RemoveExtraButtons(int ButtonCount)
    {
        for (int i = _SpawnedButtons.Count-1; i >= ButtonCount; i--)
        {
            Destroy(_SpawnedButtons[i]);
            _SpawnedButtons.RemoveAt(i);
        }
    }

    void UpdateOldRadialButtons(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SetUpRadialDialButton(i);
        }
    }

    void SetUpRadialDialButton(int index)
    {
        float tempSpacing = _DialButtonSpacing;
        if (_ButtonCount < 2) tempSpacing = 0;
        float angle = index * 360 / _ButtonCount - tempSpacing / 2;
        Vector3 radialPArtEulerAngle = new Vector3(0, 0, angle);

        _SpawnedButtons[index].transform.localEulerAngles = radialPArtEulerAngle;
        _SpawnedButtons[index].GetComponent<Image>().fillAmount = 1 / (float)_ButtonCount - (tempSpacing / 360);
        _SpawnedButtons[index].GetComponent<Image>().color = _colorPalette._PrimaryColor;
    }

    #endregion

    void DebugSelectionVector(Vector2 inputVector)
    {
        Vector3 start = _radialButtonHolder.transform.position;

        Vector3 direction = new Vector3(inputVector.x, inputVector.y, 0f);

        Debug.DrawLine(
            start,
            start + direction * 200f,
            Color.red
        );
    }
}

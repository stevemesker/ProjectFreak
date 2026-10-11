using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Sirenix.OdinInspector;

//where the player is pointing on the 3D rune field. The field, the camera and the canvas all go through this one script
//mouse: the pointer is the mouse. Gamepad: the pointer is a reticle on the canvas
//  centered: the reticle sits in the middle of the screen, faded, and the left stick moves the camera under it
//  free: the right stick moves the reticle around the screen, solid. After a while of left stick only, it goes back to the middle
//on gamepad the reticle also drives a virtual mouse, so the canvas (inventory list, popup buttons) works with A like a mouse click
//RuneField3DObject adds this to itself if it isn't there, so there's nothing to set up
public class RuneFieldPointerObject : MonoBehaviour
{
    public static RuneFieldPointerObject _Pointer; //the pointer in this scene (one per scene)

    const string VirtualMouseName = "RuneFieldVirtualMouse";
    const float ReferenceScreenHeight = 1080f; //the reticle size is set for this screen height, then scaled so it looks the same on any screen

    [Header("Settings")]
    [Tooltip("How fast the right stick moves the reticle at full tilt, in screen heights per second")]
    [SerializeField, Min(0f)] float _ReticleSpeed = 0.9f;

    [Tooltip("How stick tilt turns into reticle speed. X = how far the stick is pushed (0 to 1), Y = how much of Reticle Speed is used. A curve that starts flat makes small pushes precise")]
    [SerializeField] AnimationCurve _ReticleResponse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Stick pushes smaller than this are ignored (0 to 1)")]
    [SerializeField, Range(0f, 0.9f)] float _StickDeadzone = 0.15f;

    [Tooltip("Seconds of moving with the left stick (and not touching the right stick) before the reticle goes back to the middle")]
    [SerializeField, Min(0f)] float _RecenterDelay = 1.5f;

    [Tooltip("How see-through the reticle is while it sits in the middle (0 = invisible, 1 = solid)")]
    [SerializeField, Range(0f, 1f)] float _CenteredAlpha = 0.35f;

    [Tooltip("Reticle size on screen, in pixels at 1080p")]
    [SerializeField, Min(1f)] float _ReticleSize = 28f;

    [Tooltip("Reticle color")]
    [SerializeField] Color _ReticleColor = Color.white;

    [Tooltip("Reticle image. Left empty, it's a plain square")]
    [SerializeField] Sprite _ReticleSprite;

    [Header("References")]
    [Tooltip("The camera looking at the field. Left empty, the main camera is used")]
    [SerializeField] Camera _Camera;

    [Tooltip("The canvas the reticle is drawn on. Left empty, the first canvas found in the scene is used")]
    [SerializeField] Canvas _Canvas;

    [Header("Runtime Data")]
    [Tooltip("True while the mouse is the pointer, false after a gamepad was used (read only)")]
    [SerializeField, ReadOnly] bool _UsingMouse = true;

    [Tooltip("True while the right stick has moved the reticle away from the middle (read only)")]
    [SerializeField, ReadOnly] bool _ReticleFree;

    //local variables
    PlayerInput _input;
    PointerEventData _uiPointer; //reused for asking the canvas what's under the pointer
    EventSystem _uiPointerSystem; //the event system _uiPointer was made for, so a new one is made if the scene's event system changes
    List<RaycastResult> _uiHits = new List<RaycastResult>();
    int _uiCheckFrame = -1; //the frame the canvas was last asked, so it's only asked once a frame
    bool _overCanvas;
    bool _overInventory;
    Vector2 _reticlePosition; //screen position of the reticle
    float _leftStickOnlyTime; //how long the left stick has been used without the right stick
    RectTransform _reticle;
    Image _reticleImage;
    Mouse _virtualMouse; //a pretend mouse the reticle drives, so the canvas reacts to it
    bool _virtualButtonDown;

    private void Awake()
    {
        //scene singleton: a second pointer in the same scene is a mistake
        if (_Pointer != null && _Pointer != this && _Pointer.gameObject.scene == gameObject.scene)
        {
            Debug.LogError($"Error! Two rune field pointers in {gameObject.scene.name}, removing the one on {gameObject.name}", this);
            Destroy(this);
            return;
        }
        _Pointer = this;
        _input = new PlayerInput();
        if (_Camera == null) _Camera = Camera.main;
        _reticlePosition = GetScreenCenter();
    }

    private void OnEnable()
    {
        if (_input != null) _input.RuneField.Enable();
    }

    private void OnDisable()
    {
        if (_input != null) _input.RuneField.Disable();
        ReleaseVirtualButton();
    }

    private void Start()
    {
        //builds the reticle on the canvas. A one-time search for the canvas if none is assigned
        WarnAboutOldInputModule();
        if (_Canvas == null) _Canvas = FindObjectOfType<Canvas>();
        if (_Canvas == null) { Debug.LogWarning($"Warning! No canvas found for {gameObject.name}, the gamepad reticle won't show...", this); return; }
        BuildReticle();
        ShowReticle();
    }

    private void OnDestroy()
    {
        //removes the virtual mouse so it doesn't stay plugged into the input system after the scene closes
        if (_virtualMouse != null && _virtualMouse.added) InputSystem.RemoveDevice(_virtualMouse);
        _virtualMouse = null;
        if (_Pointer == this) _Pointer = null;
    }

    private void Update()
    {
        CheckDeviceUse();
        if (_UsingMouse == false) UpdateReticle(Time.unscaledDeltaTime); //unscaled, so it works while the game is paused under the menu
        ShowReticle();
    }

    #region Pointer
    public Vector2 GetScreenPosition()
    {
        //the mouse position, or the reticle on gamepad
        if (_UsingMouse)
        {
            Mouse mouse = GetRealMouse();
            if (mouse != null) return mouse.position.ReadValue();
        }
        return _reticlePosition;
    }

    public bool IsUsingMouse()
    {
        return _UsingMouse;
    }

    public Camera GetCamera()
    {
        return _Camera;
    }

    void CheckDeviceUse()
    {
        //remembers whether the mouse or a gamepad was used last. The virtual mouse doesn't count, only a real one
        Mouse mouse = GetRealMouse();
        if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.01f || mouse.scroll.ReadValue().sqrMagnitude > 0f || mouse.leftButton.isPressed || mouse.middleButton.isPressed))
        {
            if (_UsingMouse == false) ReleaseVirtualButton();
            _UsingMouse = true;
            return;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;
        bool used = gamepad.leftStick.ReadValue().magnitude > _StickDeadzone || gamepad.rightStick.ReadValue().magnitude > _StickDeadzone
            || gamepad.leftTrigger.ReadValue() > 0.1f || gamepad.rightTrigger.ReadValue() > 0.1f
            || gamepad.buttonSouth.isPressed || gamepad.buttonWest.isPressed || gamepad.buttonEast.isPressed
            || gamepad.dpad.ReadValue().sqrMagnitude > 0.1f; //the D-pad counts too, so D-pad navigation in the side panel doesn't drop back to mouse mode
        if (used == false) return;

        if (_UsingMouse)
        {
            _reticlePosition = GetScreenCenter(); //switching to gamepad starts the reticle in the middle
            _ReticleFree = false;
        }
        _UsingMouse = false;
    }

    Mouse GetRealMouse()
    {
        //the first mouse that isn't the reticle's virtual one. Mouse.current can't be used, it switches to the virtual mouse when the reticle moves
        for (int i = 0; i < InputSystem.devices.Count; i++)
        {
            if (InputSystem.devices[i] is Mouse mouse && mouse != _virtualMouse) return mouse;
        }
        return null;
    }

    Vector2 GetScreenCenter()
    {
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }
    #endregion

    #region Reticle
    void UpdateReticle(float deltaTime)
    {
        //moves the reticle with the right stick, sends it back to the middle after a while of left stick only, and drives the virtual mouse
        Vector2 rightStick = _input.RuneField.Reticle.ReadValue<Vector2>();
        Vector2 leftStick = _input.RuneField.Move.ReadValue<Vector2>();
        bool aiming = rightStick.magnitude > _StickDeadzone;

        if (aiming)
        {
            _ReticleFree = true;
            _leftStickOnlyTime = 0f;
            float push = Mathf.InverseLerp(_StickDeadzone, 1f, Mathf.Min(rightStick.magnitude, 1f)); //0 at the deadzone, 1 at full tilt
            float speed = _ReticleResponse.Evaluate(push) * _ReticleSpeed * Screen.height; //screen heights per second, so it feels the same at any resolution
            _reticlePosition += rightStick.normalized * speed * deltaTime;
            _reticlePosition.x = Mathf.Clamp(_reticlePosition.x, 0f, Screen.width);
            _reticlePosition.y = Mathf.Clamp(_reticlePosition.y, 0f, Screen.height);
        }
        else if (_ReticleFree && leftStick.magnitude > _StickDeadzone)
        {
            _leftStickOnlyTime += deltaTime;
            if (_leftStickOnlyTime >= _RecenterDelay) _ReticleFree = false;
        }

        if (_ReticleFree == false) _reticlePosition = GetScreenCenter(); //centered mode stays in the middle, even if the screen size changes

        UpdateVirtualMouse();
    }

    void UpdateVirtualMouse()
    {
        //moves the virtual mouse to the reticle, and holds its left button while A is held on the canvas (so the inventory list can be dragged from and popup buttons pressed)
        //InputState.Change writes straight into a device's state, which is how a pretend device gets moved
        if (_virtualMouse == null) _virtualMouse = InputSystem.AddDevice<Mouse>(VirtualMouseName);

        Vector2 oldPosition = _virtualMouse.position.ReadValue();
        InputState.Change(_virtualMouse.position, _reticlePosition);
        InputState.Change(_virtualMouse.delta, _reticlePosition - oldPosition);

        Gamepad gamepad = Gamepad.current;
        bool aHeld = gamepad != null && gamepad.buttonSouth.isPressed;
        bool aPressed = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;

        bool fieldBusy = RuneField3DObject._Field3D != null && (RuneField3DObject._Field3D.IsPanelFocused() || RuneField3DObject._Field3D.IsCarryingFromInventory()); //the panel's own gamepad focus handles A then
        if (_virtualButtonDown == false && aPressed && fieldBusy == false && IsOverCanvas()) SetVirtualButton(true); //only presses on the canvas, A on the field grabs runes instead
        else if (_virtualButtonDown && aHeld == false) SetVirtualButton(false);
    }

    void SetVirtualButton(bool down)
    {
        //presses or releases the virtual mouse's left button
        if (_virtualMouse == null) return;
        _virtualButtonDown = down;
        _virtualMouse.CopyState<MouseState>(out MouseState state); //reads the mouse's whole state, changes one button, writes it back
        state = state.WithButton(MouseButton.Left, down);
        InputState.Change(_virtualMouse, state);
    }

    void ReleaseVirtualButton()
    {
        //lets go of the virtual button if it's held (switching to the mouse, or disabling)
        if (_virtualButtonDown) SetVirtualButton(false);
    }

    public bool IsVirtualButtonDown()
    {
        //true while A is holding the virtual mouse button on the canvas (dragging from the inventory list). The field leaves A alone then
        return _virtualButtonDown;
    }

    void BuildReticle()
    {
        //makes the reticle image on the canvas, drawn on top of everything, ignored by clicks
        GameObject reticleObject = new GameObject("Rune Field Reticle", typeof(RectTransform));
        reticleObject.transform.SetParent(_Canvas.transform, false);
        _reticle = reticleObject.GetComponent<RectTransform>();
        _reticle.sizeDelta = Vector2.one * _ReticleSize;

        _reticleImage = reticleObject.AddComponent<Image>();
        _reticleImage.sprite = _ReticleSprite;
        _reticleImage.raycastTarget = false; //so the canvas never sees the reticle as something under the pointer
    }

    void ShowReticle()
    {
        //shows the reticle on gamepad (faded while centered), hides it on mouse
        if (_reticle == null) return;
        bool panelFocus = RuneField3DObject._Field3D != null && RuneField3DObject._Field3D.IsPanelFocused();
        bool show = _UsingMouse == false && panelFocus == false; //hidden while the side panel has gamepad focus, the selected button shows where you are
        if (_reticle.gameObject.activeSelf != show) _reticle.gameObject.SetActive(show);
        if (show == false) return;

        _reticle.SetAsLastSibling(); //on top of the popup and everything else
        float scale = _Canvas != null ? _Canvas.scaleFactor : 1f;
        _reticle.sizeDelta = Vector2.one * _ReticleSize * (Screen.height / ReferenceScreenHeight) / Mathf.Max(scale, 0.0001f);
        _reticle.position = _reticlePosition; //an overlay canvas uses screen pixels as positions
        Color color = _ReticleColor;
        color.a *= _ReticleFree ? 1f : _CenteredAlpha;
        _reticleImage.color = color;
    }
    #endregion

    #region Setup Checks
    void WarnAboutOldInputModule()
    {
        //the old StandaloneInputModule can't see the new Input System (no virtual mouse, no proper gamepad navigation). Says how to fix it
        if (EventSystem.current == null) return;
        if (EventSystem.current.GetComponent<StandaloneInputModule>() == null) return;
        Debug.LogWarning($"Warning! The EventSystem uses the old Standalone Input Module, so gamepad UI won't work. Select the EventSystem and press 'Replace with InputSystemUIInputModule'...", EventSystem.current);
    }
    #endregion

    #region Field
    public bool TryGetFieldPoint(out Vector3 point)
    {
        //where the pointer lands on the field's plane, in world space. False if there's no field or it misses (pointing at the sky)
        return TryScreenToField(GetScreenPosition(), out point);
    }

    public bool TryScreenToField(Vector2 screenPosition, out Vector3 point)
    {
        //where any screen position lands on the field's plane. Plane is math only, so there's no collider to set up
        point = Vector3.zero;
        if (_Camera == null || RuneField3DObject._Field3D == null) return false;

        Transform field = RuneField3DObject._Field3D.transform;
        Plane plane = new Plane(field.up, field.position);
        Ray ray = _Camera.ScreenPointToRay(screenPosition);
        if (plane.Raycast(ray, out float distance) == false || distance <= 0f) return false;

        point = ray.GetPoint(distance);
        return true;
    }
    #endregion

    #region Canvas
    public bool IsOverCanvas()
    {
        //true while the pointer is over anything on the canvas, so clicks there don't grab runes or pan
        CheckCanvas();
        return _overCanvas;
    }

    public bool IsOverInventoryList()
    {
        //true while the pointer is over the inventory list (anything under an IngredientListWindow), where dropping a rune takes it off the field
        CheckCanvas();
        return _overInventory;
    }

    void CheckCanvas()
    {
        //asks the canvas what's under the pointer, once a frame. Uses the pointer's position, so it works for the mouse and the reticle
        if (_uiCheckFrame == Time.frameCount) return;
        _uiCheckFrame = Time.frameCount;
        _overCanvas = false;
        _overInventory = false;
        if (EventSystem.current == null) return;

        if (_uiPointer == null || _uiPointerSystem != EventSystem.current)
        {
            _uiPointer = new PointerEventData(EventSystem.current);
            _uiPointerSystem = EventSystem.current;
        }
        _uiPointer.position = GetScreenPosition();
        _uiHits.Clear();
        EventSystem.current.RaycastAll(_uiPointer, _uiHits); //every UI object under the pointer

        _overCanvas = _uiHits.Count > 0;
        for (int i = 0; i < _uiHits.Count; i++)
        {
            if (_uiHits[i].gameObject.GetComponentInParent<IngredientListWindow>() != null) { _overInventory = true; break; }
        }
    }
    #endregion
}

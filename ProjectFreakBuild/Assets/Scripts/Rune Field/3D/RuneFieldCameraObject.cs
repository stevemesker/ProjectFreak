using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

//the 3D rune field's camera. Sits on the focus target (the spot on the field the camera follows)
//it moves the focus (WASD / stick, grab-drag with slide, zoom toward the cursor, edge limit) and puts the camera on the zoom curve from SO_RuneField_Camera
//no Cinemachine: this script places the camera itself. All the feel values live on the camera settings asset so play mode tweaks stick
public class RuneFieldCameraObject : MonoBehaviour
{
    const float WindowsScrollNotch = 120f; //Windows reports one scroll notch as 120, Linux (Steam Deck) as 1
    const float ZoomChangeThreshold = 0.00001f; //zoom changes smaller than this don't count (stops tiny float jitter moving the focus)
    const float StopSpeed = 0.05f; //slides slower than this (meters per second) just stop
    const float DragVelocitySmoothing = 20f; //how quickly the tracked drag speed follows the mouse. Higher = follows the last few frames more closely
    const int GizmoCurveSteps = 64; //how many straight pieces the curve gizmo is drawn with

    [Header("Data")]
    [Tooltip("The camera settings asset: zoom curve, zoom feel, move speeds. Tune it while playing, the changes stay")]
    [SerializeField] RuneFieldCameraSO _CameraSettings;

    [Tooltip("The shared rune field settings, for the field's edge. Left empty, it's taken from the 3D field or the Shade Manager")]
    [SerializeField] RuneFieldSettingsSO _FieldSettings;

    [Header("References")]
    [Tooltip("The camera this script moves. Left empty, the scene's main camera is used")]
    [SerializeField] Camera _Camera;

    [Tooltip("The field's center (0, 0) and its flat plane. Its up direction is the field's up. Left empty, the scene's 3D field (RuneField3DObject) is used, or the world origin if there isn't one")]
    [SerializeField] Transform _FieldCenter;

    [Header("Runtime Data")]
    [Tooltip("Where the camera is on the zoom curve right now (0 to 1)")]
    [SerializeField, ReadOnly] float _CurrentZoom;

    [Tooltip("Where the zoom is heading (0 to 1). The camera catches up over Zoom Smooth Time")]
    [SerializeField, ReadOnly] float _TargetZoom;

    [FoldoutGroup("Tools")]
    [Tooltip("Edit mode: moves the camera along the zoom curve to check framing without pressing play")]
    [SerializeField, Range(0f, 1f), OnValueChanged("PreviewZoom")] float _PreviewZoom = 0.8f;

    [FoldoutGroup("Tools")]
    [Tooltip("Shows zoom, height and pitch in the corner of the game view, for tuning")]
    [SerializeField] bool _ShowDebugReadout = false;

    [FoldoutGroup("Gizmos")]
    [Tooltip("Draw the zoom curve gizmos even when this object isn't selected, so the curve stays visible while editing SO_RuneField_Camera")]
    [SerializeField] bool _AlwaysShowGizmos = true;

    [FoldoutGroup("Gizmos")]
    [Tooltip("The camera's vertical line")]
    [SerializeField] Color _LineColor = new Color(0.3f, 1f, 0.3f, 1f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("The zoom layers")]
    [SerializeField] Color _LayerColor = new Color(0.8f, 0.3f, 1f, 1f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("The part of the curve the camera can use (inside Zoom Range)")]
    [SerializeField] Color _CurveColor = new Color(1f, 0.85f, 0.2f, 1f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("The part of the curve outside Zoom Range")]
    [SerializeField] Color _UnusedCurveColor = new Color(1f, 0.85f, 0.2f, 0.25f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("The camera's current spot and the point it looks at")]
    [SerializeField] Color _CameraSpotColor = new Color(1f, 0.3f, 0.3f, 1f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("The edge the focus can't pass (outer ring plus edge bleed)")]
    [SerializeField] Color _EdgeColor = new Color(1f, 1f, 1f, 0.35f);

    //local variables
    PlayerInput _input;
    float _zoomVelocity; //used by SmoothDamp to remember how fast the zoom was moving
    Vector3 _moveVelocity; //WASD / stick movement, ramped
    Vector3 _slideVelocity; //the slide after letting go of a fast drag
    Vector3 _dragVelocity; //how fast the focus is moving while dragging, so letting go can start a slide
    InputAction _dragAction; //the button holding the current grab-drag (left or middle mouse), null when not dragging
    Vector3 _grabPoint; //the spot on the field that stays under the cursor while dragging
    float _moveHeldTime; //how long a move direction has been held, for the hold boost
    bool _usingMouse = true; //which device was used last (from the scene's pointer when there is one)
    bool _warnedNoFieldSettings;

    private void Reset()
    {
        //runs when the component is first added: fills in what it can find so there's nothing to drag in by hand
        _Camera = Camera.main;
#if UNITY_EDITOR
        _FieldSettings = RuneFieldSettingsSO.FindInProject();
        _CameraSettings = FindCameraSettings();
#endif
    }

    private void Awake()
    {
        _input = new PlayerInput();
        if (_Camera == null)
        {
            _Camera = Camera.main;
            if (_Camera != null) Debug.LogWarning($"Warning! No camera assigned on {gameObject.name}, using the main camera...", this);
        }
    }

    private void OnEnable()
    {
        _input.RuneField.Enable();
    }

    private void OnDisable()
    {
        _input.RuneField.Disable();
        _dragAction = null;
    }

    private void Start()
    {
        //checks the references once, then puts the camera at its starting zoom over the field's center plane
        if (_CameraSettings == null) { Debug.LogError($"Error! Camera settings not assigned on {gameObject.name}", this); enabled = false; return; }
        if (_Camera == null) { Debug.LogError($"Error! No camera found for {gameObject.name}", this); enabled = false; return; }
        if (_FieldCenter == null && RuneField3DObject._Field3D != null) _FieldCenter = RuneField3DObject._Field3D.transform; //every Awake has run by Start, so the field's singleton is set
        if (_FieldSettings == null && ShadeManager._ShadeManager != null) _FieldSettings = ShadeManager._ShadeManager.GetRuneFieldSettings();

        transform.position = ProjectOntoField(transform.position);
        _CurrentZoom = _CameraSettings.GetStartZoom();
        _TargetZoom = _CurrentZoom;
        PlaceCamera(_CurrentZoom);
    }

    private void LateUpdate()
    {
        //LateUpdate so anything else that moves this frame is done before the camera is placed
        //unscaled time, so the camera still works while the game is paused under the menu
        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime <= 0f) return;

        CheckDeviceUse();
        Vector2 pointer = GetPointerPosition();

        UpdateZoom(pointer, deltaTime);
        UpdateDrag(pointer, deltaTime);
        UpdateMove(deltaTime);
        UpdateSlide(deltaTime);
        ClampToEdge();
        PlaceCamera(_CurrentZoom);
    }

    #region Zoom
    public float GetZoom()
    {
        //where the camera is on the zoom curve right now (0 to 1). The labels use it
        return _CurrentZoom;
    }

    void UpdateZoom(Vector2 pointer, float deltaTime)
    {
        //reads zoom input into the target zoom, eases the current zoom toward it, and keeps the spot under the cursor in place
        float scroll = _input.RuneField.ZoomScroll.ReadValue<float>();
        if (scroll != 0f)
        {
            //scrolling up zooms in (toward 0). Each notch is one Scroll Step, and quick notches stack
            float notches = Mathf.Abs(scroll) >= WindowsScrollNotch ? Mathf.Abs(scroll) / WindowsScrollNotch : Mathf.Max(1f, Mathf.Abs(scroll));
            _TargetZoom -= Mathf.Sign(scroll) * notches * _CameraSettings.GetScrollStep();
        }

        float held = _input.RuneField.Zoom.ReadValue<float>(); //positive = zoom in (right trigger, ])
        if (held != 0f) _TargetZoom -= Mathf.Sign(held) * _CameraSettings.GetHeldZoomSpeed(Mathf.Abs(held)) * deltaTime;

        _TargetZoom = _CameraSettings.ClampZoom(_TargetZoom);

        //works out the field spot under the pointer before the zoom moves (the camera is still where last frame put it)
        bool towardPointer = _CameraSettings.GetZoomTowardCursor();
        bool hadSpot = TryGetFieldPoint(pointer, out Vector3 spotBefore);

        float oldZoom = _CurrentZoom;
        float smoothTime = _CameraSettings.GetZoomSmoothTime();
        if (smoothTime <= 0f) _CurrentZoom = _TargetZoom;
        else _CurrentZoom = Mathf.SmoothDamp(_CurrentZoom, _TargetZoom, ref _zoomVelocity, smoothTime, Mathf.Infinity, deltaTime); //SmoothDamp eases toward a target without overshooting
        _CurrentZoom = _CameraSettings.ClampZoom(_CurrentZoom); //in case Zoom Range was changed while playing

        if (Mathf.Abs(_CurrentZoom - oldZoom) < ZoomChangeThreshold) return;
        if (towardPointer == false || hadSpot == false) return;

        //puts the camera at the new zoom, then moves the focus so the same field spot is back under the pointer
        PlaceCamera(_CurrentZoom);
        if (TryGetFieldPoint(pointer, out Vector3 spotAfter)) MoveFocus(spotBefore - spotAfter);
    }
    #endregion

    #region Moving
    void UpdateDrag(Vector2 pointer, float deltaTime)
    {
        //grab-drag panning: the field spot under the cursor at the click stays under the cursor
        PlaceCamera(_CurrentZoom); //the camera has to be where the focus says (zooming may have moved the focus) before asking what's under the cursor

        if (_dragAction == null) TryStartDrag(pointer);
        if (_dragAction == null) return;

        if (_dragAction.IsPressed() == false)
        {
            EndDrag();
            return;
        }

        if (TryGetFieldPoint(pointer, out Vector3 spotNow) == false) return;

        Vector3 change = _grabPoint - spotNow;
        MoveFocus(change);

        //tracks how fast the drag is going, smoothed over the last few frames. Exp gives the same smoothing at any frame rate
        float follow = 1f - Mathf.Exp(-DragVelocitySmoothing * deltaTime);
        _dragVelocity = Vector3.Lerp(_dragVelocity, change / deltaTime, follow);
    }

    void TryStartDrag(Vector2 pointer)
    {
        //starts a grab-drag on a middle click anywhere, or a left click on empty field (not over the canvas)
        InputAction dragPan = _input.RuneField.DragPan;
        InputAction select = _input.RuneField.Select;

        InputAction pressed = null;
        if (dragPan.WasPressedThisFrame()) pressed = dragPan;
        else if (select.WasPressedThisFrame() && IsMouse(select) && IsPointerOverCanvas() == false && IsPointerOnRune() == false) pressed = select; //a left click on a rune grabs the rune instead
        if (pressed == null) return;

        if (TryGetFieldPoint(pointer, out _grabPoint) == false) return;
        _dragAction = pressed;
        _dragVelocity = Vector3.zero;
        _slideVelocity = Vector3.zero; //a new drag stops any slide
    }

    void EndDrag()
    {
        //letting go: a fast enough drag keeps sliding
        _dragAction = null;
        float speed = _dragVelocity.magnitude;
        if (speed < _CameraSettings.GetMinSlideSpeed()) _slideVelocity = Vector3.zero;
        else _slideVelocity = Vector3.ClampMagnitude(_dragVelocity, _CameraSettings.GetMaxSlideSpeed());
        _dragVelocity = Vector3.zero;
    }

    void UpdateMove(float deltaTime)
    {
        //WASD / left stick moves the focus, relative to the way the camera faces, ramping up and down
        Vector2 input = Vector2.ClampMagnitude(_input.RuneField.Move.ReadValue<Vector2>(), 1f);
        if (RuneField3DObject._Field3D != null && RuneField3DObject._Field3D.IsPanelFocused()) input = Vector2.zero; //the stick is working the side panel
        bool moving = input.sqrMagnitude > 0.0001f;

        if (moving)
        {
            _slideVelocity = Vector3.zero; //moving plants the camera and stops any slide
            _moveHeldTime += deltaTime;
        }
        else _moveHeldTime = 0f;

        GetFacing(out Vector3 forward, out Vector3 right);
        float speed = _CameraSettings.GetMoveSpeed(_CurrentZoom) * _CameraSettings.GetBoost(_moveHeldTime);
        Vector3 wanted = (right * input.x + forward * input.y) * speed;

        float rampTime = _CameraSettings.GetMoveRampTime();
        if (rampTime <= 0f) _moveVelocity = wanted;
        else
        {
            float acceleration = Mathf.Max(speed, _moveVelocity.magnitude) / rampTime; //full speed in Ramp Time, and back to 0 in the same time
            _moveVelocity = Vector3.MoveTowards(_moveVelocity, wanted, acceleration * deltaTime);
        }

        if (_moveVelocity.sqrMagnitude > 0f) MoveFocus(_moveVelocity * deltaTime);
    }

    void UpdateSlide(float deltaTime)
    {
        //keeps a released drag sliding, slowing down by Slide Friction
        if (_dragAction != null) return;
        if (_slideVelocity.sqrMagnitude < StopSpeed * StopSpeed) { _slideVelocity = Vector3.zero; return; }

        MoveFocus(_slideVelocity * deltaTime);
        _slideVelocity *= Mathf.Exp(-_CameraSettings.GetSlideFriction() * deltaTime); //Exp makes the slow down the same at any frame rate
    }

    void MoveFocus(Vector3 change)
    {
        //moves the focus along the field, never up or down off it
        transform.position = ProjectOntoField(transform.position + change);
    }

    void ClampToEdge()
    {
        //keeps the focus inside the outer ring plus edge bleed, so the field's edge can't pass the middle of the screen
        float edge = GetEdgeRadius();
        if (edge <= 0f) return;

        Vector3 center = GetFieldCenter();
        Vector3 offset = transform.position - center;
        if (offset.magnitude <= edge) return;

        Vector3 outward = offset.normalized;
        transform.position = center + outward * edge;

        //takes away the part of any movement still pushing outward, so sliding along the edge works but pushing into it doesn't
        _slideVelocity -= outward * Mathf.Max(0f, Vector3.Dot(_slideVelocity, outward));
        _moveVelocity -= outward * Mathf.Max(0f, Vector3.Dot(_moveVelocity, outward));
    }
    #endregion

    #region Camera Placement
    void PlaceCamera(float zoom)
    {
        //puts the camera on the zoom curve around the focus and points it at the aim spot
        if (_Camera == null || _CameraSettings == null) return;
        GetCameraPose(transform.position, zoom, out Vector3 position, out Quaternion rotation, out _);
        _Camera.transform.SetPositionAndRotation(position, rotation);
    }

    void GetCameraPose(Vector3 focus, float zoom, out Vector3 position, out Quaternion rotation, out Vector3 aim)
    {
        //where the camera sits and how it's turned for this focus and zoom
        GetFacing(out Vector3 forward, out Vector3 right);
        Vector3 up = GetFieldUp();
        Vector2 spot = _CameraSettings.EvaluateZoom(zoom); //x = out from the target, y = height

        position = focus - forward * spot.x + up * spot.y;

        Vector3 aimOffset = _CameraSettings.GetAimOffset();
        aim = focus + right * aimOffset.x + up * aimOffset.y + forward * aimOffset.z;

        Vector3 look = aim - position;
        if (look.sqrMagnitude < 0.0001f) look = -up;

        //the camera's "up" comes from its fixed right side, so it never flips, even when it passes straight overhead
        Vector3 cameraUp = Vector3.Cross(look.normalized, right);
        rotation = Quaternion.LookRotation(look, cameraUp);
    }

    void GetFacing(out Vector3 forward, out Vector3 right)
    {
        //which way the camera faces along the field (from the camera's vertical line toward the target), and its right side
        //it comes from Follow Distance: X is sideways, Y is behind. Straight overhead (0, 0) faces the field's forward
        Vector3 up = GetFieldUp();
        Vector3 fieldForward = _FieldCenter != null ? _FieldCenter.forward : Vector3.forward;
        Vector3 fieldRight = _FieldCenter != null ? _FieldCenter.right : Vector3.right;
        Vector2 follow = _CameraSettings != null ? _CameraSettings.GetFollowDistance() : Vector2.zero;

        Vector3 outward = fieldRight * follow.x - fieldForward * follow.y; //from the target out to the vertical line
        forward = outward.sqrMagnitude > 0.0001f ? -outward.normalized : fieldForward;
        right = Vector3.Cross(up, forward).normalized; //Cross(up, forward) gives the right side in Unity
    }
    #endregion

    #region Field Plane
    Vector3 GetFieldCenter()
    {
        return _FieldCenter != null ? _FieldCenter.position : Vector3.zero;
    }

    Vector3 GetFieldUp()
    {
        return _FieldCenter != null ? _FieldCenter.up : Vector3.up;
    }

    Vector3 ProjectOntoField(Vector3 point)
    {
        //drops a point straight onto the field's plane
        return Vector3.ProjectOnPlane(point - GetFieldCenter(), GetFieldUp()) + GetFieldCenter();
    }

    bool TryGetFieldPoint(Vector2 screenPosition, out Vector3 point)
    {
        //where a screen position lands on the field's plane. False if it misses (pointing at the sky)
        //Plane is a flat surface that only exists in math, so there's no collider to set up
        point = Vector3.zero;
        if (_Camera == null) return false;

        Plane field = new Plane(GetFieldUp(), GetFieldCenter());
        Ray ray = _Camera.ScreenPointToRay(screenPosition);
        if (field.Raycast(ray, out float distance) == false || distance <= 0f) return false;

        point = ray.GetPoint(distance);
        return true;
    }

    float GetEdgeRadius()
    {
        //how far the focus can go from the center, in meters. 0 = no limit (no field settings found)
        if (_FieldSettings == null)
        {
            if (_warnedNoFieldSettings == false) Debug.LogWarning($"Warning! No rune field settings for {gameObject.name}, the camera has no edge limit...", this);
            _warnedNoFieldSettings = true;
            return 0f;
        }
        return _FieldSettings.ToMeters(_FieldSettings.GetFieldEdge() + _FieldSettings._EdgeBleed);
    }
    #endregion

    #region Pointer
    void CheckDeviceUse()
    {
        //remembers whether the mouse or a gamepad was used last. The scene's pointer does this when there is one, this is the fallback for a scene without a field
        if (RuneFieldPointerObject._Pointer != null) { _usingMouse = RuneFieldPointerObject._Pointer.IsUsingMouse(); return; }
        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.01f || mouse.scroll.ReadValue().sqrMagnitude > 0f || mouse.leftButton.isPressed || mouse.middleButton.isPressed))
        {
            _usingMouse = true;
            return;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && (gamepad.leftStick.ReadValue().sqrMagnitude > 0.04f || gamepad.leftTrigger.ReadValue() > 0.1f || gamepad.rightTrigger.ReadValue() > 0.1f)) _usingMouse = false;
    }

    Vector2 GetPointerPosition()
    {
        //the scene's pointer (mouse, or the middle of the screen on gamepad), with the same fallback for a scene without a field
        if (RuneFieldPointerObject._Pointer != null) return RuneFieldPointerObject._Pointer.GetScreenPosition();
        if (_usingMouse && Mouse.current != null) return _input.RuneField.Point.ReadValue<Vector2>();
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    bool IsMouse(InputAction action)
    {
        //true if the control that just triggered this action is on a mouse
        return action.activeControl != null && action.activeControl.device is Mouse;
    }

    bool IsPointerOverCanvas()
    {
        //true when the pointer is over any UI on the canvas, so clicks on the inventory don't pan the field
        if (RuneFieldPointerObject._Pointer != null) return RuneFieldPointerObject._Pointer.IsOverCanvas();
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    bool IsPointerOnRune()
    {
        //true when the pointer is on a rune (or dragging one), so a left click grabs the rune instead of panning
        return RuneField3DObject._Field3D != null && RuneField3DObject._Field3D.IsHoveringRune();
    }
    #endregion

    #region Tools
    void PreviewZoom()
    {
        //edit mode only: moves the camera to Preview Zoom around this object, to check framing without playing
        if (Application.isPlaying || _CameraSettings == null) return;
        if (_Camera == null) _Camera = Camera.main;
        if (_Camera == null) return;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(_Camera.transform, "Preview Rune Field Zoom"); //so Ctrl+Z puts the camera back
#endif
        GetCameraPose(transform.position, _CameraSettings.ClampZoom(_PreviewZoom), out Vector3 position, out Quaternion rotation, out _);
        _Camera.transform.SetPositionAndRotation(position, rotation);
    }

    [FoldoutGroup("Tools")]
    [Button("Preview Start Zoom")]
    void PreviewStartZoom()
    {
        //sets the preview to where the camera starts when the field opens
        if (_CameraSettings == null) return;
        _PreviewZoom = _CameraSettings.GetStartZoom();
        PreviewZoom();
    }

#if UNITY_EDITOR
    static RuneFieldCameraSO FindCameraSettings()
    {
        //editor only: finds the camera settings asset in the project, for Reset
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RuneFieldCameraSO");
        if (guids.Length == 0) return null;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<RuneFieldCameraSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
    }
#endif
    #endregion

    #region Debugging
    private void OnGUI()
    {
        //the tuning readout in the corner of the game view
        if (_ShowDebugReadout == false || _CameraSettings == null || _Camera == null) return;

        Vector2 spot = _CameraSettings.EvaluateZoom(_CurrentZoom);
        float pitch = Vector3.Angle(Vector3.ProjectOnPlane(_Camera.transform.forward, GetFieldUp()), _Camera.transform.forward); //how far the camera tilts down from level
        string text = $"Zoom {_CurrentZoom:0.000} (target {_TargetZoom:0.000})\nHeight {spot.y:0.00} m   Out {spot.x:0.00} m\nPitch {pitch:0.0}°   FOV {_Camera.fieldOfView:0}\nCurve length {_CameraSettings.GetCurveLength():0.00} m";
        GUI.Box(new Rect(10, 10, 260, 72), text);
    }

    private void OnDrawGizmos()
    {
        if (_AlwaysShowGizmos) DrawZoomGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (_AlwaysShowGizmos == false) DrawZoomGizmos();
    }

    void DrawZoomGizmos()
    {
        //draws the vertical line, the zoom layers, the curve (bright where the camera can go) and the camera's current spot
        if (_CameraSettings == null) return;

        Vector3 focus = transform.position;
        GetFacing(out Vector3 forward, out _);
        Vector3 up = GetFieldUp();
        Vector3 back = -forward;
        float lineOut = _CameraSettings.GetFollowDistance().magnitude;

        //the vertical line
        Gizmos.color = _LineColor;
        Gizmos.DrawLine(focus + back * lineOut, focus + back * lineOut + up * _CameraSettings.GetCameraHeight());

        //the zoom layers: a line from the vertical line out to each point
        Gizmos.color = _LayerColor;
        for (int i = 0; i < _CameraSettings.GetLayerCount(); i++)
        {
            Vector2 layer = _CameraSettings.GetLayerPoint(i);
            Vector3 point = focus + back * layer.x + up * layer.y;
            Gizmos.DrawLine(focus + back * lineOut + up * layer.y, point);
            Gizmos.DrawWireSphere(point, 0.15f);
        }

        //the curve, bright inside Zoom Range and faded outside
        Vector3 previous = Vector3.zero;
        for (int i = 0; i <= GizmoCurveSteps; i++)
        {
            float zoom = i / (float)GizmoCurveSteps;
            Vector2 spot = _CameraSettings.EvaluateZoom(zoom);
            Vector3 point = focus + back * spot.x + up * spot.y;
            if (i > 0)
            {
                bool usable = zoom >= _CameraSettings.GetMinZoom() && zoom <= _CameraSettings.GetMaxZoom();
                Gizmos.color = usable ? _CurveColor : _UnusedCurveColor;
                Gizmos.DrawLine(previous, point);
            }
            previous = point;
        }

        //the camera's spot (current zoom while playing, preview zoom in edit mode) and where it looks
        float shownZoom = Application.isPlaying ? _CurrentZoom : _CameraSettings.ClampZoom(_PreviewZoom);
        GetCameraPose(focus, shownZoom, out Vector3 cameraSpot, out _, out Vector3 aim);
        Gizmos.color = _CameraSpotColor;
        Gizmos.DrawSphere(cameraSpot, 0.25f);
        Gizmos.DrawLine(cameraSpot, aim);
        Gizmos.DrawWireSphere(aim, 0.15f);

        //the edge the focus can't pass (editor only, Handles can draw circles but don't exist in a build)
#if UNITY_EDITOR
        RuneFieldSettingsSO fieldSettings = _FieldSettings != null ? _FieldSettings : RuneFieldSettingsSO.FindInProject();
        if (fieldSettings == null) return;
        float edge = fieldSettings.ToMeters(fieldSettings.GetFieldEdge() + fieldSettings._EdgeBleed);
        UnityEditor.Handles.color = _EdgeColor;
        UnityEditor.Handles.DrawWireDisc(GetFieldCenter(), up, edge);
#endif
    }
    #endregion
}

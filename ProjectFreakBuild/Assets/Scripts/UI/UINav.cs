using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UINav : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("<=====Pointers=====>")]
    [SerializeField]
    private RectTransform ParentTransform;
    [SerializeField]
    private RectTransform target;

    [Header("<=====Panning=====>")]
    [SerializeField]
    private Vector3 currentOffset;
    [SerializeField, Tooltip("Haw far from the center of the screen can any corner of the dragable area be before it snaps back")]
    private float maxDragDistance;

    [Header("<====Zoom=====>")]
    [SerializeField]
    private float zoomSpeed = 0.1f;
    [SerializeField]
    private float minScale = 0.5f;
    [SerializeField]
    private float maxScale = 2f;

    [Header("<---Events--->")]
    [SerializeField] public UnityEvent ScaleEvent;

    //hidden variables
    private PlayerInput input;
    Vector3[] _corners = new Vector3[4]; //reused for GetWorldCorners so we don't make a new array every drag/zoom

    // Start is called before the first frame update
    void Start()
    {

    }
    private void Awake()
    {
        input = new PlayerInput();
        target = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (ParentTransform == null) { Debug.LogError($"Error! Parent transform not assigned on {gameObject.name}", this); return; }

        input.Enable();
        input.UI.ScrollWheel.performed += ZoomScrollWheel;
    }

    private void OnDisable()
    {
        //unsubscribes so toggling the map doesn't stack extra zoom listeners
        input.UI.ScrollWheel.performed -= ZoomScrollWheel;
        input.Disable();
    }

    #region Panning
    public void OnBeginDrag(PointerEventData eventData)
    {
        currentOffset = transform.position - Input.mousePosition;
        //transform.position = currentOffset;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //throw new System.NotImplementedException();
        transform.position = Input.mousePosition + currentOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        currentOffset = Vector3.zero;
        SnapToBounds();
    }

    void SnapToBounds()
    {
        //function that snaps the window back if an edge crossed the middle of the screen (plus the buffer)
        //everything is measured in the parent's local space so zoom, pivot and canvas scaling are all accounted for

        //GetWorldCorners gives the 4 corners after scale is applied: 0 = bottom left, 2 = top right
        target.GetWorldCorners(_corners);
        Vector3 bottomLeft = ParentTransform.InverseTransformPoint(_corners[0]);
        Vector3 topRight = ParentTransform.InverseTransformPoint(_corners[2]);

        Vector2 center = ParentTransform.rect.center; //middle of the screen in the parent's local space
        Vector3 offset = Vector3.zero; //used for snapping

        offset.x = GetSnapOffset(bottomLeft.x, topRight.x, center.x);
        offset.y = GetSnapOffset(bottomLeft.y, topRight.y, center.y);

        //offset is in parent space, TransformVector converts it to world space before moving
        target.position += ParentTransform.TransformVector(offset);
    }

    float GetSnapOffset(float minEdge, float maxEdge, float center)
    {
        //function that returns how far to move on one axis so both edges stay past the center buffer
        float lowLimit = center - maxDragDistance;  //left/bottom edge can't go further in than this
        float highLimit = center + maxDragDistance; //right/top edge can't go further in than this

        //window is too small to satisfy both sides (zoomed far out), so just center it
        if (maxEdge - minEdge < highLimit - lowLimit) return center - (minEdge + maxEdge) / 2;

        if (minEdge > lowLimit) return lowLimit - minEdge;
        if (maxEdge < highLimit) return highLimit - maxEdge;
        return 0;
    }
    #endregion

    #region zoom
    private void ZoomScrollWheel(InputAction.CallbackContext context)
    {
        Vector2 scroll = context.ReadValue<Vector2>();

        float zoomAmount = scroll.y * zoomSpeed;

        float oldScale = target.localScale.x;
        float newScale = Mathf.Clamp(oldScale + zoomAmount, minScale, maxScale);

        if (Mathf.Approximately(newScale, oldScale)) return; //already at the min/max, nothing to do
        if (oldScale <= 0) return; //can't zoom from 0 scale (would divide by 0 below)

        //the point we zoom toward is the middle of the screen, in world space
        Vector3 focusPoint = ParentTransform.TransformPoint(ParentTransform.rect.center);

        //move the window so the spot under the focus point is still under it after scaling
        //the distance from the focus point to the pivot grows/shrinks by the same ratio as the scale
        float ratio = newScale / oldScale;
        target.position = focusPoint + (target.position - focusPoint) * ratio;
        target.localScale = new Vector3(newScale, newScale, 1f);

        SnapToBounds(); //zooming out can pull an edge past the middle, so snap back here too
        ScaleEvent?.Invoke();
    }
    #endregion
}

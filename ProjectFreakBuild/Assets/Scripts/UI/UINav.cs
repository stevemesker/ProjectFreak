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
        input.Enable();
        input.UI.ScrollWheel.performed += ZoomScrollWheel;
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
        
        //throw new System.NotImplementedException();
        currentOffset = Vector3.zero;

        RectTransform rt = GetComponent<RectTransform>();
        Vector2 temp;
        temp.x = rt.rect.width;
        temp.y = rt.rect.height;

        Vector3 offset = Vector3.zero; //used for snapping

        //check max width distance
        if (rt.position.x > (ParentTransform.rect.width/2 + temp.x/2) - maxDragDistance)
        {
            offset.x = (ParentTransform.rect.width / 2 + temp.x / 2 - maxDragDistance) - rt.position.x;
        }
        if (rt.position.x < ParentTransform.rect.width / 2 - temp.x / 2 + maxDragDistance)
        {
            offset.x =  (ParentTransform.rect.width / 2 - temp.x / 2 + maxDragDistance) - rt.position.x;
        }

        //check max height distance
        if (rt.position.y > (ParentTransform.rect.height / 2 + temp.y / 2) - maxDragDistance)
        {
            offset.y = (ParentTransform.rect.height / 2 + temp.y / 2 - maxDragDistance) - rt.position.y;
        }
        if (rt.position.y < (ParentTransform.rect.height / 2 - temp.y / 2) + maxDragDistance)
        {
            offset.y = (ParentTransform.rect.height / 2 - temp.y / 2 + maxDragDistance) - rt.position.y;
        }

        rt.transform.position += offset;
    }
    #endregion

    #region zoom
    private void ZoomScrollWheel(InputAction.CallbackContext context)
    {
        Vector2 scroll = context.ReadValue<Vector2>();

        float zoomAmount = scroll.y * zoomSpeed;

        Vector3 newScale = target.localScale + Vector3.one * zoomAmount;

        // Clamp scale
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = 1f;

        target.localScale = newScale;
        ScaleEvent?.Invoke();
    }
    #endregion
}

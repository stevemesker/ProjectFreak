using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the line between two connected nodes on the dungeon map (a 2D canvas). It only draws the line: it sits on one end, points at the other and stretches to the distance
//it used to be the 2D rune field's bridge too (with tearing). The rune field is 3D now (Bridge3DObject), so that part was removed Oct 28, 2026
public class NodeBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The art that gets stretched and rotated between the two ends")]
    [SerializeField] GameObject artPointer;

    [Header("Runtime Data")]
    [Tooltip("Object at one end (read only)")]
    public GameObject connectionOne;
    [Tooltip("Object at the other end (read only)")]
    public GameObject connectionTwo;

    #region Initialize
    public void BuildConnection(GameObject X, GameObject Y)
    {
        //sets the two objects the line is drawn between
        connectionOne = X;
        connectionTwo = Y;
    }
    #endregion

    #region Drawing
    public void UpdatePosition(float length)
    {
        //move to position of first connected node
        transform.position = connectionOne.GetComponent<RectTransform>().transform.position;
        RectTransform rect = artPointer.GetComponent<RectTransform>();

        //find current angle rotation
        Vector2 direction = connectionTwo.transform.position - connectionOne.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //set angle and length of bridge
        rect.rotation = Quaternion.Euler(0, 0, angle-90f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, length);
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeBridge : MonoBehaviour
{
    [SerializeField] GameObject artPointer;
    public GameObject connectionOne;
    public GameObject connectionTwo;
    public void BuildConnection(GameObject X, GameObject Y)
    {
        connectionOne = X;
        connectionTwo = Y;
    }

    public void updatePosition(float length)
    {
        //transform.position = connectionOne.transform.position;
        transform.position = connectionOne.GetComponent<RectTransform>().transform.position;
        RectTransform rect = artPointer.GetComponent<RectTransform>();

        Vector2 direction = connectionTwo.transform.position - connectionOne.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle-90f);
        //rect.sizeDelta = new Vector2(rect.sizeDelta.x, length+(connectionOne.GetComponent<RectTransform>().rect.width/2) + (connectionTwo.GetComponent<RectTransform>().rect.width / 2));
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, length);
    }

    public void SeverConnection()
    {

    }
}

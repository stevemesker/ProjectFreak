using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInstanceOffset : MonoBehaviour
{
    //script that gives a target item an automatic offset when it is instanced
    [Tooltip("The object that will be the target for the offsetting amount, meaning this object will go to it's current location when spawned")]
    public GameObject OffsetTarget;
    [Tooltip("Extra Adjustment in case it is required fsr. Try to keep this at zero")]
    public Vector3 OffsetAdjustment;

    public void OffsetObject()
    {
        if (OffsetTarget != null) gameObject.transform.position = gameObject.transform.position - OffsetTarget.transform.localPosition + OffsetAdjustment;
    }
}

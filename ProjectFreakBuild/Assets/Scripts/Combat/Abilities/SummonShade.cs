using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonShade : MonoBehaviour
{
    public void AbilitySummonShade(GameObject source)
    {
        GameManager._GameManager.GetComponent<ShadeManager>().summonShade(source);
    }
}

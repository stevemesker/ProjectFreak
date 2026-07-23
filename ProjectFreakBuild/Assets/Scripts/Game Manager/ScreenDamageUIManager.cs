using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenDamageUIManager : MonoBehaviour
{
    [Header("Initialize Data")]
    public static ScreenDamageUIManager _UIdamage;

    [SerializeField, Tooltip("Current instance of the ui canvas")] public UIDamageCanvas _damageCanvas;

    private void Awake()
    {
        if (ScreenDamageUIManager._UIdamage == null) ScreenDamageUIManager._UIdamage = this;
        else
        {
            Destroy(gameObject);
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenDamageUIManager : MonoBehaviour
{
    //entry point for damage popups. Lives on a child of the Game Manager prefab (DamageCanvasUI), so the Game Manager's
    //duplicate check already removes any extra copy. See UIDamage Manager in the GDD

    [Header("Initialize Data")]
    public static ScreenDamageUIManager _UIdamage;

    [SerializeField, Tooltip("Current instance of the ui canvas")] public UIDamageCanvas _damageCanvas;

    private void Awake()
    {
        //only claims the singleton if it's free. A duplicate Game Manager still runs this Awake before it's destroyed at the end of the frame,
        //so this keeps the original from being replaced by the copy that's about to disappear
        if (_UIdamage == null) _UIdamage = this;
    }

    private void OnDestroy()
    {
        //clears the singleton if this was it, so nothing tries to use a destroyed manager
        if (_UIdamage == this) _UIdamage = null;
    }
}

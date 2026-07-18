using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[System.Serializable]
public class TimelineEventTrack
{
    [FoldoutGroup("Timing")]
    [MinValue(0)]
    public float StartDelay;

    [FoldoutGroup("Timing")]
    public bool WaitForContinue;

    [FoldoutGroup("Timing")]
    [HideIf(nameof(WaitForContinue))]
    [MinValue(0)]
    public float EndDelay;

    [FoldoutGroup("Event")]
    public UnityEvent Event;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class TimelineRunner : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("When true, event trigger launches automatically on enable")] 
    public bool _StartEventAutomatically;

    [Tooltip("When true, event triggers when object enters trigger volume")] 
    public bool _triggerActivation;

    [ShowIf(nameof(_triggerActivation))]
    [Tooltip("List of objects that will only trigger the event if they contain a tag in this list")] 
    public List<string> _triggerMask;

    [Header("Timeline")]
    [SerializeField]
    private List<TimelineEventTrack> timeline;

    [SerializeField]
    private UnityEvent onTimelineComplete;


    //Private Variables
    private Coroutine currentTimeline;
    private bool waitingForContinue;

    public bool IsRunning { get; private set; }

    #region Trigger Functions
    private void OnEnable()
    {
        if (_StartEventAutomatically) PlayTimeline();
    }

    private void OnDisable()
    {
        StopTimeline();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggerActivation == false) return;

        if (_triggerMask.Count > 0)
        {
            if (_triggerMask.Contains(other.gameObject.tag) == false) return;
        }

        PlayTimeline();
    }
    #endregion

    #region Timeline Functions
    public void PlayTimeline()
    {
        StopTimeline();

        currentTimeline = StartCoroutine(RunTimeline());
    }

    public void StopTimeline()
    {
        if (currentTimeline != null)
            StopCoroutine(currentTimeline);

        currentTimeline = null;
        waitingForContinue = false;
        IsRunning = false;
    }

    public void ContinueTimeline()
    {
        if (!IsRunning || !waitingForContinue)
            return;

        waitingForContinue = false;
    }

    private IEnumerator RunTimeline()
    {
        IsRunning = true;

        foreach (TimelineEventTrack track in timeline)
        {
            // Wait before starting this track
            if (track.StartDelay > 0)
                yield return new WaitForSeconds(track.StartDelay);

            // IMPORTANT:
            // Set this BEFORE invoking the event so the event
            // can immediately call ContinueTimeline().
            if (track.WaitForContinue)
                waitingForContinue = true;

            // Invoke the event
            track.Event?.Invoke();

            // Wait for completion
            if (track.WaitForContinue)
            {
                yield return new WaitUntil(() => waitingForContinue == false);
            }
            else if (track.EndDelay > 0)
            {
                yield return new WaitForSeconds(track.EndDelay);
            }
        }

        IsRunning = false;
        currentTimeline = null;

        onTimelineComplete?.Invoke();
    }
    #endregion
}
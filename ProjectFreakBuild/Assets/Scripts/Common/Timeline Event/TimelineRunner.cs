using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimelineRunner : MonoBehaviour
{
    [SerializeField]
    private List<TimelineEventTrack> timeline;

    [SerializeField]
    private UnityEvent onTimelineComplete;

    private Coroutine currentTimeline;

    private bool waitingForContinue;

    public bool IsRunning { get; private set; }

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
}
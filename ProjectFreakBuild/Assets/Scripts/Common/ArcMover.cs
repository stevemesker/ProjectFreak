using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ArcMover : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("describes half of the total arc")] private AnimationCurve _ArcCurveShape;
    [SerializeField] Vector3 _targetPosition;
    [SerializeField] float _Duration;

    [Tooltip("When object arrives at desired location, event will fire")]public UnityEvent finishEvent;

    //private variables
    Coroutine _currentArcRoutine;
    private Vector3 _startPosition;
    
    float _PeakY;

    public void LaunchTo(Vector3 destination, float arcHeight, float duration)
    {
        if (_currentArcRoutine != null) StopCoroutine(_currentArcRoutine);

        _startPosition = transform.position;
        _PeakY = Mathf.Max(_startPosition.y, destination.y) + arcHeight;
        _targetPosition = destination;
        _Duration = duration;
        enabled = true;
        _currentArcRoutine = StartCoroutine(ArcRoutine());
    }

    IEnumerator ArcRoutine()
    {
        float timeElapsed = 0f;

        while (timeElapsed < _Duration)
        {
            timeElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timeElapsed / _Duration);
            float curveT;
            float y;

            Vector3 horizontalPosition = Vector3.Lerp(_startPosition, _targetPosition, t);

            if (t < .5)
            {
                curveT = _ArcCurveShape.Evaluate(t * 2);
                y = Mathf.Lerp(
                    _startPosition.y,
                    _PeakY,
                    curveT);
            }
            else
            {
                curveT = _ArcCurveShape.Evaluate((1f-t) * 2f);
                y = Mathf.Lerp(
                    _targetPosition.y,
                    _PeakY, 
                    curveT);
            }
            

            transform.position = new Vector3(horizontalPosition.x,y, horizontalPosition.z);

            yield return new WaitForFixedUpdate();
        }

        transform.position = _targetPosition;
        finishEvent?.Invoke();

        _currentArcRoutine = null;

        enabled = false;
    }
}

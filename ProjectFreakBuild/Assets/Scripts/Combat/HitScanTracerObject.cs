using System.Collections;
using System.Collections.Generic;
using UnityEngine;

////////////////////////////////////////////////
///
/// Goes on a hit scan tracer prefab (with a LineRenderer). Draws a line from the muzzle to where the shot ended,
/// shrinks it over its life, then removes itself. See Ranged Weapon System > Hit Scan in the GDD.
///
////////////////////////////////////////////////
[RequireComponent(typeof(LineRenderer))] //Unity adds a LineRenderer automatically when this script is added
public class HitScanTracerObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds the line stays on screen")]
    [SerializeField, Min(0.01f)] float _LifeTime = 0.1f;

    [Tooltip("Line width over its life (0 = just fired, 1 = gone). Multiplies the LineRenderer's width")]
    [SerializeField] AnimationCurve _WidthOverLife = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("References")]
    [Tooltip("The line it draws. Grabbed from this object if left empty")]
    [SerializeField] LineRenderer _Line;

    //local variables
    float _startWidth; //the LineRenderer's width when spawned, so the curve scales from it
    float _timer;

    private void Awake()
    {
        if (_Line == null) _Line = GetComponent<LineRenderer>();
        _startWidth = _Line.widthMultiplier;
        _Line.useWorldSpace = true; //the points are world positions, not relative to this object
        _Line.positionCount = 2;
    }

    private void Update()
    {
        //shrinks the line over its life, then removes it
        _timer += Time.deltaTime;
        float lifePercent = _timer / _LifeTime;
        if (lifePercent >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        _Line.widthMultiplier = _startWidth * _WidthOverLife.Evaluate(lifePercent);
    }

    public void SetLine(Vector3 start, Vector3 end)
    {
        //function the weapon calls right after spawning the tracer to set where the line goes
        _Line.SetPosition(0, start);
        _Line.SetPosition(1, end);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//the rune field camera's tuning, in one asset (SO_RuneField_Camera) so changes made in play mode stay after stopping
//holds the zoom curve: follow distance, camera height and the zoom layers, plus the math that turns a zoom (0 to 1) into a camera spot
//the code never writes to this asset, it only reads it. Edit it by hand while playing to find the feel
[CreateAssetMenu(fileName = "SO_RuneField_Camera", menuName = "Rune Field/Camera", order = 1)]
public class RuneFieldCameraSO : ScriptableObject
{
    const int SamplesPerSegment = 24; //how many points each piece of the curve is cut into to measure its length. More = smoother, 24 is plenty
    const float MinStep = 0.0001f; //anything shorter than this counts as no distance (avoids dividing by zero)

    [Header("Zoom Curve")]
    [Tooltip("Where the camera's vertical line sits on the ground, from the aim target, in meters. X = sideways (right is positive), Y = behind. (0, 4) looks straight ahead, (3, 3) gives an isometric angle. Also decides which way the camera faces")]
    [SerializeField] Vector2 _FollowDistance = new Vector2(0f, 4f);

    [Tooltip("How tall the vertical line is, in meters. The top layer sits here (plus its height offset)")]
    [SerializeField, Min(0.1f)] float _CameraHeight = 14f;

    [Tooltip("The zoom curve's control points, bottom to top, spread evenly from the ground up to Camera Height. The curve passes through every one. Needs at least 2")]
    [SerializeField] List<ZoomLayerEntry> _ZoomLayers = new List<ZoomLayerEntry>()
    {
        new ZoomLayerEntry(6f, 0f),
        new ZoomLayerEntry(3f, 0f),
        new ZoomLayerEntry(-3.5f, 0f),
    };

    [Tooltip("The part of the curve the camera is allowed to use (0 = bottom layer, 1 = top layer, measured by distance along the curve). Lets the bottom layer sit on the ground while the camera never goes that low")]
    [SerializeField, MinMaxSlider(0f, 1f, true)] Vector2 _ZoomRange = new Vector2(0.25f, 1f);

    [Tooltip("Where the camera starts on the curve when the field opens (0 to 1). Kept inside Zoom Range")]
    [SerializeField, Range(0f, 1f)] float _StartZoom = 0.8f;

    [Tooltip("Moves the spot the camera looks at, in meters. X = sideways, Y = up, Z = forward (the way the camera faces). Starts at 0, use it if the framing needs a nudge")]
    [SerializeField] Vector3 _AimOffset = Vector3.zero;

    [Header("Zoom Feel")]
    [Tooltip("How far one scroll wheel notch moves along the curve (0 to 1). Quick notches stack")]
    [SerializeField, Range(0.001f, 0.5f)] float _ScrollStep = 0.08f;

    [Tooltip("How fast holding a trigger or [ ] moves along the curve at full press, in curve lengths per second")]
    [SerializeField, Min(0f)] float _HeldZoomSpeed = 0.7f;

    [Tooltip("How trigger pressure turns into zoom speed. X = how hard it's pressed (0 to 1), Y = how much of Held Zoom Speed is used. A curve that starts flat makes light presses slow and precise")]
    [SerializeField] AnimationCurve _HeldZoomResponse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("How long the camera takes to catch up to where the zoom was asked to go, in seconds. Lower = snappier. 0 = instant")]
    [SerializeField, Min(0f)] float _ZoomSmoothTime = 0.08f;

    [Tooltip("Mouse: zooming keeps the spot under the cursor where it is, so zooming in on a rune takes you to it. Off = always zoom toward the screen center")]
    [SerializeField] bool _ZoomTowardCursor = true;

    [Header("Moving")]
    [Tooltip("Move speed (WASD / left stick) when zoomed all the way in, in meters per second")]
    [SerializeField, Min(0f)] float _MoveSpeedZoomedIn = 4f;

    [Tooltip("Move speed when zoomed all the way out, in meters per second. Higher than zoomed in, so the same push covers about the same amount of screen")]
    [SerializeField, Min(0f)] float _MoveSpeedZoomedOut = 12f;

    [Tooltip("How long moving takes to reach full speed, and to stop, in seconds. 0 = instant")]
    [SerializeField, Min(0f)] float _MoveRampTime = 0.08f;

    [Tooltip("Speeds moving up after holding a direction for a while, to cross big fields faster")]
    [SerializeField] bool _UseHoldBoost = false;

    [Tooltip("How long a direction has to be held before the boost starts, in seconds")]
    [SerializeField, Min(0f), ShowIf("_UseHoldBoost")] float _BoostDelay = 1.5f;

    [Tooltip("Speed multiplier over time once the boost starts. X = seconds since it started, Y = multiplier (1 = normal speed)")]
    [SerializeField, ShowIf("_UseHoldBoost")] AnimationCurve _BoostCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 2f);

    [Header("Drag Panning")]
    [Tooltip("How quickly a slide (after letting go of a fast drag) slows down. Higher = stops sooner")]
    [SerializeField, Min(0f)] float _SlideFriction = 6f;

    [Tooltip("A drag released slower than this doesn't slide, in meters per second")]
    [SerializeField, Min(0f)] float _MinSlideSpeed = 1.5f;

    [Tooltip("The fastest a slide can start, in meters per second. Stops a wild flick from throwing the camera across the field")]
    [SerializeField, Min(0f)] float _MaxSlideSpeed = 40f;

    [Header("Edge Panning")]
    [Tooltip("While dragging a rune, holding the pointer near a screen edge pans the camera that way. Off = no edge panning")]
    [SerializeField] bool _UseEdgePan = true;

    [Tooltip("How wide the edge margin is, as a fraction of the screen's shorter side (0.08 = 8%). Same feel at any resolution")]
    [SerializeField, Range(0.01f, 0.3f), ShowIf("_UseEdgePan")] float _EdgePanMargin = 0.08f;

    [Tooltip("How fast edge panning goes at the very edge of the screen, as a multiple of the Move speed at the current zoom (so it covers about the same amount of screen at any zoom)")]
    [SerializeField, Min(0f), ShowIf("_UseEdgePan")] float _EdgePanSpeed = 1f;

    [Tooltip("How speed builds up through the margin. X = how deep into the margin (0 = its inner line, 1 = the screen edge), Y = how much of Edge Pan Speed is used")]
    [SerializeField, ShowIf("_UseEdgePan")] AnimationCurve _EdgePanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    //local variables
    [System.NonSerialized] bool _curveDirty = true; //NonSerialized: Unity doesn't save this, so it's true again every time the asset loads
    [System.NonSerialized] List<Vector2> _samples = new List<Vector2>(); //points along the curve (x = out from the target, y = height)
    [System.NonSerialized] List<float> _sampleDistances = new List<float>(); //how far along the curve each sample is
    [System.NonSerialized] float _curveLength;
    [System.NonSerialized] int _builtSignature; //a number made from the curve's values when it was last built, to notice when any of them change

    private void OnValidate()
    {
        //runs whenever a value changes in the inspector (also while playing), so the curve gets rebuilt with the new values
        _curveDirty = true;
        if (_ZoomRange.x > _ZoomRange.y) _ZoomRange = new Vector2(_ZoomRange.y, _ZoomRange.x);
    }

    #region Getters
    public Vector2 GetFollowDistance() { return _FollowDistance; }
    public float GetCameraHeight() { return _CameraHeight; }
    public Vector3 GetAimOffset() { return _AimOffset; }
    public float GetMinZoom() { return _ZoomRange.x; }
    public float GetMaxZoom() { return _ZoomRange.y; }
    public float GetStartZoom() { return ClampZoom(_StartZoom); }
    public float GetScrollStep() { return _ScrollStep; }
    public float GetZoomSmoothTime() { return _ZoomSmoothTime; }
    public bool GetZoomTowardCursor() { return _ZoomTowardCursor; }
    public float GetMoveRampTime() { return _MoveRampTime; }
    public float GetSlideFriction() { return _SlideFriction; }
    public float GetMinSlideSpeed() { return _MinSlideSpeed; }
    public float GetMaxSlideSpeed() { return _MaxSlideSpeed; }
    public int GetLayerCount() { return _ZoomLayers == null ? 0 : _ZoomLayers.Count; }

    public float ClampZoom(float zoom)
    {
        //keeps a zoom value inside the allowed part of the curve
        return Mathf.Clamp(zoom, _ZoomRange.x, _ZoomRange.y);
    }

    public float GetHeldZoomSpeed(float press)
    {
        //how fast holding a trigger at this pressure (0 to 1) moves along the curve, per second
        return _HeldZoomResponse.Evaluate(Mathf.Clamp01(press)) * _HeldZoomSpeed;
    }

    public float GetMoveSpeed(float zoom)
    {
        //move speed at this zoom, blending from the zoomed in speed to the zoomed out speed
        return Mathf.Lerp(_MoveSpeedZoomedIn, _MoveSpeedZoomedOut, zoom);
    }

    public bool GetUseEdgePan() { return _UseEdgePan; }

    public float GetEdgePanMargin(float screenWidth, float screenHeight)
    {
        //how wide the edge margin is on this screen, in pixels
        return _EdgePanMargin * Mathf.Min(screenWidth, screenHeight);
    }

    public float GetEdgePanSpeed(float depth, float zoom)
    {
        //edge panning speed this deep into the margin (0 to 1) at this zoom, in meters per second
        return _EdgePanCurve.Evaluate(Mathf.Clamp01(depth)) * _EdgePanSpeed * GetMoveSpeed(zoom);
    }

    public float GetBoost(float heldTime)
    {
        //speed multiplier for holding a direction this long. 1 = no boost
        if (_UseHoldBoost == false || heldTime < _BoostDelay) return 1f;
        return Mathf.Max(0f, _BoostCurve.Evaluate(heldTime - _BoostDelay));
    }
    #endregion

    #region Zoom Curve
    public Vector2 GetLayerPoint(int index)
    {
        //where one zoom layer sits, as (out from the aim target, height), in meters
        //out = the follow distance plus the layer's push. Height = its even spot on the line, moved by its height offset (a fraction of the even spacing)
        if (_ZoomLayers == null || index < 0 || index >= _ZoomLayers.Count) return Vector2.zero;

        int count = _ZoomLayers.Count;
        float spacing = count > 1 ? _CameraHeight / (count - 1) : 0f; //the gap between layers when spread evenly
        float evenHeight = count > 1 ? index * spacing : _CameraHeight; //a single layer sits at the top
        ZoomLayerEntry layer = _ZoomLayers[index];

        float height = evenHeight + layer._HeightOffset * spacing;
        float outward = _FollowDistance.magnitude + layer._OutwardPush;
        return new Vector2(outward, height);
    }

    public Vector2 EvaluateZoom(float zoom)
    {
        //where the camera sits at this zoom (0 to 1, by distance along the curve), as (out from the aim target, height), in meters
        RebuildIfDirty();
        if (_samples.Count == 0) return new Vector2(_FollowDistance.magnitude, _CameraHeight); //no layers: just sit at the top of the line
        if (_samples.Count == 1 || _curveLength < MinStep) return _samples[0];

        float target = Mathf.Clamp01(zoom) * _curveLength; //how far along the curve to go, in meters

        //walk the samples until the one past the target distance, then blend between it and the one before
        for (int i = 1; i < _samples.Count; i++)
        {
            if (_sampleDistances[i] < target) continue;

            float gap = _sampleDistances[i] - _sampleDistances[i - 1];
            float blend = gap > MinStep ? (target - _sampleDistances[i - 1]) / gap : 0f;
            return Vector2.Lerp(_samples[i - 1], _samples[i], blend);
        }
        return _samples[_samples.Count - 1];
    }

    public float GetCurveLength()
    {
        //total length of the curve, in meters
        RebuildIfDirty();
        return _curveLength;
    }

    void RebuildIfDirty()
    {
        //only rebuilds when a value changed, so the math isn't redone every frame
        //the signature check catches edits OnValidate might miss (like editing this asset from inside another object's inspector)
        int signature = GetCurveSignature();
        if (_curveDirty == false && _samples != null && signature == _builtSignature) return;
        _builtSignature = signature;
        RebuildCurve();
    }

    int GetCurveSignature()
    {
        //mixes every value the curve depends on into one number. If any value changes, the number almost always changes too
        //unchecked lets the number wrap around instead of erroring when it gets too big (normal for this kind of mixing)
        unchecked
        {
            int signature = 17;
            signature = signature * 31 + _FollowDistance.GetHashCode();
            signature = signature * 31 + _CameraHeight.GetHashCode();
            if (_ZoomLayers == null) return signature;

            signature = signature * 31 + _ZoomLayers.Count;
            for (int i = 0; i < _ZoomLayers.Count; i++)
            {
                if (_ZoomLayers[i] == null) continue;
                signature = signature * 31 + _ZoomLayers[i]._OutwardPush.GetHashCode();
                signature = signature * 31 + _ZoomLayers[i]._HeightOffset.GetHashCode();
            }
            return signature;
        }
    }

    void RebuildCurve()
    {
        //cuts the curve through every layer point into small straight pieces, and measures how far along each piece is
        //this is what lets zoom be measured by distance: 0.5 is always halfway along the curve, however the layers are spaced
        _curveDirty = false;
        if (_samples == null) _samples = new List<Vector2>();
        if (_sampleDistances == null) _sampleDistances = new List<float>();
        _samples.Clear();
        _sampleDistances.Clear();
        _curveLength = 0f;

        int count = GetLayerCount();
        if (count == 0) return;

        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < count; i++) points.Add(GetLayerPoint(i));

        _samples.Add(points[0]);
        _sampleDistances.Add(0f);
        if (count == 1) return;

        for (int i = 0; i < count - 1; i++)
        {
            //each piece runs from point i to point i + 1. Catmull-Rom also needs the point before and after, so the ends repeat their own point
            Vector2 before = points[Mathf.Max(i - 1, 0)];
            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            Vector2 after = points[Mathf.Min(i + 2, count - 1)];

            for (int step = 1; step <= SamplesPerSegment; step++)
            {
                float t = step / (float)SamplesPerSegment;
                Vector2 sample = CatmullRom(before, start, end, after, t);
                _curveLength += Vector2.Distance(_samples[_samples.Count - 1], sample);
                _samples.Add(sample);
                _sampleDistances.Add(_curveLength);
            }
        }
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        //a point on a Catmull-Rom curve between p1 and p2 (t = 0 is p1, t = 1 is p2). The curve passes through every point it's given
        //this is the "centripetal" kind: it spaces things by the distance between points, which stops loops and sharp kinks when layers are close together
        float t0 = 0f;
        float t1 = t0 + KnotGap(p0, p1);
        float t2 = t1 + KnotGap(p1, p2);
        float t3 = t2 + KnotGap(p2, p3);
        float u = Mathf.Lerp(t1, t2, t);

        Vector2 a1 = Blend(p0, p1, t0, t1, u);
        Vector2 a2 = Blend(p1, p2, t1, t2, u);
        Vector2 a3 = Blend(p2, p3, t2, t3, u);
        Vector2 b1 = Blend(a1, a2, t0, t2, u);
        Vector2 b2 = Blend(a2, a3, t1, t3, u);
        return Blend(b1, b2, t1, t2, u);
    }

    static float KnotGap(Vector2 a, Vector2 b)
    {
        //the centripetal spacing: square root of the distance. Never 0, so repeated end points don't divide by zero
        return Mathf.Max(Mathf.Sqrt(Vector2.Distance(a, b)), MinStep);
    }

    static Vector2 Blend(Vector2 a, Vector2 b, float ta, float tb, float u)
    {
        //blends between a and b depending on where u sits between ta and tb (one step of the Catmull-Rom math)
        return (tb - u) / (tb - ta) * a + (u - ta) / (tb - ta) * b;
    }
    #endregion
}

[System.Serializable]
public class ZoomLayerEntry
{
    [Tooltip("How far out from the vertical line the camera sits at this layer, in meters, pushed straight away from the aim target. Negative pulls it in, even past the line")]
    public float _OutwardPush = 0f;

    [Tooltip("Moves this layer up or down, as a fraction of the even gap between layers. -0.1 moves it 10% of the way toward the layer below")]
    [Range(-1f, 1f)] public float _HeightOffset = 0f;

    public ZoomLayerEntry()
    {
        //empty constructor so the inspector's "add" button can make a new layer
    }

    public ZoomLayerEntry(float outwardPush, float heightOffset)
    {
        //lets the asset's default layers be written in one line each
        _OutwardPush = outwardPush;
        _HeightOffset = heightOffset;
    }
}

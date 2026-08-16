using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeOut : MonoBehaviour
{
    [SerializeField] private Image _canvasImage;
    [SerializeField] AnimationCurve _fadeCurve;

    private Coroutine _fadeTimer;

    private void Awake()
    {
        if (_canvasImage == null)
        {
            _canvasImage = GetComponent<Image>();
            if (_canvasImage == null) Debug.LogError("Warning! Script is not attached to a canvas object with an image");
        }
    }

    public void FadeOut(float fadeTime)
    {
        _canvasImage.enabled = true;
        if (_fadeTimer != null)
        {
            StopCoroutine(_fadeTimer);
            _fadeTimer = null;
        }
        _fadeTimer = StartCoroutine(fadeTimer(fadeTime, 1));
    }

    public void FadeIn(float fadeTime)
    {
        _canvasImage.enabled = true;
        if (_fadeTimer != null)
        {
            StopCoroutine(_fadeTimer);
            _fadeTimer = null;
        }
        _fadeTimer = StartCoroutine(fadeTimer(fadeTime, 0));
    }

    public float GetFadeStatus()
    {
        return _canvasImage.color.a;
    }

    private IEnumerator fadeTimer(float fadeTime, float fadeValueGoal)
    {
        float startAlpha = _canvasImage.color.a;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(timer / fadeTime);
            float curveValue = _fadeCurve.Evaluate(normalizedTime);

            float newAlpha = Mathf.Lerp(startAlpha, fadeValueGoal, curveValue);

            Color imageColor = _canvasImage.color;
            imageColor.a = newAlpha;
            _canvasImage.color = imageColor;

            yield return null;
        }

        // Make sure we end exactly at the target value.
        Color finalColor = _canvasImage.color;
        finalColor.a = fadeValueGoal;
        _canvasImage.color = finalColor;
        if (fadeValueGoal == 0)
        {
            _canvasImage.enabled = false;
        }
        _fadeTimer = null;
    }
}

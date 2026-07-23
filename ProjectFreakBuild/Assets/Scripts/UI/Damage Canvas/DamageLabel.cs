using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageLabel : MonoBehaviour
{
    [Header("Label Settings")]
    [SerializeField] private TMP_Text damageText;
    public float normalFontSize;
    public float critFontSize;

    [SerializeField] private float _textStartOffset;

    [SerializeField] private float _RiseDistance = 35f;
    [SerializeField] private float _FadeInTime = 0.15f;
    [SerializeField] private float _HoldTime = 0.5f;
    [SerializeField] private float _FadeOutTime = 0.12f;

    [SerializeField] private float _ColorAlpha;

    [SerializeField] private AnimationCurve _MoveCurve;
    [SerializeField] private AnimationCurve _FadeInCurve;
    [SerializeField] private AnimationCurve _FadeOutCurve;

    private RectTransform _rect;
    private CanvasGroup _group;

    [SerializeField]UIDamageCanvas _poolManager;
    private Coroutine _moveCoroutine;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _group = GetComponent<CanvasGroup>();
    }
    public void Initialize(Vector2 screenPositionStart, float xRandomRange, int Damage, TMP_ColorGradient gradColor, float fontSize)
    {
        //reset coroutine
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        //set damage transform
        _rect.position = screenPositionStart - new Vector2(randomOffsetRange(xRandomRange), _textStartOffset);

        //set damage number and font size
        damageText.text = Damage.ToString();
        damageText.fontSize = fontSize;

        //set color type
        updateColorType(gradColor);
        //_group.alpha = 0;

        //start movement coroutine here
        _moveCoroutine = StartCoroutine(DamageRoutine());
    }

    #region Coroutines
    IEnumerator DamageRoutine()
    {
        // Fade In
        yield return FadeIn();

        // Hold
        yield return new WaitForSeconds(_HoldTime);

        // Fade Out
        yield return FadeOut();

        // Return to pool
        _poolManager.release(this);
    }

    IEnumerator FadeIn()
    {
        float timer = 0f;
        Vector2 startPos = _rect.position;
        Vector2 endPos = startPos + Vector2.up * _textStartOffset;

        while (timer < _FadeInTime)
        {
            timer += Time.deltaTime;

            float t = timer / _FadeInTime;

            float moveT = _MoveCurve.Evaluate(t);
            _rect.position = Vector2.Lerp(startPos, endPos, moveT);

            _group.alpha = _FadeInCurve.Evaluate(t);


            yield return null;
        }
    }

    IEnumerator FadeOut()
    {
        float timer = 0f;

        while (timer < _FadeOutTime)
        {
            timer += Time.deltaTime;

            float t = timer / _FadeOutTime;

            _group.alpha = 1f - _FadeOutCurve.Evaluate(t);

            yield return null;
        }   
        _group.alpha = 0f;
    }

    #endregion

    #region Tools
    float randomOffsetRange(float amount)
    {
        float ran = Random.Range(amount * -1, amount);
        return ran;
    }
    public void updateColorType(TMP_ColorGradient gradColor)
    {
        damageText.enableVertexGradient = true;
        damageText.colorGradientPreset = gradColor;
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Sirenix.OdinInspector;

public class UIDamageCanvas : MonoBehaviour
{
    [FoldoutGroup("Settings")]
    public float _FontSize = 50;
    [FoldoutGroup("Settings")]
    public float _CritFontSize = 75;
    [FoldoutGroup("Settings")]
    public float _textXStartRange = 40;

    [FoldoutGroup("Color Settings")]
    [Header("Color Gradiants")]
    [SerializeField] private TMPro.TMP_ColorGradient _ColorBasic;
    [FoldoutGroup("Color Settings")]
    [SerializeField] private TMPro.TMP_ColorGradient _ColorCrit;
    [FoldoutGroup("Color Settings")]
    [SerializeField] private TMPro.TMP_ColorGradient _ColorHealing;
    [FoldoutGroup("Color Settings")]
    [SerializeField] private TMPro.TMP_ColorGradient _ColorFire;
    [FoldoutGroup("Color Settings")]
    [SerializeField] private TMPro.TMP_ColorGradient _ColorPoison;

    [FoldoutGroup("Reference")]
    private ObjectPool<DamageLabel> _damageLabelPopupPool;
    [FoldoutGroup("Reference")]
    [SerializeField] private DamageLabel damageLabelPrefab;

    private void Awake()
    {
        _damageLabelPopupPool = new ObjectPool<DamageLabel>(CreateDamageLabel,OnGetDamageLabel,OnReleaseDamageLabel,OnDestroyDamageLabel,true,20,100);
    }
    public void DisplayDamage(Vector3 damageLocation, int dmg, bool isCritDisplay)
    {
        if (IsOnScreen(damageLocation) == false) return;

        DamageLabel label = _damageLabelPopupPool.Get();
        Vector2 screenPos = Camera.main.WorldToScreenPoint(damageLocation);

        label.Initialize(screenPos, _textXStartRange , Mathf.Abs(dmg), GetDamageColor(isCritDisplay, dmg), GetDamageSize(isCritDisplay));
    }

    public void Release(DamageLabel label)
    {
        _damageLabelPopupPool.Release(label);
    }

    #region Tools
    bool IsOnScreen(Vector3 damageLocation)
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(damageLocation);

        return viewportPos.z > 0 &&
               viewportPos.x >= 0f &&
               viewportPos.x <= 1f &&
               viewportPos.y >= 0f &&
               viewportPos.y <= 1f;
    }

    TMPro.TMP_ColorGradient GetDamageColor(bool crit, int dmg)
    {
        if (dmg < 0) return _ColorHealing;
        if (crit) return _ColorCrit;
        
        return _ColorBasic;
    }

    float GetDamageSize(bool isCrit)
    {
        if (isCrit) return _CritFontSize;
        else return _FontSize;
    }

    #endregion

    #region Object Pool Functions
    private DamageLabel CreateDamageLabel()
    {
        DamageLabel label = Instantiate(damageLabelPrefab, damageLabelPrefab.transform.parent);
        label.gameObject.name += "_instanced";
        return label;
    }

    private void OnGetDamageLabel(DamageLabel label)
    {
        label.gameObject.SetActive(true);
    }

    private void OnReleaseDamageLabel(DamageLabel label)
    {
        label.gameObject.SetActive(false);
    }

    private void OnDestroyDamageLabel(DamageLabel label)
    {
        Destroy(label.gameObject);
    }

    [Button("Test")]
    void UpdateTextColor(DamageLabel label)
    {
        label.UpdateColorType(_ColorBasic);
    }
    #endregion
}
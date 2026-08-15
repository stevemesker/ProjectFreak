using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class HUDManager : MonoBehaviour
{
    [Header("Pointers")]
    public static HUDManager _HUD;
    public ScreenFadeOut FadeOutCanvasObject;

    private void Awake()
    {
        if (HUDManager._HUD != null)
        {
            Destroy(gameObject);
            return;
        }
        HUDManager._HUD = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ToggleHud(bool state)
    {
        gameObject.SetActive(state);
    }

    public void ToggleBattleHUD(bool state)
    {
        return;
    }

    #region Fades

    public float getCurrentFadeValue ()
    {
        return FadeOutCanvasObject.GetFadeStatus();
    }

    [FoldoutGroup("Screen Fades")]
    [Button("Fade Out")]
    public void FadeOut(float speed)
    {
        FadeOutCanvasObject.FadeOut(speed);
    }
    [FoldoutGroup("Screen Fades")]
    [Button("Fade In")]
    public void FadeIn(float speed)
    {
        FadeOutCanvasObject.FadeIn(speed);
    }

    #endregion
}

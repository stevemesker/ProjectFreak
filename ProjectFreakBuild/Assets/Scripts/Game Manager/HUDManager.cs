using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class HUDManager : MonoBehaviour
{
    [Header("Pointers")]
    public static HUDManager _HUD;
    public ScreenFadeOut FadeOutCanvasObject;
    [SerializeField] GameObject _RadialMenu;

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

    #region RadialMenu
    [Button("Activate")]
    public void toggleRadialMenu()
    {
        if (_RadialMenu == null) return;
        _RadialMenu.SetActive(!_RadialMenu.activeSelf);
        _RadialMenu.GetComponent<RadialMenuManager>().enabled = _RadialMenu.activeSelf;
        //turns on tamer abilities but not sure if we'll need to make other kinds of abilities...
        UpdateRadialDial(Player.player.pData._TamerAbilities.Count);
        Player.player.SetPlayerTurning(!_RadialMenu.activeSelf);
    }

    [Button("Update Radial Dial")]
    void UpdateRadialDial (int ButtonCount)
    {
        _RadialMenu.GetComponent<RadialMenuManager>().UpdateRadialButtonSetUp(ButtonCount);
    }

    #endregion

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

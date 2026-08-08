using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class HUDManager : MonoBehaviour
{
    [Header("Pointers")]
    public ScreenFadeOut FadeOutCanvasObject;

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
}

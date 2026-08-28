using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ColorPalette_Color", menuName = "Color/ColorPalette", order = 0)]
public class ColorPaletteSO : ScriptableObject
{
    public Color _PrimaryColor;
    public Color _SecondaryColor;
    public Color _TertiaryColor;
}

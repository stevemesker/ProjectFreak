using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ElementObject : MonoBehaviour
{
    public Image ElementIcon; 

    public void UpdateIcon(Sprite newSprite)
    {
        ElementIcon.sprite = newSprite;
    }
}

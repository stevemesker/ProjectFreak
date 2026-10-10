using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//tints a group of renderers with one color, without making copies of their materials
//shared by the 3D rune field pieces (runes, nodes, bridges) so the tint code is written once
//a plain C# class, not a component: each piece makes one with "new RuneFieldTint(...)"
public class RuneFieldTint
{
    //local variables
    List<Renderer> _renderers = new List<Renderer>();
    MaterialPropertyBlock _block; //a MaterialPropertyBlock changes a value for one renderer only, so every rune can have its own color while sharing one material
    int _colorID;

    public RuneFieldTint(List<Renderer> renderers, string colorProperty)
    {
        //colorProperty is the material's color name: "_BaseColor" for HDRP Lit, "_UnlitColor" for HDRP Unlit
        if (renderers != null) _renderers.AddRange(renderers);
        _block = new MaterialPropertyBlock();
        _colorID = Shader.PropertyToID(string.IsNullOrEmpty(colorProperty) ? "_BaseColor" : colorProperty);
    }

    public void SetColor(Color color)
    {
        //puts this color on every renderer in the group
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].GetPropertyBlock(_block); //reads what's already set, so other values on the block aren't wiped
            _block.SetColor(_colorID, color);
            _renderers[i].SetPropertyBlock(_block);
        }
    }

    public void Clear()
    {
        //goes back to the materials' own colors
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (_renderers[i] != null) _renderers[i].SetPropertyBlock(null);
        }
    }

    public static List<Renderer> FindRenderers(Transform root, Transform skip)
    {
        //every renderer under root, leaving out anything under skip (like a node's OnEffect mesh). skip can be null
        List<Renderer> found = new List<Renderer>();
        if (root == null) return found;

        Renderer[] all = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (skip != null && all[i].transform.IsChildOf(skip)) continue; //IsChildOf is also true for skip itself
            found.Add(all[i]);
        }
        return found;
    }
}

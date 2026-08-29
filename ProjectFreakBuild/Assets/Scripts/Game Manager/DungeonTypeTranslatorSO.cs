using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonTypeTranslator", menuName = "Dungeon/Dungeon Type Translator")]
public class DungeonTypeTranslatorSO : ScriptableObject
{
    public TypePackage _DefaultTypePackage;
    public List<TypePackage> _TypePackages;

    public Sprite GetSprite(POIType.Type type)
    {
        return GetPackage(type)._TypeSprite;
    }

    public Texture2D GetTexture(POIType.Type type)
    {
        return GetPackage(type)._TypeTexture;
    }

    private TypePackage GetPackage(POIType.Type type)
    {
        foreach (TypePackage package in _TypePackages)
        {
            if (package._PackageType == type)
                return package;
        }

        return _DefaultTypePackage;
    }
}

[System.Serializable]
public class TypePackage
{
    public POIType.Type _PackageType;
    public Sprite _TypeSprite;
    public Texture2D _TypeTexture;
}

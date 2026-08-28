using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonDoor : MonoBehaviour
{
    [Header("Data")]
    public DungeonMapNode _NextRoomNode;
    [SerializeField] ColorPaletteSO _ColorPalette;

    [Header("References")]
    [SerializeField] DungeonManagerWrapper _DMWrapper;
    [SerializeField] Renderer[] _DoorRenderers;

    private MaterialPropertyBlock _PropertyBlock;

    private static readonly int DoorColor = Shader.PropertyToID("_DoorColor");

    private void Awake()
    {
        _PropertyBlock = new MaterialPropertyBlock();
    }

    public void ApplyNodeData(DungeonMapNode data)
    {
        _NextRoomNode = data;
        _ColorPalette = _NextRoomNode._ColorSwatch;

        ApplyDoorColor();

        if (_DMWrapper == null)
        {
            if (gameObject.GetComponent<DungeonManagerWrapper>() == null)
                _DMWrapper = gameObject.AddComponent<DungeonManagerWrapper>();
            else
                _DMWrapper = gameObject.GetComponent<DungeonManagerWrapper>();
        }
    }

    private void ApplyDoorColor()
    {
        if (_ColorPalette == null)
            return;

        foreach (Renderer renderer in _DoorRenderers)
        {
            renderer.GetPropertyBlock(_PropertyBlock);
            _PropertyBlock.SetColor(DoorColor, _ColorPalette._PrimaryColor);
            renderer.SetPropertyBlock(_PropertyBlock);
        }
    }

    public void EnterNewDungeonScene()
    {
        _DMWrapper.MoveToDungeonRoom(_NextRoomNode._ID);
    }
    /*
    public DungeonMapNode _NextRoomNode;
    //public string _SceneName;
    [SerializeField] DungeonManagerWrapper _DMWrapper;
    [SerializeField] ColorPaletteSO _ColorPalette;

    public void ApplyNodeData(DungeonMapNode data)
    {
        _NextRoomNode = data;
        _ColorPalette = _NextRoomNode._ColorSwatch;

        if (_DMWrapper == null)
        {
            if (gameObject.GetComponent<DungeonManagerWrapper>() == null)
                _DMWrapper = gameObject.AddComponent<DungeonManagerWrapper>();
            else
                _DMWrapper = gameObject.GetComponent<DungeonManagerWrapper>();
        }
    }

    public void EnterNewDungeonScene()
    {
        _DMWrapper.MoveToDungeonRoom(_NextRoomNode._ID);
    }
    */
}

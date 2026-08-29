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
    [SerializeField] GameObject _TpLocator;
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

        if (DungeonManager._DM._PreviousRoomNode == _NextRoomNode)
        {
            print($"Player just came from room behind {gameObject.name}, now teleportying to front of door...");
            Player.player.transform.position = _TpLocator.transform.position;
            Player.player.transform.rotation = _TpLocator.transform.rotation;
        }

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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class ChestContent : MonoBehaviour
{
    public List<ItemSO> _Content;
    public TimelineRunner _TLine;
    public bool _isOpened;

    [SerializeField] private PlayableDirector director;
    [SerializeField] GameObject _itemHolderPrefab;

    [SerializeField, Tooltip("How wide of an ar the hest will drop loot")] private float _LootArc = 90;
    [SerializeField, Tooltip("How far the loot can drop from the chest")] private float _LootDistance;
    [SerializeField, Tooltip("How far below the chest an item can drop")] private float _maxLootHeight;
    [SerializeField] private float _LootDisperseWaitTime;
    [SerializeField] private float _LootArcHeight = 1.5f;
    [SerializeField] private float _LootArcDuration = .5f;
    Coroutine lootTimer;

    #region Opening Animations
    private void Awake()
    {
        //I'm a forgetful boy sometimes....
        if (director == null) director = gameObject.GetComponent<PlayableDirector>();
        director.stopped += OnTimelineFinished;
    }
    private void OnDestroy()
    {
        director.stopped -= OnTimelineFinished;
    }
    private void OnTimelineFinished(PlayableDirector director)
    {
        _TLine.ContinueTimeline();
    }

    public void PlayChestOpen()
    {
        director.Play();
    }
    public void openChest()
    {
        if (_isOpened) return;
        _isOpened = true;

        Debug.Log("Opening chest " + gameObject.name);

        _TLine.PlayTimeline();
    }
    #endregion

    #region Loot
    public void SpawnLootDrop()
    {
        print("Now spawning loot X" + _Content.Count);
        lootTimer = StartCoroutine(spawnTimer());
    }

    IEnumerator spawnTimer()
    {
        for (int i = 0; i < _Content.Count; i++)
        {
            GameObject itemInstance = Instantiate(_itemHolderPrefab, transform.position, Quaternion.identity);

            ItemDrop spawn = itemInstance.GetComponent<ItemDrop>();
            spawn.fillDrop(_Content[i]);

            spawn.MoveArc(
                lootTargetPosition(i, _Content.Count),
                _LootArcHeight,
                _LootArcDuration);

            yield return new WaitForSeconds(_LootDisperseWaitTime);
        }
    }

    Vector3 lootTargetPosition (int index, int totalLoot)
    {
        //return transform.position + transform.forward;
        // Single item? Just drop directly in front.
        if (totalLoot == 1)
            return transform.position + transform.forward * _LootDistance;

        // Degrees between each drop.
        float spacing = _LootArc / (totalLoot - 1);

        int offset;

        if (index == 0)
        {
            offset = 0;
        }
        else if (index % 2 == 1)
        {
            offset = -(index + 1) / 2;
        }
        else
        {
            offset = index / 2;
        }

        float angle = offset * spacing;

        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

        Vector3 direction = rotation * transform.forward;

        return transform.position + direction * _LootDistance;
    }
    #endregion
}

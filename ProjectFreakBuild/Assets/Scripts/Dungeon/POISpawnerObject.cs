using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POISpawnerObject : MonoBehaviour
{
    [SerializeField] List<POIType.Type> _TypeTags;
    void Start()
    {
        DungeonManager._DM.getPOIFromCurrentRoom();
    }

}

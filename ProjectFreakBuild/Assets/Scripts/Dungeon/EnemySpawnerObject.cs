using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

public class EnemySpawnerObject : MonoBehaviour
{
    //Places enemies on the floor before the player gets there. Asks the Dungeon Manager for the dungeon's enemy table,
    //then picks spread-out spots on the NavMesh inside its box and spawns an enemy right on each one.
    //For enemies that pour out of something during play, use an emitter instead (not built yet).

    [Header("Data")]
    [InfoBox("MiniBoss and Boss ranks are skipped. They're placed by hand in their arenas", InfoMessageType.Warning, "HasBossRanks")]
    [Tooltip("How many enemies of each rank to place. A random number between Min and Max is picked, then multiplied by the floor type multiplier in the dungeon's enemy table")]
    [SerializeField] List<SpawnCountEntry> _SpawnCounts = new List<SpawnCountEntry>()
    {
        new SpawnCountEntry(EnemyType.Rank.Popcorn, 2, 4),
        new SpawnCountEntry(EnemyType.Rank.Basic, 1, 2),
        new SpawnCountEntry(EnemyType.Rank.Lieutenant, 0, 1),
    };

    [Header("Settings")]
    [Tooltip("Size of the spawn box in meters (width, height, depth). Centered on this object and turns with it. The floor needs to be inside the box's height")]
    [SerializeField] Vector3 _SpawnAreaSize = new Vector3(8f, 4f, 8f);

    [Tooltip("Closest two enemies can spawn to each other, in meters. Also keeps away from enemies placed by other spawners")]
    [SerializeField, Min(0f)] float _MinSpacing = 2f;

    [Tooltip("Closest an enemy can spawn to the player, in meters")]
    [SerializeField, Min(0f)] float _MinPlayerDistance = 8f;

    [Tooltip("Only use spots that can be walked to from the middle of the box. Stops enemies landing on ledges or tables that are cut off from the floor")]
    [SerializeField] bool _RequireReachable = true;

    [Tooltip("Turn each enemy to face a random direction. Off = they all face the way this spawner faces")]
    [SerializeField] bool _RandomFacing = true;

    [Tooltip("How many random spots to try for each enemy before giving up on it. Higher fits more enemies into tight areas but takes longer to load")]
    [SerializeField, Range(1, 100)] int _TriesPerEnemy = 30;

    [Tooltip("Color of the spawn box in the Scene view")]
    [SerializeField] Color _GizmoColor = new Color(1f, 0.3f, 0.2f, 1f);

    [FoldoutGroup("Testing")]
    [Tooltip("Enemy table to use when not inside a dungeon, like when playing this floor scene directly. Leave empty to spawn nothing outside a dungeon")]
    [SerializeField] DungeonEnemyTableSO _TestEnemyTable;

    [FoldoutGroup("Testing")]
    [Tooltip("Floor type to use with the test table's multipliers")]
    [SerializeField] POIType.Type _TestFloorType = POIType.Type.Basic;

    [Header("Runtime Data")]
    [Tooltip("The enemies this spawner placed (read only)")]
    [SerializeField, ReadOnly] List<GameObject> _SpawnedEnemies = new List<GameObject>();

    //local variables
    List<Vector3> _pickedSpots = new List<Vector3>(); //the spots this spawner used, kept for the gizmos
    NavMeshPath _path; //reused for the reachable check so we don't make a new one every try
    bool _hasSpawned;

    void Start()
    {
        //same as the POI spawner: register with this scene's floor and let it call SpawnEnemies() once the NavMesh is built
        //otherwise spawn right away, so hand-built scenes with a baked NavMesh still work
        if (DungeonFloorObject._Floor != null && DungeonFloorObject._Floor.gameObject.scene == gameObject.scene) //same scene check so we never register with a floor left over from the last scene
        {
            DungeonFloorObject._Floor.RegisterEnemySpawner(this);
            return;
        }
        SpawnEnemies(new List<Vector3>());
    }

    private void OnValidate()
    {
        //keeps Max from going below Min when values are typed in the inspector
        for (int i = 0; i < _SpawnCounts.Count; i++)
        {
            if (_SpawnCounts[i] == null) continue;
            if (_SpawnCounts[i]._Max < _SpawnCounts[i]._Min) _SpawnCounts[i]._Max = _SpawnCounts[i]._Min;
        }

        //a box with no width or depth has nowhere to spawn
        _SpawnAreaSize.x = Mathf.Max(0.1f, _SpawnAreaSize.x);
        _SpawnAreaSize.y = Mathf.Max(0.1f, _SpawnAreaSize.y);
        _SpawnAreaSize.z = Mathf.Max(0.1f, _SpawnAreaSize.z);
    }

    #region Spawning
    public void SpawnEnemies(List<Vector3> takenSpots)
    {
        //function that picks enemies from the enemy table and places each one on its own spot in the box
        //takenSpots is shared by every spawner on the floor, so spawners with overlapping boxes still keep their enemies apart
        if (_hasSpawned) return; //already spawned, don't double up
        if (takenSpots == null) takenSpots = new List<Vector3>();
        if (TryGetEnemyTable(out DungeonEnemyTableSO enemyTable, out POIType.Type floorType) == false) return;

        _hasSpawned = true;
        _path = new NavMeshPath();

        //find the middle of the box on the NavMesh, used to check each spot can be walked to
        bool checkReachable = _RequireReachable;
        Vector3 centerPoint = transform.position;
        if (checkReachable && NavMeshTools.TryGetNavMeshPoint(transform.position, _SpawnAreaSize.y * 0.5f, out centerPoint) == false)
        {
            Debug.LogWarning($"Warning! The middle of {gameObject.name}'s box isn't on the NavMesh, so spots can't be checked for being reachable. Spawning without that check...", this);
            checkReachable = false;
        }

        for (int i = 0; i < _SpawnCounts.Count; i++)
        {
            SpawnCountEntry countEntry = _SpawnCounts[i];
            if (countEntry == null) continue;
            if (IsSpawnerRank(countEntry._Rank) == false) continue; //bosses are placed by hand, the info box warns about this

            int count = GetSpawnCount(countEntry, enemyTable, floorType);
            for (int j = 0; j < count; j++)
            {
                GameObject enemyPrefab = enemyTable.GetEnemy(countEntry._Rank);
                if (enemyPrefab == null)
                {
                    Debug.LogWarning($"Warning! {enemyTable.name} has no {countEntry._Rank} enemies for {gameObject.name}, skipping that rank...", this);
                    break;
                }

                if (TryFindSpot(takenSpots, checkReachable, centerPoint, out Vector3 spot) == false)
                {
                    Debug.LogWarning($"Warning! {gameObject.name} ran out of room and placed {j} of {count} {countEntry._Rank} enemies. Make the box bigger or the spacing smaller...", this);
                    break;
                }

                takenSpots.Add(spot);
                _pickedSpots.Add(spot);
                SpawnEnemy(enemyPrefab, spot);
            }
        }
    }

    int GetSpawnCount(SpawnCountEntry countEntry, DungeonEnemyTableSO enemyTable, POIType.Type floorType)
    {
        //function that rolls how many enemies of a rank to place, then applies the floor type multiplier
        int baseCount = Random.Range(countEntry._Min, countEntry._Max + 1); //Random.Range with whole numbers never returns the max, so +1 to include it
        float multiplier = enemyTable.GetCountMultiplier(floorType, countEntry._Rank);
        return Mathf.RoundToInt(baseCount * multiplier);
    }

    bool TryFindSpot(List<Vector3> takenSpots, bool checkReachable, Vector3 centerPoint, out Vector3 spot)
    {
        //function that tries random spots in the box until one passes every check. Returns false if none did
        float halfHeight = _SpawnAreaSize.y * 0.5f;
        for (int i = 0; i < _TriesPerEnemy; i++)
        {
            //random spot on the box's middle layer, then snap it onto the NavMesh
            Vector3 localSpot = new Vector3(Random.Range(-_SpawnAreaSize.x, _SpawnAreaSize.x) * 0.5f, 0f, Random.Range(-_SpawnAreaSize.z, _SpawnAreaSize.z) * 0.5f);
            Vector3 candidate = transform.position + transform.rotation * localSpot; //rotating the offset makes the box turn with the object
            if (NavMeshTools.TryGetNavMeshPoint(candidate, halfHeight, out spot) == false) continue;

            if (IsInsideBox(spot) == false) continue; //snapping can slide the spot out past the box's edge
            if (IsTooCloseToOthers(spot, takenSpots)) continue;
            if (IsTooCloseToPlayer(spot)) continue;
            if (checkReachable && IsReachable(centerPoint, spot) == false) continue;
            return true;
        }

        spot = transform.position;
        return false;
    }

    void SpawnEnemy(GameObject enemyPrefab, Vector3 spot)
    {
        //function that spawns one enemy on its spot, facing a random way or the spawner's way
        float facing = _RandomFacing ? Random.Range(0f, 360f) : transform.eulerAngles.y; //"a ? b : c" means "if a then b, otherwise c"
        GameObject enemy = Instantiate(enemyPrefab, spot, Quaternion.Euler(0f, facing, 0f));

        //new objects go into whichever scene is "active", so make sure the enemy lands in this floor's scene and unloads with it
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enemy, gameObject.scene);
        _SpawnedEnemies.Add(enemy);
    }

    bool TryGetEnemyTable(out DungeonEnemyTableSO enemyTable, out POIType.Type floorType)
    {
        //function that asks the Dungeon Manager for the current dungeon's enemy table and floor type, or uses the test table outside a dungeon
        enemyTable = null;
        floorType = _TestFloorType;

        if (DungeonManager._DM != null && DungeonManager._DM._CurrentDungeon != null)
        {
            enemyTable = DungeonManager._DM.GetCurrentEnemyTable();
            floorType = DungeonManager._DM.GetCurrentRoomType();
            if (enemyTable == null) { Debug.LogWarning($"Warning! {DungeonManager._DM._CurrentDungeon.name} has no enemy table, so {gameObject.name} is spawning nothing...", this); return false; }
            return true;
        }

        if (_TestEnemyTable != null)
        {
            enemyTable = _TestEnemyTable;
            return true;
        }

        Debug.LogWarning($"Warning! Not inside a dungeon and no test enemy table set on {gameObject.name}, spawning nothing...", this);
        return false;
    }
    #endregion

    #region Spot Checks
    bool IsInsideBox(Vector3 worldSpot)
    {
        //function that checks if a spot is inside the spawn box. Turns the spot into the box's own space first so rotation doesn't matter
        Vector3 localSpot = Quaternion.Inverse(transform.rotation) * (worldSpot - transform.position);
        Vector3 halfSize = _SpawnAreaSize * 0.5f;
        return Mathf.Abs(localSpot.x) <= halfSize.x && Mathf.Abs(localSpot.y) <= halfSize.y && Mathf.Abs(localSpot.z) <= halfSize.z;
    }

    bool IsTooCloseToOthers(Vector3 spot, List<Vector3> takenSpots)
    {
        //function that checks the spot isn't too close to an enemy that's already been placed
        for (int i = 0; i < takenSpots.Count; i++)
        {
            if (Vector3.Distance(spot, takenSpots[i]) < _MinSpacing) return true;
        }
        return false;
    }

    bool IsTooCloseToPlayer(Vector3 spot)
    {
        //function that checks the spot isn't right next to the player (already moved to the spawn point or door by now)
        if (Player.player == null) return false;
        return Vector3.Distance(spot, Player.player.transform.position) < _MinPlayerDistance;
    }

    bool IsReachable(Vector3 from, Vector3 to)
    {
        //CalculatePath works out a route without moving anything. PathComplete = the route gets all the way there,
        //anything else means the spot is on a separate patch of NavMesh (like a tabletop)
        if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path) == false) return false;
        return _path.status == NavMeshPathStatus.PathComplete;
    }
    #endregion

    #region Tools
    bool IsSpawnerRank(EnemyType.Rank rank)
    {
        //function that checks if a rank is placed by spawners. MiniBosses and Bosses are placed by hand
        return rank != EnemyType.Rank.MiniBoss && rank != EnemyType.Rank.Boss;
    }

    bool HasBossRanks()
    {
        //editor check for the warning box: any MiniBoss or Boss in the spawn counts
        for (int i = 0; i < _SpawnCounts.Count; i++)
        {
            if (_SpawnCounts[i] != null && IsSpawnerRank(_SpawnCounts[i]._Rank) == false) return true;
        }
        return false;
    }
    #endregion

    #region Test Tools
    [Button("Respawn")]
    [GUIColor(0f, 1f, 0f)]
    void Respawn()
    {
        //play mode button for tuning: clears this spawner's enemies and spawns a new set (ignores other spawners' spots)
        if (Application.isPlaying == false) { Debug.LogWarning("Warning! Respawn only works in play mode"); return; }
        ClearSpawned();
        SpawnEnemies(new List<Vector3>());
    }

    [Button("Clear Spawned")]
    [GUIColor("#ff9000")]
    void ClearSpawned()
    {
        //play mode button that removes every enemy this spawner placed and lets it spawn again
        if (Application.isPlaying == false) { Debug.LogWarning("Warning! Clear Spawned only works in play mode"); return; }
        for (int i = 0; i < _SpawnedEnemies.Count; i++)
        {
            if (_SpawnedEnemies[i] != null) Destroy(_SpawnedEnemies[i]); //may already be dead and gone
        }
        _SpawnedEnemies.Clear();
        _pickedSpots.Clear();
        _hasSpawned = false;
    }
    #endregion

    #region Debugging
    private void OnDrawGizmos()
    {
        //draws the spawn box in the Scene view. Gizmos.matrix moves and turns everything drawn after it to match this object
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(_GizmoColor.r, _GizmoColor.g, _GizmoColor.b, 0.1f);
        Gizmos.DrawCube(Vector3.zero, _SpawnAreaSize);
        Gizmos.color = _GizmoColor;
        Gizmos.DrawWireCube(Vector3.zero, _SpawnAreaSize);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void OnDrawGizmosSelected()
    {
        //when selected, shows the spacing: each circle is half the min spacing, so two enemies at the minimum distance have touching circles
        Gizmos.color = _GizmoColor;
        float spacingRadius = _MinSpacing * 0.5f;

        if (_pickedSpots.Count == 0)
        {
            Gizmos.DrawWireSphere(transform.position, spacingRadius); //nothing spawned yet, show one sample at the middle
            return;
        }

        for (int i = 0; i < _pickedSpots.Count; i++)
        {
            Gizmos.DrawWireSphere(_pickedSpots[i], spacingRadius);
        }
    }
    #endregion
}

[System.Serializable]
public class SpawnCountEntry
{
    [Tooltip("Which rank of enemy to place")]
    public EnemyType.Rank _Rank;

    [Tooltip("Fewest enemies of this rank to place")]
    [Min(0)] public int _Min;

    [Tooltip("Most enemies of this rank to place")]
    [Min(0)] public int _Max;

    public SpawnCountEntry()
    {
        //empty constructor, used when a new entry is added in the inspector
    }

    public SpawnCountEntry(EnemyType.Rank rank, int min, int max)
    {
        //constructor so the spawner can fill in its default list
        _Rank = rank;
        _Min = min;
        _Max = max;
    }
}

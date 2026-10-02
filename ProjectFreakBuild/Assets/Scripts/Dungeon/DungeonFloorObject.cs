using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.AI.Navigation;
using Sirenix.OdinInspector;

public class DungeonFloorObject : MonoBehaviour
{
    //Runs a dungeon floor's loading steps in order and tells the Scene Manager when the floor is ready to be seen.
    //Only goes in dungeon floor scenes. Scenes without one are treated as ready as soon as they load.

    //the floor in the currently loaded scene. Scene objects (like POI spawners) register themselves with it instead of the floor searching for them
    public static DungeonFloorObject _Floor;

    [Header("Settings")]
    [Tooltip("Build a NavMesh for this floor after the POIs spawn. Turn off for floors that never have AI")]
    [SerializeField] bool _BuildNavMesh = true;

    [Tooltip("Which layers count as walkable ground or walls when building the NavMesh. Untick the layer the player, shades and enemies are on so they don't get baked in as obstacles. Only used if no NavMesh Surface is assigned below")]
    [SerializeField, ShowIf("_BuildNavMesh")] LayerMask _NavMeshLayers = ~0;

    [Tooltip("How far from the check point (in meters) the NavMesh check looks for a NavMesh. Should be a bit more than the player's ride height")]
    [SerializeField, Min(0.1f), ShowIf("_BuildNavMesh")] float _NavMeshCheckDistance = 3f;

    [Tooltip("Print how long each loading step took to the console. Handy for seeing where load time goes")]
    [SerializeField] bool _LogPhaseTimes = true;

    [Header("References")]
    [Tooltip("Optional. A NavMesh Surface to build with, if you want to set its options yourself. If empty, one is added at runtime using the settings above")]
    [SerializeField, ShowIf("_BuildNavMesh")] NavMeshSurface _NavMeshSurface;

    [Tooltip("Optional. Where to check that the NavMesh was built. If empty, checks under the player, wherever the spawn point or door put them")]
    [SerializeField, ShowIf("_BuildNavMesh")] Transform _NavMeshCheckPoint;

    [Header("Events")]
    [Tooltip("Called once the floor is fully loaded, right before the screen fades in. Hook up things like starting music or waking enemies")]
    [SerializeField] UnityEvent _OnFloorReady;

    [Header("Runtime Data")]
    [Tooltip("Which loading step the floor is on (read only)")]
    [SerializeField, ReadOnly] FloorLoadType.Phase _CurrentPhase = FloorLoadType.Phase.NotStarted;

    [Tooltip("The POI spawners that registered with this floor (read only, fills in during play)")]
    [SerializeField, ReadOnly] List<POISpawnerObject> _POISpawners = new List<POISpawnerObject>();

    //"event" is a C# feature: other scripts can subscribe a function to it and get called when the floor finishes
    public event System.Action FloorReady;

    //local variables
    Coroutine _loadRoutine;
    NavMeshData _runtimeNavMeshData; //the NavMesh we build at runtime, kept so it can be cleaned up when the floor unloads
    float _phaseStartTime;

    private void Awake()
    {
        //singleton setup. A floor from the old scene can still be around for a moment during a scene change, so only a second floor in the SAME scene is a mistake
        if (_Floor != null && _Floor != this && _Floor.gameObject.scene == gameObject.scene)
        {
            Debug.LogError($"Error! Two Dungeon Floor objects in {gameObject.scene.name}, turning off {gameObject.name}. Only keep one per floor scene", this);
            enabled = false;
            return;
        }
        _Floor = this;

        //let the Scene Manager know this scene has a floor to wait on before fading in
        if (SceneManagerObject._SceneManager != null) SceneManagerObject._SceneManager.RegisterLoadingFloor(this);
    }

    private void Start()
    {
        //Start() runs after the Scene Manager hears the scene loaded, so it's already waiting on us
        _loadRoutine = StartCoroutine(LoadFloor());
    }

    private void OnDisable()
    {
        //stops loading if the floor gets turned off or unloaded partway through
        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = null;
    }

    private void OnDestroy()
    {
        //clear the singleton, tell the Scene Manager to stop waiting on us, and free the runtime NavMesh so it doesn't pile up in memory
        if (_Floor == this) _Floor = null;
        if (SceneManagerObject._SceneManager != null) SceneManagerObject._SceneManager.UnregisterLoadingFloor(this);
        if (_runtimeNavMeshData != null) Destroy(_runtimeNavMeshData);
    }

    #region Loading
    IEnumerator LoadFloor()
    {
        //function that runs each loading step in order. "yield return" pauses here until the step is done, without freezing the game

        //wait one frame first. Spawners register in their own Start(), and Unity doesn't promise our Start() runs after theirs,
        //but every Start() in the scene is finished by the next frame
        yield return null;

        //Step 1 - POIs
        StartPhase(FloorLoadType.Phase.SpawningPOIs);
        SpawnPOIs();
        yield return null; //wait one frame so the new POIs finish setting themselves up before we read their colliders

        //Step 2 - NavMesh
        if (_BuildNavMesh)
        {
            StartPhase(FloorLoadType.Phase.BuildingNavMesh);
            yield return BuildNavMesh(); //yielding another IEnumerator runs it here and waits until it finishes
            CheckNavMesh();
        }

        //Step 3 - Enemies
        StartPhase(FloorLoadType.Phase.SpawningEnemies);
        //todo: spawn or wake enemies here once the enemy system exists (step 4 of the AI plan)

        //Done
        StartPhase(FloorLoadType.Phase.Ready);
        _loadRoutine = null;
        _OnFloorReady?.Invoke();
        FloorReady?.Invoke();
    }

    void SpawnPOIs()
    {
        //function that tells every POI spawner on this floor to spawn its POI
        for (int i = 0; i < _POISpawners.Count; i++)
        {
            if (_POISpawners[i] == null) continue; //spawner was deleted after we found it, skip it
            _POISpawners[i].SpawnPOI();
        }
    }

    IEnumerator BuildNavMesh()
    {
        //function that builds this floor's NavMesh in the background so the loading animation keeps playing
        SetUpNavMeshSurface();

        //swap out any NavMesh already on the surface for a fresh one we own.
        //building into a new one means we never change a NavMesh baked in the editor (changes to assets made in play mode can stick)
        _NavMeshSurface.RemoveData();
        _runtimeNavMeshData = new NavMeshData(_NavMeshSurface.agentTypeID);
        _runtimeNavMeshData.position = _NavMeshSurface.transform.position; //line the NavMesh up with the surface, the same way Unity's own bake does
        _runtimeNavMeshData.rotation = _NavMeshSurface.transform.rotation;
        _NavMeshSurface.navMeshData = _runtimeNavMeshData;
        _NavMeshSurface.AddData();

        //UpdateNavMesh builds over several frames and hands back an AsyncOperation we can wait on
        AsyncOperation buildOperation = _NavMeshSurface.UpdateNavMesh(_runtimeNavMeshData);
        if (buildOperation == null) { Debug.LogError($"Error! NavMesh build couldn't start on {gameObject.name}", this); yield break; }

        while (buildOperation.isDone == false) yield return null;
    }

    void SetUpNavMeshSurface()
    {
        //function that makes sure we have a NavMesh Surface to build with, adding and setting one up if none was assigned
        if (_NavMeshSurface == null)
        {
            _NavMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            _NavMeshSurface.collectObjects = CollectObjects.All; //look at everything loaded
            _NavMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; //build from colliders, faster and cleaner than render meshes
            _NavMeshSurface.layerMask = _NavMeshLayers;
        }

        //the player is always loaded, so if its layer is included it gets baked in as an obstacle and cuts a hole where it's standing
        if (Player.player != null && LayerIsInMask(Player.player.gameObject.layer, _NavMeshSurface.layerMask))
            Debug.LogWarning($"Warning! The player's layer is included in the NavMesh layers on {gameObject.name}, so units get baked in as obstacles. Put units on their own layer and untick it. Building anyway...", this);
    }

    void CheckNavMesh()
    {
        //function that checks a NavMesh actually got built where the player starts
        Transform checkPoint = GetNavMeshCheckPoint();
        if (checkPoint == null) { Debug.LogWarning($"Warning! No NavMesh check point or player found for {gameObject.name}, skipping the NavMesh check...", this); return; }

        //SamplePosition looks for the closest point on any NavMesh within the given distance
        if (NavMesh.SamplePosition(checkPoint.position, out NavMeshHit hit, _NavMeshCheckDistance, NavMesh.AllAreas) == false)
            Debug.LogError($"Error! No NavMesh found within {_NavMeshCheckDistance}m of {checkPoint.name} on {gameObject.scene.name}. Check the NavMesh layers and that the floor has colliders", this);
    }

    void StartPhase(FloorLoadType.Phase newPhase)
    {
        //function that moves to the next loading step and logs how long the last one took
        if (_LogPhaseTimes && _CurrentPhase != FloorLoadType.Phase.NotStarted)
        {
            float phaseTime = (Time.realtimeSinceStartup - _phaseStartTime) * 1000f; //realtimeSinceStartup keeps counting even if the game is paused
            Debug.Log($"{gameObject.scene.name}: {_CurrentPhase} took {phaseTime:F0}ms");
        }

        _CurrentPhase = newPhase;
        _phaseStartTime = Time.realtimeSinceStartup;
    }
    #endregion

    #region Registration
    public void RegisterPOISpawner(POISpawnerObject spawner)
    {
        //called by a POI spawner when it loads in, so the floor knows to spawn it during loading
        if (spawner == null || _POISpawners.Contains(spawner)) return;

        //a spawner that shows up after the POI step already ran (like one inside a POI) spawns right away, but it missed the NavMesh build
        if (_CurrentPhase != FloorLoadType.Phase.NotStarted && _CurrentPhase != FloorLoadType.Phase.SpawningPOIs)
        {
            Debug.LogWarning($"Warning! {spawner.gameObject.name} registered after the POI step on {gameObject.scene.name}, spawning it now but it won't be in the NavMesh...", spawner);
            spawner.SpawnPOI();
            return;
        }

        _POISpawners.Add(spawner);
    }
    #endregion

    #region Tools
    public bool IsFloorReady()
    {
        //function the Scene Manager uses to check if the floor finished loading
        return _CurrentPhase == FloorLoadType.Phase.Ready;
    }

    public FloorLoadType.Phase GetCurrentPhase()
    {
        //function for checking which step the floor is on, used in the Scene Manager's timeout error
        return _CurrentPhase;
    }

    Transform GetNavMeshCheckPoint()
    {
        //function that picks where to run the NavMesh check: the assigned point, or wherever the player is standing (already moved by the spawn point or door)
        if (_NavMeshCheckPoint != null) return _NavMeshCheckPoint;
        if (Player.player != null) return Player.player.transform;
        return null;
    }

    bool LayerIsInMask(int layer, LayerMask mask)
    {
        //function that checks if a layer is ticked in a layer mask. A layer mask stores each layer as one bit, so we shift a 1 over to that layer's bit and check it
        return (mask.value & (1 << layer)) != 0;
    }
    #endregion
}

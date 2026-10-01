using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

public class SceneManagerObject : MonoBehaviour
{
    [FoldoutGroup ("Opening Scene Data")]
    [SerializeField, Tooltip("List index tied to what chapter the save slot is on. Will load what value that chapter is set to. Default 0")] 
    List<string> _loadSceneOpeningByChapterIndex;

    [FoldoutGroup("Scene Data")]
    [SerializeField] string _currentSceneName;

    [FoldoutGroup("Location Change")]
    [SerializeField] bool _fadeInTransition;
    [FoldoutGroup("Location Change")]
    [SerializeField] float _fadeInTransitionSpeed;
    [FoldoutGroup("Location Change")]
    [SerializeField, Tooltip("world space location where the player is going to move to")] Vector3 _PlayerMoveLocation;
    [FoldoutGroup("Location Change")]
    [SerializeField] SceneLocationSO _PlayerMoveTarget;

    [FoldoutGroup("Floor Loading")]
    [SerializeField, Min(1f), Tooltip("Longest the fade-in will wait for a dungeon floor to finish loading, in seconds. If it runs out, an error is logged and the screen fades in anyway so the player is never stuck on black")]
    float _FloorLoadTimeout = 10f;

    //local variables
    DungeonFloorObject _loadingFloor; //the dungeon floor that registered itself in the scene being loaded, null in non-dungeon scenes
    Coroutine _floorWaitTimer;

    public static SceneManagerObject _SceneManager;

    private UnityEngine.SceneManagement.Scene _currentOpeningScene;

    #region Setup
    private void Awake()
    {
        if (_SceneManager == null) _SceneManager = this;
    }
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    #region Opening Scripts
    public void LoadStartScene()
    {
        //this should never happen but just in case
        if (_loadSceneOpeningByChapterIndex.Count == 0 || GameManager._GameManager == null) return;

        //function that loads the initial ui screen when the game starts up
        //this allows the opening scene to be different based on what chapter the player is on
        SaveManager _sm = GameManager._GameManager.GetComponent<SaveManager>();
        int _loadIndex;

        //make sure the chapter is in the list, else default to original load
        if (_loadSceneOpeningByChapterIndex.Count - 1 < _sm._saveSlotList[_sm.GetCurrentActiveSaveSlot()]._SaveChapter) _loadIndex = 0;
        else _loadIndex = _sm._saveSlotList[_sm.GetCurrentActiveSaveSlot()]._SaveChapter;


        StartCoroutine(LoadOpeningScene(_loadSceneOpeningByChapterIndex[_loadIndex]));
    }

    private IEnumerator LoadOpeningScene(string _sceneName)
    {
        //Load the scene additively so the initial scene stays loaded
        AsyncOperation _operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
            _sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Additive
        );

        //Wait until the scene has finished loading
        while (!_operation.isDone)
        {
            yield return null;
        }

        //Get the scene we just loaded
        _currentOpeningScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(_sceneName);

        //Disable all root objects so the scene is loaded but not visible/active
        GameObject[] _rootObjects = _currentOpeningScene.GetRootGameObjects();

        foreach (GameObject _rootObject in _rootObjects)
        {
            _rootObject.SetActive(false);
        }
    }

    public void ActivateOpeningScene()
    {
        if (!_currentOpeningScene.IsValid())
        {
            Debug.LogWarning("No opening scene is currently loaded");
            return;
        }

        GameObject[] _rootObjects = _currentOpeningScene.GetRootGameObjects();

        foreach (GameObject _rootObject in _rootObjects)
        {
            _rootObject.SetActive(true);
        }
    }
    #endregion

    #region Scene Selection
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_fadeInTransition == false) return;
        _fadeInTransition = false;

        //dungeon floors still have to spawn POIs and build their NavMesh, so wait for them before fading in
        //any other scene (no floor object) is ready as soon as it loads, same as before
        if (_loadingFloor != null && _loadingFloor.gameObject.scene == scene && _loadingFloor.IsFloorReady() == false)
        {
            if (_floorWaitTimer != null) StopCoroutine(_floorWaitTimer);
            _floorWaitTimer = StartCoroutine(WaitForFloorThenFade(_loadingFloor, _fadeInTransitionSpeed));
            return;
        }

        FadeInHUD(_fadeInTransitionSpeed);
    }

    IEnumerator WaitForFloorThenFade(DungeonFloorObject floor, float fadeSpeed)
    {
        //function that waits for a dungeon floor to finish loading, then fades in. Gives up after the timeout so the player isn't stuck on a black screen
        float timer = 0f;
        string sceneName = floor.gameObject.scene.name; //saved now in case the floor gets destroyed while we wait

        while (floor != null && floor.IsFloorReady() == false)
        {
            timer += Time.unscaledDeltaTime; //unscaled so pausing the game doesn't stop the timeout
            if (timer >= _FloorLoadTimeout)
            {
                Debug.LogError($"Error! {sceneName} didn't finish loading within {_FloorLoadTimeout}s (stuck on {floor.GetCurrentPhase()}), fading in anyway", floor);
                break;
            }
            yield return null;
        }

        _floorWaitTimer = null;
        FadeInHUD(fadeSpeed);
    }

    void FadeInHUD(float fadeSpeed)
    {
        //function that fades the screen in, with a check so a missing HUD doesn't throw an error
        if (HUDManager._HUD == null) { Debug.LogError("Error! HUD Manager not found, can't fade the screen in", this); return; }
        HUDManager._HUD.FadeIn(fadeSpeed);
    }

    public void ChangeScene(string sceneName)
    {
        _currentSceneName = sceneName;
        SceneManager.LoadScene(_currentSceneName);
    }

    public void SceneLocationDataChange(SceneLocationSO data)
    {
        if (data._IsLinked)
        {
            _PlayerMoveTarget = data._Link;
            ChangeScene(data._Scene);
        }
        else
        {
            MovePlayerToLocation(data._LinkLocation, Player.player.gameObject.transform.rotation);
        }
        
    }

    public void RegisterLoadingFloor(DungeonFloorObject floor)
    {
        //called by a DungeonFloorObject when its scene loads so the fade-in knows to wait for it
        _loadingFloor = floor;
    }

    public void UnregisterLoadingFloor(DungeonFloorObject floor)
    {
        //called when a DungeonFloorObject is destroyed so we don't hold on to a floor from an old scene
        if (_loadingFloor == floor) _loadingFloor = null;
    }

    public void HudFadeOnOpen(float timing)
    {
        //when called, ensures the next time a scene is loaded the hud will fade in
        _fadeInTransition = true;
        _fadeInTransitionSpeed = timing;
    }
    #endregion

    public bool TestDoorEntranceTarget(SceneLocationSO data)
    {
        if (data == _PlayerMoveTarget)
        {
            return true;
        }
        return false;
    }

    public void MovePlayerToLocation(Vector3 location, Quaternion rotation)
    {
        Player.player.gameObject.transform.position = location;
        Player.player.gameObject.transform.rotation = rotation;
    }
}

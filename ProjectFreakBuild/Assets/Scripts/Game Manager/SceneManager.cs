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

    [FoldoutGroup("Rune Field")]
    [SerializeField, Tooltip("Name of the rune field scene that access points load. It has to be in File > Build Settings")]
    string _RuneFieldScene = "3d_Evolution_UI_scene";
    [FoldoutGroup("Rune Field")]
    [SerializeField, Min(0f), Tooltip("Seconds the screen takes to fade to black before going to or from the rune field. Skipped if the screen is already black (like when the access point's timeline faded it). 0 = no fade")]
    float _RuneFieldFadeOutTime = 0.5f;
    [FoldoutGroup("Rune Field")]
    [SerializeField, Min(0f), Tooltip("Seconds the screen takes to fade back in once the rune field (or the scene coming back from it) has loaded")]
    float _RuneFieldFadeInTime = 1f;
    [FoldoutGroup("Rune Field")]
    [SerializeField, ReadOnly, Tooltip("The scene the player opened the rune field from, so leaving the field goes back there (read only). Empty when the field wasn't opened from an access point")]
    string _RuneFieldReturnScene;

    //local variables
    DungeonFloorObject _loadingFloor; //the dungeon floor that registered itself in the scene being loaded, null in non-dungeon scenes
    Coroutine _floorWaitTimer;
    Coroutine _runeFieldTrip; //the fade and scene change to or from the rune field, so a second press can't start another one
    Vector3 _runeFieldReturnPosition; //where the player goes back to after leaving the rune field
    Quaternion _runeFieldReturnRotation;
    bool _returningFromRuneField; //true while the scene we came from is loading, so the player gets put back at the access point

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
        //coming back from the rune field: put the player back at the access point first
        //sceneLoaded runs after the new scene's Awake and OnEnable, so this wins over its spawn points and dungeon doors
        if (_returningFromRuneField && scene.name == _RuneFieldReturnScene) PutPlayerBackFromRuneField();

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

    #region Rune Field Trips
    public void TravelToRuneField(Vector3 returnPosition, Quaternion returnRotation)
    {
        //function access points use (through RuneFieldAccessWrapper) to swap to the rune field scene
        //remembers this scene and the spot to come back to, so leaving the field returns the player here
        if (_runeFieldTrip != null) return; //already on the way
        if (string.IsNullOrEmpty(_RuneFieldScene)) { Debug.LogError($"Error! No rune field scene set on {gameObject.name} (Rune Field foldout)", this); return; }
        if (Application.CanStreamedLevelBeLoaded(_RuneFieldScene) == false) { Debug.LogError($"Error! The rune field scene {_RuneFieldScene} isn't in File > Build Settings, so it can't be loaded", this); return; }

        _RuneFieldReturnScene = SceneManager.GetActiveScene().name;
        _runeFieldReturnPosition = returnPosition;
        _runeFieldReturnRotation = returnRotation;

        //shades get put away while their fields are being edited (also stops the player being stuck driving a released shade)
        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.ReturnShade();

        _runeFieldTrip = StartCoroutine(FadeThenChangeScene(_RuneFieldScene, true));
    }

    public void LeaveRuneField()
    {
        //function the rune field's On Leave calls (through SceneManagerWrapper) to go back to the scene and access point the player came from
        if (_runeFieldTrip != null) return; //already on the way
        if (string.IsNullOrEmpty(_RuneFieldReturnScene)) { Debug.LogWarning("Warning! The rune field wasn't opened from an access point, so there's no scene to go back to. Staying in the field...", this); return; }
        if (Application.CanStreamedLevelBeLoaded(_RuneFieldReturnScene) == false) { Debug.LogError($"Error! {_RuneFieldReturnScene} isn't in File > Build Settings, so the rune field can't go back to it", this); return; }

        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.SetFieldAccess(RuneFieldType.Access.None); //the field is closing, so nothing is limited any more
        _returningFromRuneField = true;
        _runeFieldTrip = StartCoroutine(FadeThenChangeScene(_RuneFieldReturnScene, false));
    }

    IEnumerator FadeThenChangeScene(string sceneName, bool hidePlayer)
    {
        //function that fades to black, then loads the scene. The fade back in happens once it's loaded (HudFadeOnOpen)
        bool alreadyBlack = HUDManager._HUD != null && HUDManager._HUD.GetCurrentFadeValue() >= 1f; //the access point's timeline may have faded already
        if (_RuneFieldFadeOutTime > 0f && HUDManager._HUD != null && alreadyBlack == false)
        {
            HUDManager._HUD.FadeOut(_RuneFieldFadeOutTime);
            yield return new WaitForSeconds(_RuneFieldFadeOutTime);
        }

        //the player isn't in the rune field, so they're hidden (not destroyed, they carry over between scenes) until they come back
        if (hidePlayer && Player.player != null) Player.player.gameObject.SetActive(false);

        _runeFieldTrip = null;
        HudFadeOnOpen(_RuneFieldFadeInTime);
        ChangeScene(sceneName);
    }

    void PutPlayerBackFromRuneField()
    {
        //function that moves the player to the access point they left from and shows them again
        _returningFromRuneField = false;
        _RuneFieldReturnScene = "";
        if (Player.player == null) { Debug.LogWarning("Warning! No player found coming back from the rune field, nobody to put back...", this); return; }

        MovePlayerToLocation(_runeFieldReturnPosition, _runeFieldReturnRotation); //moved while still hidden, so there's no one-frame pop at the old spot
        Player.player.gameObject.SetActive(true);
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

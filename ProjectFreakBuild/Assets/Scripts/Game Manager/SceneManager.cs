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
    public void loadStartScene()
    {
        Debug.Log("Testing: " + _loadSceneOpeningByChapterIndex.Count);
        //this should never happen but just in case
        if (_loadSceneOpeningByChapterIndex.Count == 0 || GameManager._GameManager == null) return;

        //function that loads the initial ui screen when the game starts up
        //this allows the opening scene to be different based on what chapter the player is on
        SaveManager _sm = GameManager._GameManager.GetComponent<SaveManager>();
        int _loadIndex;

        //make sure the chapter is in the list, else default to original load
        if (_loadSceneOpeningByChapterIndex.Count - 1 < _sm._saveSlotList[_sm.getCurrentActiveSaveSlot()]._SaveChapter) _loadIndex = 0;
        else _loadIndex = _sm._saveSlotList[_sm.getCurrentActiveSaveSlot()]._SaveChapter;


        StartCoroutine(loadOpeningScene(_loadSceneOpeningByChapterIndex[_loadIndex]));
    }

    private IEnumerator loadOpeningScene(string _sceneName)
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

    public void activateOpeningScene()
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
        Debug.Log($"Scene loaded: {scene.name}");
        Debug.Log($"Loading mode: {mode}");
        if (_fadeInTransition == false) return;
        HUDManager._HUD.FadeIn(_fadeInTransitionSpeed);
        _fadeInTransition = false;
    }

    public void changeScene(string sceneName)
    {
        _currentSceneName = sceneName;
        SceneManager.LoadScene(_currentSceneName);
    }

    public void sceneLocationDataChange(SceneLocationSO data)
    {
        if (data._IsLinked)
        {
            _PlayerMoveTarget = data._Link;
            changeScene(data._Scene);
        }
        else
        {
            movePlayerToLocation(data._LinkLocation, Player.player.gameObject.transform.rotation);
        }
        
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
            print("Found matching door!");
            return true;
        }
        return false;
    }

    public void movePlayerToLocation(Vector3 location, Quaternion rotation)
    {
        Player.player.gameObject.transform.position = location;
        Player.player.gameObject.transform.rotation = rotation;
    }
}

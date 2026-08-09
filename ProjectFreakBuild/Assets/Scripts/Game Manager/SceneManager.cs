using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField, Tooltip("List index tied to what chapter the save slot is on. Will load what value that chapter is set to. Default 0")] 
    List<string> _loadSceneOpeningByChapterIndex;

    private UnityEngine.SceneManagement.Scene _currentOpeningScene;

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

        Debug.Log("Finished loading opening scene: " + _sceneName);
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

        Debug.Log("Activated opening scene");
    }
}

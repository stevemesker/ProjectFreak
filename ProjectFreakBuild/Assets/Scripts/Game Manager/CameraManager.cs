using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager _CamManager;
    [Header("Settings")]
    public GameObject _gameplayCameraPrefab;
    public Vector3 _followOffset;

    [Header("Realtime data")]
    public GameObject _currentGameplayCamera;
    private CinemachineVirtualCamera _followCam;
    private CinemachineTransposer _transposer;

    private void Awake()
    {
        if (_CamManager != null) return;
        _CamManager = this;
        if (_currentGameplayCamera == null)
        {
            _currentGameplayCamera = Instantiate(_gameplayCameraPrefab);
            print("Spawning gameplay camera... Camera name" + _currentGameplayCamera.name);
        }
    }

    private void Start()
    {
        print("enableing virtual camera");
        _followCam = _currentGameplayCamera.GetComponent<CinemachineVirtualCamera>();
        _transposer = _followCam.GetCinemachineComponent<CinemachineTransposer>();

        _followCam.Priority = 0;
        _transposer.m_FollowOffset = _followOffset;
        setCamTargetToPlayer();
    }

    public void setCamTargetToPlayer()
    {
        if (Player.player != null)
        {
            _followCam.Follow = Player.player.transform;
            _followCam.LookAt = Player.player.transform;
            Player.player.GetComponent<CharacterMovement>()._MainCamera = _currentGameplayCamera;
        }
    }

    public void setGameplayCameraPriority(int priority)
    {
        _followCam.Priority = priority;
    }
}

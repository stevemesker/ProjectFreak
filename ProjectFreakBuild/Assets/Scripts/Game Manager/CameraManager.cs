using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;

public class CameraManager : MonoBehaviour
{
    public static CameraManager _CamManager;
    [Header("Settings")]
    public GameObject _gameplayCameraPrefab;
    public Vector3 _followOffset;

    [Header("Runtime data")]
    public GameObject _currentGameplayCamera;
    public GameObject _currentFollowTarget;
    private CinemachineVirtualCamera _followCam;
    private CinemachineTransposer _transposer;

    [Header("Private")]
    CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        if (_CamManager != null) return;
        _CamManager = this;
        if (_currentGameplayCamera == null)
        {
            _currentGameplayCamera = Instantiate(_gameplayCameraPrefab);
            
            print("Spawning gameplay camera... Camera name" + _currentGameplayCamera.name);
        }
        _impulseSource = _currentGameplayCamera.GetComponent<CinemachineImpulseSource>();
        print(_impulseSource);
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
            _currentFollowTarget = Player.player.gameObject;
        }
    }

    public void setGameplayCameraPriority(int priority)
    {
        _followCam.Priority = priority;
    }

    #region Camera Shake
    [Button("Test Shake")]
    void testShake(float force)
    {
        CombatCameraShake(force, null);
    }

    public void CombatCameraShake(float force, CinemachineImpulseSource impulseSource)
    {
        CinemachineImpulseSource temp = impulseSource;
        if (temp == null) temp = _impulseSource;
        print(temp);
        //dampen force here
        CameraShake(force, temp);
    }

    public void CameraShake(float force, CinemachineImpulseSource impulseSource)
    {
        CinemachineImpulseSource temp = impulseSource;
        if (temp == null) temp = _impulseSource;
        temp.GenerateImpulseWithForce(force);
    }

    #endregion
}

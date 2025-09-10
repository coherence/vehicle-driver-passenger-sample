using System.Linq;
using Coherence.Connection;
using Coherence.Toolkit;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private CinemachineCamera playerFollowCamera;
    
    private CoherenceBridge _coherenceBridge;
    private GameObject _playerReference;
    private Transform _camFollowTarget;

    private void OnEnable()
    {
        _coherenceBridge = FindFirstObjectByType<CoherenceBridge>();
        _coherenceBridge.ClientConnections.OnSynced += SpawnPlayer;
        _coherenceBridge.onDisconnected.AddListener(OnDisconnected);
    }

    private void OnDisable()
    {
        _coherenceBridge.onDisconnected.RemoveListener(OnDisconnected);
    }

    private void SpawnPlayer(CoherenceClientConnectionManager coherenceClientConnectionManager)
    {
        _coherenceBridge.ClientConnections.OnSynced -= SpawnPlayer;
        
        // Spawn the player with a Prefab based on how many clients are connected so far
        int n = (_coherenceBridge.ClientConnections.GetAll().Count() - 1) % playerPrefabs.Length;
        _playerReference = Instantiate(playerPrefabs[n], transform.position, transform.rotation);
        
        // Connect the Cinemachine Camera
        _camFollowTarget = _playerReference.GetComponent<ThirdPersonController>().CinemachineCameraTarget.transform;
        playerFollowCamera.Target.TrackingTarget = _camFollowTarget;
        
        // Lock the mouse cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisconnected(CoherenceBridge bridge, ConnectionCloseReason reason)
    {
        // Destroy the player game object
        Destroy(_playerReference);
        
        // Unlock the cursor
        Cursor.lockState = CursorLockMode.None;
    }
}

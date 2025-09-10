using Coherence.Connection;
using Coherence.Toolkit;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerNetworkController : MonoBehaviour
{
    [SerializeField] private GameObject playerFollowCamera;
    private CoherenceSync coherenceSync;
    
    private void OnEnable()
    {
        coherenceSync = GetComponent<CoherenceSync>();
        coherenceSync.CoherenceBridge.onConnected.AddListener(OnPlayerConnected);
        coherenceSync.CoherenceBridge.onDisconnected.AddListener(OnPlayerDisconnected);
    }

    private void OnDisable()
    {
        coherenceSync.CoherenceBridge.onConnected.RemoveListener(OnPlayerConnected);
        coherenceSync.CoherenceBridge.onDisconnected.RemoveListener(OnPlayerDisconnected);
    }

    private void OnPlayerConnected(CoherenceBridge bridge)
    {
        EnableCamera();
    }

    private void OnPlayerDisconnected(CoherenceBridge bridge, ConnectionCloseReason reason)
    {
        DisableCamera();
    }
    
    private void EnableCamera()
    {
        playerFollowCamera.SetActive(true);
    }

    private void DisableCamera()
    {
        playerFollowCamera.SetActive(false);
    }
}

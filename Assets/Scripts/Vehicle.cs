using System;
using Coherence;
using Coherence.Toolkit;
using Unity.Cinemachine;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    [SerializeField] private Transform _driverSeat;
    [SerializeField] private Transform _passengerSeat;
    [SerializeField] private CinemachineCamera _driverCamera;
    [SerializeField] private CinemachineCamera _passengerCamera;
    [SerializeField] private HighlightOutline _outlineScript;
    
    [Sync] public bool hasDriver;
    [Sync] public bool hasPassenger;

    public event Action<InteractionResponse> RespondToInteraction;
    
    private VehicleInputs _inputs;
    private CoherenceSync _sync;
    private bool _awaitingAuthority;

    public Transform GetDriverSeat => _driverSeat;
    public Transform GetPassengerSeat => _passengerSeat;

    private void Awake()
    {
        _sync = GetComponent<CoherenceSync>();
        _inputs = GetComponent<VehicleInputs>();
    }

    private void OnEnable()
    {
        _inputs.enabled = false;
        _sync.OnAuthorityRequest.AddListener(OnAuthorityRequested);
        _sync.OnStateAuthority.AddListener(OnAuthorityGranted);
        _sync.OnAuthorityRequestRejected.AddListener(OnAuthorityRejected);
    }

    private void OnDisable()
    {
        _sync.OnAuthorityRequest.RemoveListener(OnAuthorityRequested);
        _sync.OnStateAuthority.RemoveListener(OnAuthorityGranted);
        _sync.OnAuthorityRequestRejected.RemoveListener(OnAuthorityRejected);
    }

    public void Highlight()
    {
        if(!hasDriver || !hasPassenger) { _outlineScript.HighlightAvailable(); }
        else { _outlineScript.HighlightUnavailable(); }
    }
    
    public void RemoveHighlight()
    {
        _outlineScript.RemoveHighlight();
    }
    
    public void RequestInteraction(CoherenceSync requester)
    {
        if (!hasDriver) TryAddDriver();
        else if (!hasPassenger) TryAddPassenger(requester);
        else RejectControl();
    }

    private void TryAddDriver()
    {
        if (_sync.HasStateAuthority) ConfirmDriver();
        else
        {
            _awaitingAuthority = true;
            _sync.RequestAuthority(AuthorityType.Full);
        }
    }

    private void TryAddPassenger(CoherenceSync requester)
    {
        if (_sync.HasStateAuthority) ConfirmPassenger();
        else
        {
            _sync.SendCommand<Vehicle>(nameof(RequestBoardingAsPassenger), MessageTarget.AuthorityOnly, requester);
        }
    }

    [Command]
    public void RequestBoardingAsPassenger(CoherenceSync requester)
    {
        requester.SendCommand<PlayerBrain>(nameof(PlayerBrain.OnRemoteVehicleResponded), MessageTarget.AuthorityOnly,
            !hasPassenger);

        ConfirmPassenger();
    }

    private void OnAuthorityRequested(AuthorityRequest request, CoherenceSync coherenceSync)
    {
        if(!hasDriver) request.Accept();
        else request.Reject();
    }

    private void OnAuthorityGranted()
    {
        if (_awaitingAuthority)
        {
            ConfirmDriver();
            _awaitingAuthority = false;
        }
        else
        {
            hasDriver = false;
        }
    }
    
    private void OnAuthorityRejected(AuthorityType arg0)
    {
        RejectControl();
        _awaitingAuthority = false;
    }

    private void ConfirmDriver()
    {
        hasDriver = true;
        _driverCamera.Priority = 100;
        _inputs.enabled = true;
        if (transform.up.y < 0.5f)
        {
            // Reset, in case it's upside down
            transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        }
        RespondToInteraction?.Invoke(InteractionResponse.Drive);
    }
    
    private void ConfirmPassenger()
    {
        hasPassenger = true;
    }

    public void BoardAsPassenger()
    {
        _passengerCamera.Priority = 100;
    }

    public void RemoveDriver()
    {
        hasDriver = false;
        _inputs.enabled = false;
        _driverCamera.Priority = 0;
        RespondToInteraction?.Invoke(InteractionResponse.Exit);
    }

    public void RemovePassenger()
    {
        _sync.SendCommand<Vehicle>(nameof(RemovePassengerOnAuthority), MessageTarget.AuthorityOnly);
        _passengerCamera.Priority = 0;
        RespondToInteraction?.Invoke(InteractionResponse.Exit);
    }
    
    [Command]
    public void RemovePassengerOnAuthority()
    {
        hasPassenger = false;
    }

    // Vehicle is full
    private void RejectControl()
    {
        RespondToInteraction?.Invoke(InteractionResponse.Refused);
    }

    public enum InteractionResponse
    {
        Drive,
        Exit,
        Passenger,
        Refused,
    }
}
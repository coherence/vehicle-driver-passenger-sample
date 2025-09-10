using Coherence.Toolkit;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    [SerializeField] private InputActionReference _interactAction;
    
    [SerializeField] private State _state;
    
    private StarterAssetsInputs _input;
    private CharacterController _characterController;
    private ThirdPersonController _thirdPersonController;
    private Animator _animator;
    private CoherenceSync _sync;
    
    private Vehicle _targetVehicle;

    private void Awake()
    {
        _sync = GetComponent<CoherenceSync>();
        _input = GetComponent<StarterAssetsInputs>();
        _characterController = GetComponent<CharacterController>();
        _thirdPersonController = GetComponent<ThirdPersonController>();
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _interactAction.action.Enable();
        _interactAction.action.performed += OnInteractInput;
    }
    
    private void OnDisable()
    {
        _interactAction.action.performed -= OnInteractInput;
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Walking:
                Vector3 rayPos = transform.position + Vector3.up * 2f;
                if (Physics.Raycast(rayPos, transform.forward, out RaycastHit hit, 5f))
                {
                    Rigidbody hitRigidbody = hit.rigidbody;
                    if (hitRigidbody != null && hitRigidbody.TryGetComponent(out Vehicle vehicle))
                    {
                        _targetVehicle = vehicle;
                        vehicle.Highlight();
                    }
                }
                else
                {
                    if (_targetVehicle != null)
                    {
                        _targetVehicle.RespondToInteraction -= OnVehicleResponded;
                        _targetVehicle.RemoveHighlight();
                        _targetVehicle = null;
                    }
                }
                break;
        }
    }

    private void OnInteractInput(InputAction.CallbackContext obj)
    {
        switch (_state)
        {
            case State.Walking:
                if (_targetVehicle != null)
                {
                    _targetVehicle.RespondToInteraction += OnVehicleResponded;
                    _targetVehicle.RequestInteraction(_sync);
                }
                break;
            
            case State.Passenger:
                LeaveVehicleAsPassenger();
                break;
                
            case State.Driving:
                LeaveVehicle();
                break;
        }
    }

    private void OnVehicleResponded(Vehicle.InteractionResponse response)
    {
        _targetVehicle.RespondToInteraction -= OnVehicleResponded;
        
        switch (response)
        {
            case Vehicle.InteractionResponse.Drive:
                _state = State.Driving;
                SetCharacterScripts(false);
                transform.position = _targetVehicle.GetDriverSeat.position;
                transform.rotation = _targetVehicle.GetDriverSeat.rotation;
                transform.SetParent(_targetVehicle.GetDriverSeat);
                _targetVehicle.RemoveHighlight();
                break;
            
            case Vehicle.InteractionResponse.Passenger:
                _state = State.Passenger;
                SetCharacterScripts(false);
                transform.position = _targetVehicle.GetPassengerSeat.position;
                transform.rotation = _targetVehicle.GetPassengerSeat.rotation;
                transform.SetParent(_targetVehicle.GetPassengerSeat);
                _targetVehicle.RemoveHighlight();
                _targetVehicle.BoardAsPassenger();
                break;
                
            case Vehicle.InteractionResponse.Exit:
                _state = State.Walking;
                transform.position = _targetVehicle.transform.position + _targetVehicle.transform.right * -3f + Vector3.up;
                transform.SetParent(null);
                transform.rotation = Quaternion.LookRotation(_targetVehicle.transform.forward, Vector3.up);
                SetCharacterScripts(true);
                _thirdPersonController.OrientCamera(_targetVehicle.transform.forward);
                _targetVehicle = null;
                break;
                
            case Vehicle.InteractionResponse.Refused:
                break;
        }
    }

    [Command]
    public void OnRemoteVehicleResponded(bool canEnter)
    {
        OnVehicleResponded(canEnter ? Vehicle.InteractionResponse.Passenger : Vehicle.InteractionResponse.Refused);
    }
    
    private void SetCharacterScripts(bool enable)
    {
        _input.enabled = enable;
        _characterController.enabled = enable;
        _thirdPersonController.enabled = enable;
        _animator.SetBool("IsDriving", !enable);
    }

    private void LeaveVehicle()
    {
        _targetVehicle.RespondToInteraction += OnVehicleResponded;
        _targetVehicle.RemoveDriver();
    }

    private void LeaveVehicleAsPassenger()
    {
        _targetVehicle.RespondToInteraction += OnVehicleResponded;
        _targetVehicle.RemovePassenger();
    }

    private enum State
    {
        Walking,
        Driving,
        Passenger,
    }
}
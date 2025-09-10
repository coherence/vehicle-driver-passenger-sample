using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleInputs : MonoBehaviour
{
    public VehicleController vehicleController;
    
    public InputActionReference accelerationAction;
    public InputActionReference steeringAction;
    public InputActionReference resetAction;

    private void OnEnable()
    {
        accelerationAction.action.actionMap.Enable();
        
        steeringAction.action.performed += OnSteer;
        steeringAction.action.canceled += OnStoppedSteering;
        resetAction.action.performed += OnVehicleReset;
    }

    private void OnDisable()
    {
        accelerationAction.action.actionMap.Disable();
        
        steeringAction.action.performed -= OnSteer;
        steeringAction.action.canceled -= OnStoppedSteering;
        resetAction.action.performed -= OnVehicleReset;
        
        // Clean inputs
        vehicleController.AccelerationInput(0f);
        vehicleController.SteeringInput(0f);
    }

    private void Update()
    {
        float acceleration = accelerationAction.action.ReadValue<float>();
        vehicleController.AccelerationInput(acceleration);
    }
    
    private void OnSteer(InputAction.CallbackContext obj) => vehicleController.SteeringInput(obj.ReadValue<float>());

    private void OnStoppedSteering(InputAction.CallbackContext obj) => vehicleController.SteeringInput(0f);

    private void OnVehicleReset(InputAction.CallbackContext _)
    {
        vehicleController.ResetVehicle();
    }
}

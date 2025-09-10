using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Rigidbody carRigidBody;
    [SerializeField] private Transform carTransform;
    [SerializeField] private VehicleWheel[] tires;
    [SerializeField] private Transform frontLeftTire;
    [SerializeField] private Transform frontRightTire;
    [SerializeField] private Transform rearLeftTire;
    [SerializeField] private Transform rearRightTire;
    
    [Header("Vehicle Settings")]
    [SerializeField] private float carTopSpeed;
    [SerializeField] private float accelerationPower;
    [SerializeField] private AnimationCurve torquePowerCurve;
    [SerializeField] private AnimationCurve reverseTorquePowerCurve;
    [SerializeField] private AnimationCurve brakePowerCurve;
    [SerializeField, Range(0.0f, 1.0f)] private float reverseSpeedLimit;
    [SerializeField] private bool useFourWheelSteering;
    [SerializeField] private bool crabSteerAtHighSpeeds;
    [SerializeField] private AnimationCurve fourWheelSteeringCurve;
    [SerializeField, Range(0.0f, 1.0f)] private float innerWheelTurningPower = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float frontDrivePowerFactor = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float rearDrivePowerFactor = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float restfulGripFactor = 0.5f;
    
    [Header("Tire Settings")]
    [SerializeField] private float tireMass;
    [SerializeField] private bool useGripCurves = true;
    [SerializeField] private bool useDistinctFrontAndRearGrip = true;
    [SerializeField, Range(0.0f, 1.0f)] private float frontTireGripStrength;
    [SerializeField, Range(0.0f, 1.0f)] private float rearTireGripStrength;
    [SerializeField] private AnimationCurve frontTireGripCurve;
    [SerializeField] private AnimationCurve rearTireGripCurve;
    
    [Header("Suspension Settings")]
    [SerializeField] private float suspensionRestDistance;
    [SerializeField] private float springStrength;
    [SerializeField] private float springDamper;
    [SerializeField] private float lowerSuspensionExtentLimit;

    [Header("Inputs")]
    public float accelerationInput;
    public float steeringInput;
    
    private Quaternion frontLeftTireRotation;
    private Quaternion frontRightTireRotation;
    private Quaternion rearLeftTireRotation;
    private Quaternion rearRightTireRotation;
    private float rotationCompletion;
    private float rotationSpeed = 5.0f;
    
    private float tireHeightBasis;
    private float tireBaseOffset;
    
    private bool turningLeft;
    private bool turningRight;
    private bool lowSpeedSteer;

    private void ResetSteeringCompletion()
    {
        rotationCompletion = 0.0f;
        frontLeftTireRotation = frontLeftTire.localRotation;
        frontRightTireRotation = frontRightTire.localRotation;
        rearLeftTireRotation = rearLeftTire.localRotation;
        rearRightTireRotation = rearRightTire.localRotation;
    }
    
    private void Awake()
    {
        tireHeightBasis = frontLeftTire.position.y - transform.position.y;
        tireBaseOffset = -frontLeftTire.localPosition.y;

        if (useDistinctFrontAndRearGrip) return;
        rearTireGripStrength = frontTireGripStrength;
        rearTireGripCurve = frontTireGripCurve;
    }

    private void Update()
    {
        UpdateSteering();
    }

    private void UpdateSteering()
    {
        if (rotationCompletion < 1.0f)
        {
            ApplyWheelRotations();
            rotationCompletion += Time.deltaTime * rotationSpeed;
        }
    }

    public void AccelerationInput(float accelerationValue)
    {
        accelerationInput = accelerationValue;
    }
    
    public void SteeringInput(float steeringValue)
    {
        steeringInput = steeringValue;
        
        turningLeft = steeringValue < 0.0f;
        turningRight = steeringValue > 0.0f;
        rotationCompletion = 0f;
        
        // Cache current wheel rotations
        frontLeftTireRotation = frontLeftTire.localRotation;
        frontRightTireRotation = frontRightTire.localRotation;
        rearLeftTireRotation = rearLeftTire.localRotation;
        rearRightTireRotation = rearRightTire.localRotation;
    }

    public void ResetVehicle()
    {
        accelerationInput = 0f;
        ResetWheels();
        ResetSteeringCompletion();
        carRigidBody.linearVelocity = Vector3.zero;
        carRigidBody.angularVelocity = Vector3.zero;
        carRigidBody.MovePosition(carRigidBody.position + Vector3.up * 2.0f);
        
        Quaternion r = carRigidBody.rotation;
        r.SetLookRotation(transform.forward, Vector3.up);
        carRigidBody.rotation = r;
    }

    private void ApplyWheelRotations()
    {
        float targetAngle = GetTargetSteeringAngle();

        frontLeftTire.localRotation = Quaternion.Slerp(frontLeftTireRotation, Quaternion.Euler(0, targetAngle, 0), rotationCompletion);
        frontRightTire.localRotation = Quaternion.Slerp(frontRightTireRotation, Quaternion.Euler(0, targetAngle, 0), rotationCompletion);

        if (useFourWheelSteering) ApplyRearWheelRotations(targetAngle);
    }

    private float GetTargetSteeringAngle()
    {
        if (!turningLeft && !turningRight)
            return 0f;

        float angle = turningLeft ? -30f : 30f;

        // Slightly different angles for front wheels in 2-wheel steering
        if (!useFourWheelSteering)
        {
            return turningLeft ?
                frontLeftTire == null ? -35f : -30f :
                frontRightTire == null ? 35f : 30f;
        }

        return angle;
    }

    private void ApplyRearWheelRotations(float frontAngle)
    {
        float rearAngle;

        if (crabSteerAtHighSpeeds) rearAngle = frontAngle;
        else if (lowSpeedSteer) rearAngle = -frontAngle * 0.833f; // Converting 30 to 25 degrees
        else rearAngle = 0f;

        rearLeftTire.localRotation = Quaternion.Slerp(rearLeftTireRotation, Quaternion.Euler(0, rearAngle, 0), rotationCompletion);
        rearRightTire.localRotation = Quaternion.Slerp(rearRightTireRotation, Quaternion.Euler(0, rearAngle, 0), rotationCompletion);
    }

    private void FixedUpdate()
    {
        foreach (VehicleWheel tire in tires)
        {
            bool rayHitGround = Physics.Raycast(tire.transform.position, Vector3.down, out RaycastHit tireRaycast, suspensionRestDistance + lowerSuspensionExtentLimit,
                groundLayer);

            if (!rayHitGround) continue;
            
            // Align mesh to raycast
            float tireMeshOffset = tireHeightBasis - tireRaycast.distance;
            float targetOffset = tireBaseOffset + tireMeshOffset;
            Transform tireMesh = tire.transform.GetChild(0);
            Vector3 targetPosition = new Vector3(0, targetOffset, 0);
            Vector3 currentPosition = tireMesh.localPosition;

            tireMesh.localPosition = Vector3.Slerp(currentPosition, targetPosition, Time.fixedDeltaTime * 4.0f);
           
            // Compute spring force (suspension)
            // Rev. 2: Try to apply force along surface normal of ray impact instead (slopes)
            Vector3 springForceDir = tireRaycast.normal;
            Vector3 tireWorldVel = carRigidBody.GetPointVelocity(tire.transform.position);
            float offset = suspensionRestDistance - tireRaycast.distance;
            float vel = Vector3.Dot(springForceDir, tireWorldVel);
            float force = (offset * springStrength) - (vel * springDamper);
            
            carRigidBody.AddForceAtPosition(springForceDir * force, tire.transform.position);
                
            // Calculate speed variables for steering and acceleration
            Vector3 accelerationForceDir = tire.transform.forward;
            float carSpeed = Vector3.Dot(carTransform.forward, carRigidBody.linearVelocity);
            float normalizedCarSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / carTopSpeed);
                
            // Compute steering force
            Vector3 steeringForceDir = tire.transform.right;
            float steeringVel = Vector3.Dot(steeringForceDir, tireWorldVel);
            float sampledGripStrength;
            if (tire.drivePosition is WheelPosition.LeftFront or WheelPosition.RightFront)
            {
                // Apply front tire grip
                sampledGripStrength = useGripCurves ? frontTireGripCurve.Evaluate(Mathf.Abs(normalizedCarSpeed)) : frontTireGripStrength;
            }
            else
            {
                // Apply rear tire grip
                sampledGripStrength = useGripCurves ? rearTireGripCurve.Evaluate(Mathf.Abs(normalizedCarSpeed)) : rearTireGripStrength;
            }
                
            float desiredVelChange = -steeringVel * sampledGripStrength;
            float desiredAcceleration = desiredVelChange / Time.fixedDeltaTime;
                
            float appliedSteeringPower = tireMass * desiredAcceleration;
                
            if (turningLeft && tire.drivePosition is WheelPosition.LeftFront or WheelPosition.LeftRear)
            {
                appliedSteeringPower *= innerWheelTurningPower;
            }

            if (turningRight && tire.drivePosition is WheelPosition.RightFront or WheelPosition.RightRear)
            {
                appliedSteeringPower *= innerWheelTurningPower;
            }
                
            carRigidBody.AddForceAtPosition(steeringForceDir * appliedSteeringPower, tire.transform.position);
                
            // Compute acceleration
            float appliedAcceleration;
            if (tire.drivePosition is WheelPosition.LeftRear or WheelPosition.RightRear)
            {
                appliedAcceleration = rearDrivePowerFactor * accelerationPower;
            }
            else
            {
                appliedAcceleration = frontDrivePowerFactor * accelerationPower;
            }
                
            if (accelerationInput > 0.0f)
            {
                if (carSpeed < -0.1f)
                {
                    float availableBrakePower = brakePowerCurve.Evaluate(normalizedCarSpeed);
                    float forwardVel = Vector3.Dot(accelerationForceDir, tireWorldVel);
                    float desiredForwardVelChange = -forwardVel * availableBrakePower;
                    float desiredForwardAcceleration = desiredForwardVelChange / Time.fixedDeltaTime;
                    
                    carRigidBody.AddForceAtPosition(accelerationForceDir * (tireMass * desiredForwardAcceleration), tire.transform.position);
                }
                else
                {
                    float availableTorque = torquePowerCurve.Evaluate(normalizedCarSpeed) * accelerationInput * appliedAcceleration;
                    if (normalizedCarSpeed < 1.0f)
                    {
                        carRigidBody.AddForceAtPosition(accelerationForceDir * availableTorque, tire.transform.position);
                    }
                }
            }
            else if (accelerationInput < 0.0f)
            {
                // Compute brake / reverse
                if (carSpeed > 0.1f)
                {
                    float availableBrakePower = brakePowerCurve.Evaluate(normalizedCarSpeed);
                    float forwardVel = Vector3.Dot(accelerationForceDir, tireWorldVel);
                    float desiredForwardVelChange = -forwardVel * availableBrakePower;
                    float desiredForwardAcceleration = desiredForwardVelChange / Time.fixedDeltaTime;
                    
                    carRigidBody.AddForceAtPosition(accelerationForceDir * (tireMass * desiredForwardAcceleration), tire.transform.position);
                }
                else
                {
                    float reverseNormalizedCarSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / (carTopSpeed * reverseSpeedLimit));
                    float availableTorque = reverseTorquePowerCurve.Evaluate(reverseNormalizedCarSpeed) * accelerationInput * appliedAcceleration;
                    if (reverseNormalizedCarSpeed < 1.0f)
                    {
                        carRigidBody.AddForceAtPosition(accelerationForceDir * availableTorque, tire.transform.position);
                    }
                }
            }
                
            // Simulate tire friction when rolling to a stop
            else if (normalizedCarSpeed != 0)
            {
                float forwardVel = Vector3.Dot(accelerationForceDir, tireWorldVel);
                if (tire.drivePosition is WheelPosition.LeftFront or WheelPosition.RightFront)
                {
                    sampledGripStrength = useGripCurves ? frontTireGripCurve.Evaluate(Mathf.Abs(forwardVel)) : frontTireGripStrength;
                }
                else
                {
                    sampledGripStrength = useGripCurves ? rearTireGripCurve.Evaluate(Mathf.Abs(forwardVel)) : rearTireGripStrength;
                }
                float desiredForwardVelChange = -forwardVel * (sampledGripStrength * restfulGripFactor);
                float desiredForwardAcceleration = desiredForwardVelChange / Time.fixedDeltaTime;
                    
                carRigidBody.AddForceAtPosition(accelerationForceDir * (tireMass * desiredForwardAcceleration), tire.transform.position);
            }
                
            // Rotate wheel
            float tireCircumference = Mathf.PI * 2.0f * 0.5f;
            float directionalSpeed = Vector3.Dot(accelerationForceDir, tireWorldVel);
            float rotationAmount = directionalSpeed / tireCircumference;
            tire.transform.GetChild(0).GetChild(0).Rotate(Vector3.right, rotationAmount * 3.0f, Space.Self);
                
            // Switch 4WD steering mode
            float sampledSteering = fourWheelSteeringCurve.Evaluate(Mathf.Abs(normalizedCarSpeed));
                
            if (sampledSteering < 1.0f)
            {
                if (!lowSpeedSteer) continue;
                lowSpeedSteer = false;
                ResetSteeringCompletion();
            }
            else
            {
                if (lowSpeedSteer) continue;
                lowSpeedSteer = true;
                ResetSteeringCompletion();
            }
        }
    }

    private void ResetWheels()
    {
        frontLeftTire.localRotation = Quaternion.identity;
        frontRightTire.localRotation = Quaternion.identity;
        rearLeftTire.localRotation = Quaternion.identity;
        rearRightTire.localRotation = Quaternion.identity;
    }
}

using System;
using UnityEngine;

public class VehicleWheel : MonoBehaviour
{
    public WheelPosition drivePosition;
}

public enum WheelPosition
{
    LeftFront,
    LeftRear,
    RightFront,
    RightRear
}
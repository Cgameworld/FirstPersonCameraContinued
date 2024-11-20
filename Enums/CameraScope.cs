namespace FirstPersonCameraContinued.Enums
{
    /// <summary>
    /// Describes the current scope of the first
    /// person camera. Used for effects and positioning.
    /// </summary>
    public enum CameraScope
    {
        Default,
        Citizen,
        Pet,

        UnknownVehicle,
        Car,
        Truck,
        Van,

        Train,
        Airplane,
        Helicopter,
        Building
    }
}

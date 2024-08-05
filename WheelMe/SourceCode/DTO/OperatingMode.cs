namespace WheelMe.DTO
{
    public enum OperatingMode
    {
        UnspecifiedOperatingMode = 0,
        Autonomous = 1,
        OperatingModeInitializing = 2,
        TeleOp = 3,
        Joystick = 4,
        PoweredOff = 5,
        Reconnecting = 6
    }
}
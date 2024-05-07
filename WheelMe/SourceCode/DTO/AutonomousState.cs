namespace WheelMe.DTO
{
    public enum AutonomousState
    {
        UnspecifiedAutonomousState = 0,
        Idle = 1,
        AutonomousStateInitializing = 2,
        Navigating = 3,
        Stuck = 4,
        Waiting = 5,
        Paused = 6,
        NonAutonomous = 7
    }
}
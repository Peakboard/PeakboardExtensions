namespace WheelMe.DTO
{
    public class RobotDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
    
        public Point2D Position { get; set; }
    
        public OperatingMode OperatingMode { get; set; }

        public AutonomousState State { get; set; }
    
        public long? NavigatingToPositionId { get; set; }

        public string NavigatingToPositionName { get; set; }

        public long? CurrentPositionId { get; set; }

        public string CurrentPositionName { get; set; }
    }
}
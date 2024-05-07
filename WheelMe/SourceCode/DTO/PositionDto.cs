namespace WheelMe.DTO
{
    public class PositionDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public Point2D Position { get; set; }

        public PositionState State { get; set; }

        public string OccupiedBy { get; set; }
    }
}
namespace AuPairGo.API.DTOs
{
    public class LocationLogResponseDto
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public int AuPairId { get; set; }
        public int ParentId { get; set; }
        public int? ChildId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
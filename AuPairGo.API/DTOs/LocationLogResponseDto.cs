namespace AuPairGo.API.DTOs
{
    public class LocationLogResponseDto
    {
        public int Id { get; set; }
        public int DutyTaskId { get; set; }
        public int? ChildId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
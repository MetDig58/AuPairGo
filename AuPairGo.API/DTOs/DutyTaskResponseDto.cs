namespace AuPairGo.API.DTOs
{
    public class DutyTaskResponseDto
    {
        public int Id { get; set; }
        public int ParentProfileId { get; set; }
        public int AuPairProfileId { get; set; }
        public int? ChildProfileId { get; set; }
        public int PickupAddressId { get; set; }
        public int DropOffAddressId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
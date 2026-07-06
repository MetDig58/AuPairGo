namespace AuPairGo.API.Models;

public class LocationLogs
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int AuPairID { get; set; }
    public int ParentId { get; set; }
    public int ChildId { get; set; }
    public double latitude { get; set; }
    public double longitude { get; set; }
    public DateTime TimeStamp { get; set; }
}
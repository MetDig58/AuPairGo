namespace AuPairGo.API.Models;

public class Schedule
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public int ParentId { get; set; }
    public int StatusId { get; set; }
    public DateTime PickupTime { get; set; }
    public int PickupAddressId { get; set; }
    public DateTime ExpectedDropOffTime { get; set; }
    public int DropOffAddressId { get; set; }
}
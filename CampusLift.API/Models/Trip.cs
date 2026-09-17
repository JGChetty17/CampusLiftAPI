using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("trips")]
    public class Trip : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }
        [Column("driver_id")] public Guid DriverId { get; set; }
        [Column("vehicle_id")] public Guid? VehicleId { get; set; }
        [Column("from_location")] public string FromLocation { get; set; } = string.Empty;
        [Column("to_location")] public string ToLocation { get; set; } = string.Empty;
        [Column("from_lat")] public double? FromLat { get; set; }
        [Column("from_lng")] public double? FromLng { get; set; }
        [Column("to_lat")] public double? ToLat { get; set; }
        [Column("to_lng")] public double? ToLng { get; set; }
        [Column("event_time")] public DateTime EventTime { get; set; }
        [Column("price_per_seat")] public decimal PricePerSeat { get; set; }
        [Column("total_seats")] public int TotalSeats { get; set; }
        [Column("description")] public string? Description { get; set; }
        [Column("is_active")] public bool IsActive { get; set; } = true;
        [Column("is_complete")] public bool IsComplete { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}
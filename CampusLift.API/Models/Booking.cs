using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("bookings")]
    public class Booking : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }
        [Column("trip_id")] public Guid TripId { get; set; }
        [Column("passenger_id")] public Guid PassengerId { get; set; }
        [Column("seats_requested")] public int SeatsRequested { get; set; } = 1;
        [Column("approval")] public string Approval { get; set; } = "pending";
        [Column("approved_time")] public DateTime? ApprovedTime { get; set; }
        [Column("booking_time")] public DateTime BookingTime { get; set; }
        [Column("cancellation_time")] public DateTime? CancellationTime { get; set; }
        [Column("pickup_confirmed")] public bool PickupConfirmed { get; set; }
    }
}
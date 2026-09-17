namespace CampusLift.API.DTOs
{
    public record CreateBookingRequest(
        Guid TripId,
        int SeatsRequested
    );

    public record ApproveBookingRequest(
        bool Approve // true = approved, false = rejected
    );

    public class BookingWithTrip
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid PassengerId { get; set; }
        public int SeatsRequested { get; set; }
        public string Approval { get; set; } = "pending";
        public DateTime? ApprovedTime { get; set; }
        public DateTime BookingTime { get; set; }
        public DateTime? CancellationTime { get; set; }
        public bool PickupConfirmed { get; set; }

        // Trip summary so the client doesn't need a second request
        public string FromLocation { get; set; } = "";
        public string ToLocation { get; set; } = "";
        public DateTime EventTime { get; set; }
        public decimal PricePerSeat { get; set; }
    }
}
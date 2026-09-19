namespace CampusLift.API.DTOs
{
    public record CreateTripRequest(
        Guid? VehicleId,
        string FromLocation,
        string ToLocation,
        double? FromLat,
        double? FromLng,
        double? ToLat,
        double? ToLng,
        DateTime EventTime,
        decimal PricePerSeat,
        int TotalSeats,
        string? Description
    );

    public record UpdateTripRequest(
        Guid? VehicleId,
        string? FromLocation,
        string? ToLocation,
        double? FromLat,
        double? FromLng,
        double? ToLat,
        double? ToLng,
        DateTime? EventTime,
        decimal? PricePerSeat,
        int? TotalSeats,
        string? Description,
        bool? IsActive
    );

    /// <summary>
    /// Trip plus computed fields for the client:
    /// SeatsTaken (sum of active bookings), SeatsRemaining,
    /// plus denormalized Driver + Vehicle for ride cards.
    /// </summary>
    public class TripWithAvailability
    {
        public Guid Id { get; set; }
        public Guid DriverId { get; set; }
        public Guid? VehicleId { get; set; }
        public string FromLocation { get; set; } = "";
        public string ToLocation { get; set; } = "";
        public double? FromLat { get; set; }
        public double? FromLng { get; set; }
        public double? ToLat { get; set; }
        public double? ToLng { get; set; }
        public DateTime EventTime { get; set; }
        public decimal PricePerSeat { get; set; }
        public int TotalSeats { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }
        public DateTime CreatedAt { get; set; }

        public int SeatsTaken { get; set; }
        public int SeatsRemaining { get; set; }

        // Denormalized display info
        public PublicUserSummary? Driver { get; set; }
        public PublicVehicleSummary? Vehicle { get; set; }
    }
}
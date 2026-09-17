namespace CampusLift.API.DTOs
{
    public record CreateVehicleRequest(
        string Make,
        string Model,
        int? Year,
        string? Color,
        string LicensePlate,
        int Seats
    );

    public record UpdateVehicleRequest(
        string? Make,
        string? Model,
        int? Year,
        string? Color,
        string? LicensePlate,
        int? Seats,
        bool? IsActive
    );
}
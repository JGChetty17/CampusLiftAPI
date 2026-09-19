namespace CampusLift.API.DTOs
{
    public class PublicUserSummary
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? ProfilePicture { get; set; }
    }

    public class PublicVehicleSummary
    {
        public Guid Id { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? Color { get; set; }
        public string? LicensePlate { get; set; }
        public int Seats { get; set; }
    }
}
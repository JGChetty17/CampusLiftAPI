using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("vehicles")]
    public class Vehicle : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("make")] public string Make { get; set; } = string.Empty;
        [Column("model")] public string Model { get; set; } = string.Empty;
        [Column("year")] public int? Year { get; set; }
        [Column("color")] public string? Color { get; set; }
        [Column("license_plate")] public string? LicensePlate { get; set; }
        [Column("seats")] public int Seats { get; set; }
        [Column("is_active")] public bool IsActive { get; set; } = true;
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}
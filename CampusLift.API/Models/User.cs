using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }

        [Column("firebase_uid")] public string FirebaseUid { get; set; } = string.Empty;
        [Column("email")] public string? Email { get; set; }
        [Column("name")] public string? Name { get; set; }
        [Column("surname")] public string? Surname { get; set; }
        [Column("student_number")] public string? StudentNumber { get; set; }
        [Column("university")] public string? University { get; set; }
        [Column("is_verified")] public bool IsVerified { get; set; }
        [Column("profile_picture")] public string? ProfilePicture { get; set; }
        [Column("emergency_contact")] public string? EmergencyContact { get; set; }
        [Column("language")] public string? Language { get; set; }
        [Column("dark_mode")] public bool DarkMode { get; set; }
        [Column("biometric_enabled")] public bool BiometricEnabled { get; set; }
        [Column("notification_enabled")] public bool NotificationEnabled { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
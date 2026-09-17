using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        [Column("type")] public string Type { get; set; } = "system";
        [Column("title")] public string Title { get; set; } = string.Empty;
        [Column("message")] public string Message { get; set; } = string.Empty;
        [Column("timestamp")] public DateTime Timestamp { get; set; }
        [Column("is_read")] public bool IsRead { get; set; }
    }
}
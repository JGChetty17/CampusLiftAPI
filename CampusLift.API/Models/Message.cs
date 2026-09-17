using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }
        [Column("trip_id")] public Guid TripId { get; set; }
        [Column("sender_id")] public Guid SenderId { get; set; }
        [Column("content")] public string Content { get; set; } = string.Empty;
        [Column("timestamp")] public DateTime Timestamp { get; set; }
        [Column("is_read")] public bool IsRead { get; set; }
    }
}
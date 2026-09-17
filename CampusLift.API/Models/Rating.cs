using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CampusLift.API.Models
{
    [Table("ratings")]
    public class Rating : BaseModel
    {
        [PrimaryKey("id", false)] public Guid Id { get; set; }
        [Column("trip_id")] public Guid TripId { get; set; }
        [Column("rater_id")] public Guid RaterId { get; set; }
        [Column("rated_user_id")] public Guid RatedUserId { get; set; }
        [Column("score")] public int Score { get; set; }
        [Column("comment")] public string? Comment { get; set; }
        [Column("timestamp")] public DateTime Timestamp { get; set; }
    }
}
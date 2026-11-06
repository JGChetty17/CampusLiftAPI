using CampusLift.API.Extensions;
using CampusLift.API.Models;

namespace CampusLift.API.DTOs
{
    /// <summary>Request body for POST /api/trips/{tripId}/messages</summary>
    public record SendMessageRequest(string Content);

    /// <summary>Single message enriched with the sender's public profile.</summary>
    public class MessageResponse
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public PublicUserSummary? Sender { get; set; }

        public static MessageResponse FromMessage(Message m, PublicUserSummary? sender) => new()
        {
            Id = m.Id,
            TripId = m.TripId,
            SenderId = m.SenderId,
            Content = m.Content,
            Timestamp = m.Timestamp.AsUtc(),
            IsRead = m.IsRead,
            Sender = sender
        };
    }

    /// <summary>Response body for GET /api/trips/{tripId}/messages</summary>
    public class MessageListResponse
    {
        public List<MessageResponse> Items { get; set; } = new();
        public bool ReadOnly { get; set; }
    }
}
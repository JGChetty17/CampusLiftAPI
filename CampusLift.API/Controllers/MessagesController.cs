using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using CampusLift.API.Services;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

namespace CampusLift.API.Controllers
{
    /// <summary>
    /// Trip chat endpoints. Access is limited to the trip's driver and any
    /// passenger whose booking is "approved" and not cancelled.
    /// Chat becomes read-only when the trip is completed or cancelled.
    /// </summary>
    [Route("api/trips/{tripId:guid}/messages")]
    public class MessagesController : BaseApiController
    {
        private readonly FcmService _fcm;

        public MessagesController(Supabase.Client sb, FcmService fcm) : base(sb)
        {
            _fcm = fcm;
        }

        // ═══════════════════════════════════════════════════════════
        // GET — list messages for a trip (oldest first)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> List(Guid tripId)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var trip = await LoadTripOrNull(tripId);
            if (trip == null) return NotFound("Trip not found.");

            var hasAccess = await UserCanAccessTripChat(trip, user);
            if (!hasAccess) return StatusCode(403, "You are not a participant on this trip.");

            var res = await Sb.From<Message>()
                .Where(m => m.TripId == tripId)
                .Order(m => m.Timestamp, Constants.Ordering.Ascending)
                .Get();

            var senderIds = res.Models.Select(m => m.SenderId).Distinct().ToList();
            var sendersById = await LoadSenders(senderIds);

            var items = res.Models
                .Select(m => MessageResponse.FromMessage(m, sendersById.GetValueOrDefault(m.SenderId)))
                .ToList();

            var readOnly = trip.IsComplete || !trip.IsActive;

            return Ok(new MessageListResponse
            {
                Items = items,
                ReadOnly = readOnly
            });
        }

        // ═══════════════════════════════════════════════════════════
        // POST — send a message
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Send(Guid tripId, [FromBody] SendMessageRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var trip = await LoadTripOrNull(tripId);
            if (trip == null) return NotFound("Trip not found.");

            var hasAccess = await UserCanAccessTripChat(trip, user);
            if (!hasAccess) return StatusCode(403, "You are not a participant on this trip.");

            if (trip.IsComplete || !trip.IsActive)
                return BadRequest("This chat is closed because the trip has ended.");

            var content = (req.Content ?? "").Trim();
            if (string.IsNullOrEmpty(content))
                return BadRequest("Message content is required.");
            if (content.Length > 500)
                return BadRequest("Message content must not exceed 500 characters.");

            var message = new Message
            {
                TripId = tripId,
                SenderId = user.Id,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            var inserted = await Sb.From<Message>().Insert(message);
            var saved = inserted.Models.First();

            // Fire notifications to all other participants
            var preview = content.Length <= 60 ? content : content[..60] + "...";
            var recipients = await GetApprovedParticipantIds(trip);
            recipients.Remove(user.Id);   // don't notify yourself

            var senderName = $"{user.Name} {user.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(senderName)) senderName = "a rider";

            foreach (var recipientId in recipients)
            {
                await Sb.CreateNotification(
                    recipientId,
                    "message",
                    $"New message from {senderName}",
                    preview,
                    _fcm);
            }

            var sender = new PublicUserSummary
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                ProfilePicture = user.ProfilePicture
            };

            return Ok(MessageResponse.FromMessage(saved, sender));
        }

        // ═══════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════

        private async Task<Trip?> LoadTripOrNull(Guid tripId)
        {
            var res = await Sb.From<Trip>()
                .Where(t => t.Id == tripId)
                .Limit(1)
                .Get();
            return res.Models.FirstOrDefault();
        }

        /// <summary>
        /// True if the user is the trip's driver, OR a passenger with an
        /// "approved" booking that hasn't been cancelled.
        /// </summary>
        private async Task<bool> UserCanAccessTripChat(Trip trip, User user)
        {
            if (trip.DriverId == user.Id) return true;

            var bookingRes = await Sb.From<Booking>()
                .Where(b => b.TripId == trip.Id
                         && b.PassengerId == user.Id
                         && b.Approval == "approved"
                         && b.CancellationTime == null)
                .Limit(1)
                .Get();

            return bookingRes.Models.Any();
        }

        /// <summary>
        /// Returns internal user ids for all chat participants (driver + approved passengers).
        /// Used for notification fan-out.
        /// </summary>
        private async Task<HashSet<Guid>> GetApprovedParticipantIds(Trip trip)
        {
            var ids = new HashSet<Guid> { trip.DriverId };

            var bookings = await Sb.From<Booking>()
                .Where(b => b.TripId == trip.Id
                         && b.Approval == "approved"
                         && b.CancellationTime == null)
                .Get();

            foreach (var b in bookings.Models)
                ids.Add(b.PassengerId);

            return ids;
        }

        /// <summary>
        /// Batch-loads PublicUserSummary for the given sender ids.
        /// </summary>
        private async Task<Dictionary<Guid, PublicUserSummary>> LoadSenders(List<Guid> senderIds)
        {
            var result = new Dictionary<Guid, PublicUserSummary>();
            foreach (var id in senderIds)
            {
                var r = await Sb.From<User>().Where(u => u.Id == id).Limit(1).Get();
                var u = r.Models.FirstOrDefault();
                if (u != null)
                    result[u.Id] = new PublicUserSummary
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        ProfilePicture = u.ProfilePicture
                    };
            }
            return result;
        }
    }
}
using CampusLift.API.Models;
using CampusLift.API.Services;
using Supabase;

namespace CampusLift.API.Extensions
{
    public static class NotificationExtensions
    {
        /// <summary>
        /// Writes a notification row AND sends an FCM push (if FcmService is provided).
        /// Failures are swallowed — never break the caller.
        /// </summary>
        public static async Task CreateNotification(
            this Supabase.Client sb,
            Guid userId,
            string type,
            string title,
            string message,
            FcmService? fcm = null)        // ← optional param, back-compatible
        {
            // 1. Persist to notifications table (existing behaviour)
            try
            {
                var n = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };
                await sb.From<Notification>().Insert(n);
            }
            catch
            {
                // intentionally swallowed
            }

            // 2. Fire FCM push (new) — only if FcmService injected
            if (fcm != null)
            {
                await fcm.SendToUserAsync(userId, title, message, new Dictionary<string, string>
                {
                    { "type", type }
                });
            }
        }
    }
}
using CampusLift.API.Models;
using Supabase;

namespace CampusLift.API.Extensions
{
    public static class NotificationExtensions
    {
        // Writes a row to the notifications table.Failures are swallowed never break the caller because a notification insert died.
        public static async Task CreateNotification(
            this Supabase.Client sb,
            Guid userId,
            string type,      
            string title,
            string message)
        {
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
        }
    }
}
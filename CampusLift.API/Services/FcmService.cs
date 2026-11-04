using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Supabase;

namespace CampusLift.API.Services
{
    /// <summary>
    /// Wraps Firebase Admin SDK to send push notifications to specific users.
    /// Looks up the user's FCM token from the `users` table, then sends via FCM.
    /// Fails silently — never throws to callers (matches CreateNotification behaviour).
    /// </summary>
    public class FcmService
    {
        private readonly Supabase.Client _sb;
        private readonly IConfiguration _config;

        public FcmService(Supabase.Client sb, IConfiguration config)
        {
            _sb = sb;
            _config = config;
        }

        /// <summary>
        /// Send a push notification to one user by internal user id.
        /// No-op if Firebase is not initialized or the user has no token.
        /// </summary>
        public async Task SendToUserAsync(
            Guid userId,
            string title,
            string body,
            Dictionary<string, string>? data = null)
        {
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    Console.WriteLine("[FCM] Skipped — Firebase Admin not initialized.");
                    return;
                }

                // Load token from DB
                var userRes = await _sb.From<Models.User>()
                    .Where(u => u.Id == userId)
                    .Limit(1)
                    .Get();
                var user = userRes.Models.FirstOrDefault();

                if (user == null || string.IsNullOrWhiteSpace(user.FcmToken))
                {
                    Console.WriteLine($"[FCM] Skipped — user {userId} has no FCM token.");
                    return;
                }

                var message = new Message
                {
                    Token = user.FcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Title = title,
                            Body = body,
                            Sound = "default",
                            // Channel must match what Suvan creates on the Android side
                            ChannelId = "campuslift_default"
                        }
                    }
                };

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine($"[FCM] Sent to user {userId}: {messageId}");
            }
            catch (FirebaseMessagingException fme) when (fme.MessagingErrorCode == MessagingErrorCode.Unregistered)
            {
                // Token is stale — clear it so we don't keep trying
                Console.WriteLine($"[FCM] Stale token for user {userId}, clearing.");
                try
                {
                    var userRes = await _sb.From<Models.User>()
                        .Where(u => u.Id == userId).Limit(1).Get();
                    var user = userRes.Models.FirstOrDefault();
                    if (user != null)
                    {
                        user.FcmToken = null;
                        await _sb.From<Models.User>().Update(user);
                    }
                }
                catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM] ERROR sending to user {userId}: {ex.Message}");
                // Intentionally swallowed — a push failure must never break the API call
            }
        }
    }
}
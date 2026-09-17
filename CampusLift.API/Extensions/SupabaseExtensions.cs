using CampusLift.API.Models;
using Supabase;

namespace CampusLift.API.Extensions
{
    public static class SupabaseExtensions
    {
        /// <summary>
        /// Looks up the internal Supabase user by the Firebase UID header.
        /// Returns null if not found.
        /// </summary>
        public static async Task<User?> GetByFirebaseUidAsync(
            this Supabase.Client sb, string? firebaseUid)
        {
            if (string.IsNullOrWhiteSpace(firebaseUid)) return null;

            var res = await sb.From<User>()
                .Where(u => u.FirebaseUid == firebaseUid)
                .Limit(1)
                .Get();

            return res.Models.FirstOrDefault();
        }

        /// <summary>
        /// Pulls the X-Firebase-Uid header and resolves the user.
        /// Returns null if the header is missing or the user doesn't exist.
        /// </summary>
        public static async Task<User?> ResolveUserAsync(
            this Supabase.Client sb, HttpContext ctx)
        {
            var uid = ctx.Request.Headers["X-Firebase-Uid"].FirstOrDefault();
            return await sb.GetByFirebaseUidAsync(uid);
        }
    }
}
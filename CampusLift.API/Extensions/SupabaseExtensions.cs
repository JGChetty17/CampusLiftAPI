using CampusLift.API.Models;
using Supabase;

namespace CampusLift.API.Extensions
{
    public static class SupabaseExtensions
    {
        // Looks up the internal Supabase user by the Firebase UID header.Returns null if not found.
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

        // Pulls the X-Firebase-Uid header and resolves the user.Returns null if the header is missing or the user doesn't exist.
        public static async Task<User?> ResolveUserAsync(
            this Supabase.Client sb, HttpContext ctx)
        {
            var uid = ctx.Request.Headers["X-Firebase-Uid"].FirstOrDefault();
            return await sb.GetByFirebaseUidAsync(uid);
        }
    }
}
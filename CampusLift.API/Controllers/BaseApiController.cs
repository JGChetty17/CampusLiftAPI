using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace CampusLift.API.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly Supabase.Client Sb;

        protected BaseApiController(Supabase.Client sb)
        {
            Sb = sb;
        }

        /// <summary>
        /// Firebase UID of the current user. Sent by the Android app
        /// as a header on every request.
        /// </summary>
        [FromHeader(Name = "X-Firebase-Uid")]
        public string? FirebaseUid { get; set; }

        /// <summary>
        /// Resolves the current user from the Firebase UID header.
        /// Returns null if missing or unknown.
        /// This is the single chokepoint we'll hook Firebase token
        /// verification into later.
        /// </summary>
        protected Task<User?> CurrentUser()
            => Sb.ResolveUserAsync(HttpContext);
    }
}
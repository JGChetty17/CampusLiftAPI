// =============================================================================
// BaseApiController
// -----------------------------------------------------------------------------
// Abstract base class for all CampusLift API controllers.
//
// Provides:
//   - A shared Supabase.Client instance (injected via constructor).
//   - A `FirebaseUid` property bound from the "X-Firebase-Uid" request header.
//   - A `CurrentUser()` helper that resolves the current User entity from
//     Supabase using the Firebase UID header via HttpContext.
//
// References:
//   [1] Microsoft Learn — Create web APIs with ASP.NET Core
//       https://learn.microsoft.com/aspnet/core/web-api/
//   [2] Microsoft Learn — Binding source parameter inference
//       https://learn.microsoft.com/aspnet/core/mvc/models/model-binding
//   [3] Supabase C# API Reference — Introduction
//       https://supabase.com/docs/reference/csharp/introduction
//   [4] Firebase Auth REST API
//       https://firebase.google.com/docs/reference/rest/auth
//
// Author: CampusLift API
// =============================================================================

using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace CampusLift.API.Controllers
{
    // See [1]: [ApiController] enables automatic model validation and binding inference.
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        // See [3]: shared Supabase client, injected by DI.
        protected readonly Supabase.Client Sb;

        protected BaseApiController(Supabase.Client sb)
        {
            Sb = sb;
        }

        // See [2]: [FromHeader] binds this property from the "X-Firebase-Uid" request header.
        // See [4]: The value is a Firebase UID (localId) sent by the Android client.
        [FromHeader(Name = "X-Firebase-Uid")]
        public string? FirebaseUid { get; set; }

        // Resolves the current user from the Firebase UID header.
        // Returns null if missing or unknown.
        protected Task<User?> CurrentUser()
            => Sb.ResolveUserAsync(HttpContext);
    }
}
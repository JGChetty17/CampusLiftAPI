// =============================================================================
// UsersController
// -----------------------------------------------------------------------------
// Handles user-related endpoints for the CampusLift API:
//   - Sync user (called by Android after Firebase login)
//   - Get current user ("me")
//   - Update current user profile ("me")
//
// References:
//   [1] Supabase C# API Reference — Introduction
//       https://supabase.com/docs/reference/csharp/introduction
//   [2] Supabase C# SDK — GitHub Repository
//       https://github.com/supabase-community/supabase-csharp
//   [3] Firebase Auth REST API
//       https://firebase.google.com/docs/reference/rest/auth
//   [4] Microsoft Learn — Create web APIs with ASP.NET Core
//       https://learn.microsoft.com/aspnet/core/web-api/
//   [5] Microsoft Learn — Binding source parameter inference
//       https://learn.microsoft.com/aspnet/core/mvc/models/model-binding
//
// Author: CampusLift API
// =============================================================================

using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace CampusLift.API.Controllers
{
    // See [4]: attribute routing for all endpoints in this controller.
    [Route("api/users")]
    public class UsersController : BaseApiController
    {
        public UsersController(Supabase.Client sb) : base(sb) { }

        // SYNC — called by Android after Firebase login
        // See [3]: the Android client sends the Firebase UID obtained from sign-in.
        // See [5]: [FromBody] binds JSON to SyncUserRequest.
        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FirebaseUid))
                return BadRequest("FirebaseUid is required.");

            var existing = await Sb.GetByFirebaseUidAsync(req.FirebaseUid);
            if (existing != null) return Ok(existing);

            var user = new User
            {
                FirebaseUid = req.FirebaseUid,
                Email = req.Email,
                Name = req.Name,
                Surname = req.Surname,
                Language = "en",
                NotificationEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // See [1] and [2]: .Insert(entity) issues INSERT ... RETURNING *.
            var inserted = await Sb.From<User>().Insert(user);
            return Ok(inserted.Models.First());
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");
            return Ok(user);
        }

        // UPDATE PROFILE
        // See [4]: [HttpPatch] for partial updates.
        [HttpPatch("me")]
        public async Task<IActionResult> Update([FromBody] UpdateUserRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (req.Name != null) user.Name = req.Name;
            if (req.Surname != null) user.Surname = req.Surname;
            if (req.StudentNumber != null) user.StudentNumber = req.StudentNumber;
            if (req.University != null) user.University = req.University;
            if (req.ProfilePicture != null) user.ProfilePicture = req.ProfilePicture;
            if (req.EmergencyContact != null) user.EmergencyContact = req.EmergencyContact;
            if (req.Language != null) user.Language = req.Language;
            if (req.DarkMode.HasValue) user.DarkMode = req.DarkMode.Value;
            if (req.BiometricEnabled.HasValue) user.BiometricEnabled = req.BiometricEnabled.Value;
            if (req.NotificationEnabled.HasValue) user.NotificationEnabled = req.NotificationEnabled.Value;

            user.UpdatedAt = DateTime.UtcNow;

            // See [1]: .Update(entity) persists the modified row.
            var updated = await Sb.From<User>().Update(user);
            return Ok(updated.Models.First());
        }
    }
}
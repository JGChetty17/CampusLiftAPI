using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace CampusLift.API.Controllers
{
    [Route("api/users")]
    public class UsersController : BaseApiController
    {
        public UsersController(Supabase.Client sb) : base(sb) { }

        // SYNC — called by Android after Firebase login
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

            var updated = await Sb.From<User>().Update(user);
            return Ok(updated.Models.First());
        }
    }
}
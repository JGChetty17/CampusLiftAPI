// =============================================================================
// NotificationsController
// -----------------------------------------------------------------------------
// Handles notification endpoints for the CampusLift API:
//   - List my notifications (newest first, paginated, optional unread filter)
//   - Get unread count
//   - Mark a single notification as read/unread
//   - Mark all notifications as read
//   - Delete a notification
//
// References:
//   [1] Supabase C# API Reference — Introduction
//       https://supabase.com/docs/reference/csharp/introduction
//   [2] Supabase C# API Reference — Using Modifiers
//       https://supabase.com/docs/reference/csharp/using-modifiers
//   [3] Supabase C# SDK — GitHub Repository
//       https://github.com/supabase-community/supabase-csharp
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
using Supabase.Postgrest;

namespace CampusLift.API.Controllers
{
    // See [4]: attribute routing for all endpoints in this controller.
    [Route("api/notifications")]
    public class NotificationsController : BaseApiController
    {
        public NotificationsController(Supabase.Client sb) : base(sb) { }

        // LIST — my notifications, newest first, paginated
        // See [5]: [FromQuery] binds query-string params.
        [HttpGet]
        public async Task<IActionResult> Mine(
            [FromQuery] bool? unreadOnly,
            [FromQuery] int limit = 30,
            [FromQuery] int offset = 0)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            if (offset < 0) offset = 0;

            // See [1]: From<Notification>().Where(...) builds the base SELECT.
            var q = Sb.From<Notification>()
                .Where(n => n.UserId == user.Id);

            if (unreadOnly == true)
                q = q.Where(n => n.IsRead == false);

            // See [2]: .Order() then .Range() implements LIMIT/OFFSET pagination.
            var res = await q
                .Order(n => n.Timestamp, Constants.Ordering.Descending)
                .Range(offset, offset + limit - 1)
                .Get();

            return Ok(new
            {
                items = res.Models,
                limit,
                offset,
                count = res.Models.Count
            });
        }

        // UNREAD COUNT
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            // See [1]: chained .Where() clauses combine with AND.
            var res = await Sb.From<Notification>()
                .Where(n => n.UserId == user.Id && n.IsRead == false)
                .Get();

            return Ok(new { unread = res.Models.Count });
        }

        // MARK ONE AS READ
        // See [4]: [HttpPatch] for partial updates.
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> MarkRead(Guid id, [FromBody] UpdateNotificationRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Notification>()
                .Where(n => n.Id == id && n.UserId == user.Id)
                .Limit(1)
                .Get();

            var n = res.Models.FirstOrDefault();
            if (n == null) return NotFound();

            n.IsRead = req.IsRead;
            // See [1]: .Update(entity) persists the modified row.
            var updated = await Sb.From<Notification>().Update(n);
            return Ok(updated.Models.First());
        }

        // MARK ALL AS READ
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Notification>()
                .Where(n => n.UserId == user.Id && n.IsRead == false)
                .Get();

            var count = 0;
            foreach (var n in res.Models)
            {
                n.IsRead = true;
                await Sb.From<Notification>().Update(n);
                count++;
            }

            return Ok(new { markedRead = count });
        }

        // DELETE
        // See [4]: [HttpDelete] with route constraint {id:guid}.
        // See [3]: .Delete(entity) issues a DELETE against the mapped table.
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Notification>()
                .Where(n => n.Id == id && n.UserId == user.Id)
                .Limit(1)
                .Get();

            var n = res.Models.FirstOrDefault();
            if (n == null) return NotFound();

            await Sb.From<Notification>().Delete(n);
            return NoContent();
        }
    }
}
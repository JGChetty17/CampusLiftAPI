using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

namespace CampusLift.API.Controllers
{
    [Route("api/notifications")]
    public class NotificationsController : BaseApiController
    {
        public NotificationsController(Supabase.Client sb) : base(sb) { }

        // LIST — my notifications, newest first, paginated
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

            var q = Sb.From<Notification>()
                .Where(n => n.UserId == user.Id);

            if (unreadOnly == true)
                q = q.Where(n => n.IsRead == false);

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

            var res = await Sb.From<Notification>()
                .Where(n => n.UserId == user.Id && n.IsRead == false)
                .Get();

            return Ok(new { unread = res.Models.Count });
        }

        // MARK ONE AS READ
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
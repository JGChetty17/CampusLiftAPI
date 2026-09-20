using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

namespace CampusLift.API.Controllers
{
    [Route("api/vehicles")]
    public class VehiclesController : BaseApiController
    {
        public VehiclesController(Supabase.Client sb) : base(sb) { }

        // LIST MY VEHICLES
        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Vehicle>()
                .Where(v => v.UserId == user.Id)
                .Order(v => v.CreatedAt, Constants.Ordering.Descending)
                .Get();

            return Ok(res.Models);
        }

        // GET BY ID
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Vehicle>()
                .Where(v => v.Id == id && v.UserId == user.Id)
                .Limit(1)
                .Get();

            var vehicle = res.Models.FirstOrDefault();
            if (vehicle == null) return NotFound();
            return Ok(vehicle);
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (string.IsNullOrWhiteSpace(req.Make) ||
                string.IsNullOrWhiteSpace(req.Model) ||
                string.IsNullOrWhiteSpace(req.LicensePlate))
                return BadRequest("Make, Model and LicensePlate are required.");

            if (req.Seats < 1 || req.Seats > 20)
                return BadRequest("Seats must be between 1 and 20.");

            var vehicle = new Vehicle
            {
                UserId = user.Id,
                Make = req.Make.Trim(),
                Model = req.Model.Trim(),
                Year = req.Year,
                Color = req.Color,
                LicensePlate = req.LicensePlate.Trim(),
                Seats = req.Seats,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await Sb.From<Vehicle>().Insert(vehicle);
            return Ok(inserted.Models.First());
        }

        // UPDATE
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Vehicle>()
                .Where(v => v.Id == id && v.UserId == user.Id)
                .Limit(1)
                .Get();

            var vehicle = res.Models.FirstOrDefault();
            if (vehicle == null) return NotFound();

            if (req.Make != null) vehicle.Make = req.Make.Trim();
            if (req.Model != null) vehicle.Model = req.Model.Trim();
            if (req.Year.HasValue) vehicle.Year = req.Year.Value;
            if (req.Color != null) vehicle.Color = req.Color;
            if (req.LicensePlate != null) vehicle.LicensePlate = req.LicensePlate.Trim();
            if (req.Seats.HasValue)
            {
                if (req.Seats.Value < 1 || req.Seats.Value > 20)
                    return BadRequest("Seats must be between 1 and 20.");
                vehicle.Seats = req.Seats.Value;
            }
            if (req.IsActive.HasValue) vehicle.IsActive = req.IsActive.Value;

            var updated = await Sb.From<Vehicle>().Update(vehicle);
            return Ok(updated.Models.First());
        }

        // DELETE — refuse if the vehicle is on an active trip
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Vehicle>()
                .Where(v => v.Id == id && v.UserId == user.Id)
                .Limit(1)
                .Get();

            var vehicle = res.Models.FirstOrDefault();
            if (vehicle == null) return NotFound();

            var activeTripsRes = await Sb.From<Trip>()
                .Where(t => t.VehicleId == id && t.IsActive == true && t.IsComplete == false)
                .Get();

            if (activeTripsRes.Models.Any())
                return Conflict("Cannot delete a vehicle used by an active trip. Cancel those trips first.");

            await Sb.From<Vehicle>().Delete(vehicle);
            return NoContent();
        }
    }
}
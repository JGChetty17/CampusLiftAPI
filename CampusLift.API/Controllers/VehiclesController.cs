// =============================================================================
// VehiclesController
// -----------------------------------------------------------------------------
// Handles vehicle-related endpoints for the CampusLift API:
//   - List my vehicles
//   - Get a vehicle by id
//   - Create a vehicle
//   - Update a vehicle
//   - Delete a vehicle (blocked if used on an active trip)
//
// References:
//   [1] Supabase C# API Reference — Introduction
//       https://supabase.com/docs/reference/csharp/introduction
//   [2] Supabase.Postgrest NuGet Package
//       https://www.nuget.org/packages/Supabase.Postgrest
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

            // See [1]: From<Vehicle>().Where(...).Order(...).Get() query chain.
            var res = await Sb.From<Vehicle>()
                .Where(v => v.UserId == user.Id)
                .Order(v => v.CreatedAt, Constants.Ordering.Descending)
                .Get();

            return Ok(res.Models);
        }

        // GET BY ID
        // See [4]: route constraint {id:guid} restricts the segment to GUIDs.
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
        // See [5]: [FromBody] binds JSON to CreateVehicleRequest.
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

            // See [1] and [3]: .Insert(entity) issues INSERT ... RETURNING *.
            var inserted = await Sb.From<Vehicle>().Insert(vehicle);
            return Ok(inserted.Models.First());
        }

        // UPDATE
        // See [4]: [HttpPatch] for partial updates.
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

            // See [1]: .Update(entity) persists the modified row.
            var updated = await Sb.From<Vehicle>().Update(vehicle);
            return Ok(updated.Models.First());
        }

        // DELETE — refuse if the vehicle is on an active trip
        // See [4]: [HttpDelete] with route constraint {id:guid}.
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

            // See [3]: .Delete(entity) issues a DELETE against the mapped table.
            await Sb.From<Vehicle>().Delete(vehicle);
            return NoContent();
        }
    }
}
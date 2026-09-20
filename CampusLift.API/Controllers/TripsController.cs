using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

namespace CampusLift.API.Controllers
{
    [Route("api/trips")]
    public class TripsController : BaseApiController
    {
        public TripsController(Supabase.Client sb) : base(sb) { }

        // SEARCH — paginated, driver+vehicle denormalized
        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] DateTime? date,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            if (offset < 0) offset = 0;

            var q = Sb.From<Trip>()
                .Where(t => t.IsActive == true && t.IsComplete == false);

            if (!string.IsNullOrWhiteSpace(from))
                q = q.Filter("from_location", Constants.Operator.ILike, $"%{from.Trim()}%");

            if (!string.IsNullOrWhiteSpace(to))
                q = q.Filter("to_location", Constants.Operator.ILike, $"%{to.Trim()}%");

            if (date.HasValue)
            {
                var start = date.Value.Date.ToUniversalTime();
                var end = start.AddDays(1);
                q = q.Where(t => t.EventTime >= start && t.EventTime < end);
            }

            var tripsRes = await q
                .Order(t => t.EventTime, Constants.Ordering.Ascending)
                .Range(offset, offset + limit - 1)
                .Get();

            var (users, vehicles) = await LoadLookups(tripsRes.Models);

            var enriched = new List<TripWithAvailability>();
            foreach (var t in tripsRes.Models)
            {
                var dto = ToAvailability(t, await GetSeatsTaken(t.Id));
                dto.Driver = users.GetValueOrDefault(t.DriverId);
                dto.Vehicle = t.VehicleId.HasValue
                    ? vehicles.GetValueOrDefault(t.VehicleId.Value)
                    : null;
                enriched.Add(dto);
            }

            return Ok(new
            {
                items = enriched,
                limit,
                offset,
                count = enriched.Count
            });
        }

        // MY TRIPS — paginated, driver+vehicle denormalized
        [HttpGet("mine")]
        public async Task<IActionResult> MyTrips(
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            if (offset < 0) offset = 0;

            var res = await Sb.From<Trip>()
                .Where(t => t.DriverId == user.Id)
                .Order(t => t.EventTime, Constants.Ordering.Descending)
                .Range(offset, offset + limit - 1)
                .Get();

            var (users, vehicles) = await LoadLookups(res.Models);

            var enriched = new List<TripWithAvailability>();
            foreach (var t in res.Models)
            {
                var dto = ToAvailability(t, await GetSeatsTaken(t.Id));
                dto.Driver = users.GetValueOrDefault(t.DriverId);
                dto.Vehicle = t.VehicleId.HasValue
                    ? vehicles.GetValueOrDefault(t.VehicleId.Value)
                    : null;
                enriched.Add(dto);
            }

            return Ok(new
            {
                items = enriched,
                limit,
                offset,
                count = enriched.Count
            });
        }

        // GET BY ID — driver & vehicle denormalized
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Trip>()
                .Where(t => t.Id == id)
                .Limit(1)
                .Get();

            var trip = res.Models.FirstOrDefault();
            if (trip == null) return NotFound();

            var dto = ToAvailability(trip, await GetSeatsTaken(trip.Id));
            var (users, vehicles) = await LoadLookups(new[] { trip });
            dto.Driver = users.GetValueOrDefault(trip.DriverId);
            dto.Vehicle = trip.VehicleId.HasValue
                ? vehicles.GetValueOrDefault(trip.VehicleId.Value)
                : null;

            return Ok(dto);
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (string.IsNullOrWhiteSpace(req.FromLocation) ||
                string.IsNullOrWhiteSpace(req.ToLocation))
                return BadRequest("FromLocation and ToLocation are required.");

            if (req.EventTime <= DateTime.UtcNow)
                return BadRequest("EventTime must be in the future.");

            if (req.TotalSeats < 1 || req.TotalSeats > 20)
                return BadRequest("TotalSeats must be between 1 and 20.");

            if (req.PricePerSeat < 0)
                return BadRequest("PricePerSeat cannot be negative.");

            if (req.VehicleId.HasValue)
            {
                var vRes = await Sb.From<Vehicle>()
                    .Where(v => v.Id == req.VehicleId.Value && v.UserId == user.Id)
                    .Limit(1)
                    .Get();

                if (vRes.Models.FirstOrDefault() == null)
                    return BadRequest("Vehicle not found or not owned by you.");
            }

            var trip = new Trip
            {
                DriverId = user.Id,
                VehicleId = req.VehicleId,
                FromLocation = req.FromLocation.Trim(),
                ToLocation = req.ToLocation.Trim(),
                FromLat = req.FromLat,
                FromLng = req.FromLng,
                ToLat = req.ToLat,
                ToLng = req.ToLng,
                EventTime = req.EventTime,
                PricePerSeat = req.PricePerSeat,
                TotalSeats = req.TotalSeats,
                Description = req.Description,
                IsActive = true,
                IsComplete = false,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await Sb.From<Trip>().Insert(trip);
            var created = inserted.Models.First();

            var dto = ToAvailability(created, 0);
            var (users, vehicles) = await LoadLookups(new[] { created });
            dto.Driver = users.GetValueOrDefault(created.DriverId);
            dto.Vehicle = created.VehicleId.HasValue
                ? vehicles.GetValueOrDefault(created.VehicleId.Value)
                : null;

            return Ok(dto);
        }

        // UPDATE
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTripRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Trip>()
                .Where(t => t.Id == id && t.DriverId == user.Id)
                .Limit(1)
                .Get();

            var trip = res.Models.FirstOrDefault();
            if (trip == null) return NotFound();

            if (trip.IsComplete)
                return BadRequest("Cannot edit a completed trip.");

            if (req.VehicleId.HasValue)
            {
                var vRes = await Sb.From<Vehicle>()
                    .Where(v => v.Id == req.VehicleId.Value && v.UserId == user.Id)
                    .Limit(1)
                    .Get();

                if (vRes.Models.FirstOrDefault() == null)
                    return BadRequest("Vehicle not found or not owned by you.");
                trip.VehicleId = req.VehicleId.Value;
            }

            if (req.FromLocation != null) trip.FromLocation = req.FromLocation.Trim();
            if (req.ToLocation != null) trip.ToLocation = req.ToLocation.Trim();
            if (req.FromLat.HasValue) trip.FromLat = req.FromLat.Value;
            if (req.FromLng.HasValue) trip.FromLng = req.FromLng.Value;
            if (req.ToLat.HasValue) trip.ToLat = req.ToLat.Value;
            if (req.ToLng.HasValue) trip.ToLng = req.ToLng.Value;
            if (req.Description != null) trip.Description = req.Description;

            if (req.EventTime.HasValue)
            {
                if (req.EventTime.Value <= DateTime.UtcNow)
                    return BadRequest("EventTime must be in the future.");
                trip.EventTime = req.EventTime.Value;
            }

            if (req.PricePerSeat.HasValue)
            {
                if (req.PricePerSeat.Value < 0)
                    return BadRequest("PricePerSeat cannot be negative.");
                trip.PricePerSeat = req.PricePerSeat.Value;
            }

            if (req.TotalSeats.HasValue)
            {
                if (req.TotalSeats.Value < 1 || req.TotalSeats.Value > 20)
                    return BadRequest("TotalSeats must be between 1 and 20.");

                var taken = await GetSeatsTaken(trip.Id);
                if (req.TotalSeats.Value < taken)
                    return BadRequest($"TotalSeats cannot be less than seats already booked ({taken}).");

                trip.TotalSeats = req.TotalSeats.Value;
            }

            if (req.IsActive.HasValue) trip.IsActive = req.IsActive.Value;

            var updated = await Sb.From<Trip>().Update(trip);
            var outTrip = updated.Models.First();

            var dto = ToAvailability(outTrip, await GetSeatsTaken(outTrip.Id));
            var (users, vehicles) = await LoadLookups(new[] { outTrip });
            dto.Driver = users.GetValueOrDefault(outTrip.DriverId);
            dto.Vehicle = outTrip.VehicleId.HasValue
                ? vehicles.GetValueOrDefault(outTrip.VehicleId.Value)
                : null;

            return Ok(dto);
        }

        // CANCEL
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Trip>()
                .Where(t => t.Id == id && t.DriverId == user.Id)
                .Limit(1)
                .Get();

            var trip = res.Models.FirstOrDefault();
            if (trip == null) return NotFound();

            if (trip.IsComplete)
                return BadRequest("Cannot cancel a completed trip.");

            trip.IsActive = false;
            await Sb.From<Trip>().Update(trip);

            var cancelled = await CancelActiveBookingsForTrip(trip.Id);

            return Ok(new
            {
                message = "Trip cancelled.",
                tripId = trip.Id,
                bookingsCancelled = cancelled
            });
        }

        // COMPLETE
        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> Complete(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Trip>()
                .Where(t => t.Id == id && t.DriverId == user.Id)
                .Limit(1)
                .Get();

            var trip = res.Models.FirstOrDefault();
            if (trip == null) return NotFound();

            if (trip.IsComplete)
                return BadRequest("Trip already completed.");

            if (trip.EventTime > DateTime.UtcNow)
                return BadRequest("Cannot complete a trip before its departure time.");

            trip.IsComplete = true;
            trip.IsActive = false;
            await Sb.From<Trip>().Update(trip);

            return Ok(new { message = "Trip completed.", tripId = trip.Id });
        }

        private async Task<int> GetSeatsTaken(Guid tripId)
        {
            var res = await Sb.From<Booking>()
                .Where(b => b.TripId == tripId
                         && b.CancellationTime == null
                         && (b.Approval == "pending" || b.Approval == "approved"))
                .Get();

            return res.Models.Sum(b => b.SeatsRequested);
        }

        private async Task<int> CancelActiveBookingsForTrip(Guid tripId)
        {
            var res = await Sb.From<Booking>()
                .Where(b => b.TripId == tripId && b.CancellationTime == null)
                .Get();

            var count = 0;
            foreach (var b in res.Models)
            {
                b.CancellationTime = DateTime.UtcNow;
                if (b.Approval == "pending" || b.Approval == "approved")
                    b.Approval = "rejected";
                await Sb.From<Booking>().Update(b);
                count++;

                await Sb.CreateNotification(
                    b.PassengerId,
                    "booking",
                    "Trip cancelled",
                    "The trip you booked has been cancelled by the driver.");
            }
            return count;
        }

        private async Task<(Dictionary<Guid, PublicUserSummary> Users,
                            Dictionary<Guid, PublicVehicleSummary> Vehicles)>
            LoadLookups(IEnumerable<Trip> trips)
        {
            var tripList = trips.ToList();

            var driverIds = tripList.Select(t => t.DriverId).Distinct().ToList();
            var vehicleIds = tripList
                .Where(t => t.VehicleId.HasValue)
                .Select(t => t.VehicleId!.Value)
                .Distinct()
                .ToList();

            var users = new Dictionary<Guid, PublicUserSummary>();
            var vehicles = new Dictionary<Guid, PublicVehicleSummary>();

            foreach (var id in driverIds)
            {
                var r = await Sb.From<User>().Where(u => u.Id == id).Limit(1).Get();
                var u = r.Models.FirstOrDefault();
                if (u != null)
                    users[u.Id] = new PublicUserSummary
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        ProfilePicture = u.ProfilePicture
                    };
            }

            foreach (var id in vehicleIds)
            {
                var r = await Sb.From<Vehicle>().Where(v => v.Id == id).Limit(1).Get();
                var v = r.Models.FirstOrDefault();
                if (v != null)
                    vehicles[v.Id] = new PublicVehicleSummary
                    {
                        Id = v.Id,
                        Make = v.Make,
                        Model = v.Model,
                        Year = v.Year,
                        Color = v.Color,
                        LicensePlate = v.LicensePlate,
                        Seats = v.Seats
                    };
            }

            return (users, vehicles);
        }

        private static TripWithAvailability ToAvailability(Trip t, int taken) => new()
        {
            Id = t.Id,
            DriverId = t.DriverId,
            VehicleId = t.VehicleId,
            FromLocation = t.FromLocation,
            ToLocation = t.ToLocation,
            FromLat = t.FromLat,
            FromLng = t.FromLng,
            ToLat = t.ToLat,
            ToLng = t.ToLng,
            EventTime = t.EventTime,
            PricePerSeat = t.PricePerSeat,
            TotalSeats = t.TotalSeats,
            Description = t.Description,
            IsActive = t.IsActive,
            IsComplete = t.IsComplete,
            CreatedAt = t.CreatedAt,
            SeatsTaken = taken,
            SeatsRemaining = Math.Max(0, t.TotalSeats - taken)
        };
    }
}
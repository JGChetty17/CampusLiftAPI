using CampusLift.API.DTOs;
using CampusLift.API.Extensions;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

namespace CampusLift.API.Controllers
{
    [Route("api/bookings")]
    public class BookingsController : BaseApiController
    {
        public BookingsController(Supabase.Client sb) : base(sb) { }

        // ----------------------------------------------------------------
        // CREATE BOOKING (passenger) — atomic via book_seat RPC
        // ----------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            if (req.SeatsRequested < 1 || req.SeatsRequested > 10)
                return BadRequest("SeatsRequested must be between 1 and 10.");

            try
            {
                var rpc = await Sb.Rpc("book_seat", new
                {
                    p_trip_id = req.TripId,
                    p_passenger_id = user.Id,
                    p_seats = req.SeatsRequested
                });

                var json = rpc?.Content;
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return StatusCode(500, "Booking was created but could not be read.");

                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
                    }
                };
                var booking = Newtonsoft.Json.JsonConvert.DeserializeObject<Booking>(json, settings);

                if (booking == null)
                    return StatusCode(500, "Booking was created but could not be parsed.");

                var tripRes = await Sb.From<Trip>()
                    .Where(t => t.Id == req.TripId).Limit(1).Get();
                var trip = tripRes.Models.FirstOrDefault();
                if (trip != null)
                {
                    await Sb.CreateNotification(
                        trip.DriverId,
                        "booking",
                        "New booking request",
                        $"{user.Name} {user.Surname} wants to book {req.SeatsRequested} seat(s) on your trip.");
                }

                return Ok(booking);
            }
            catch (Exception ex)
            {
                var msg = ex.Message ?? "";

                if (msg.Contains("23505") || msg.Contains("duplicate key"))
                    return Conflict("You already have an active booking on this trip.");

                if (msg.Contains("Trip not found")) return NotFound("Trip not found.");
                if (msg.Contains("not active")) return BadRequest("Trip is not active.");
                if (msg.Contains("already completed")) return BadRequest("Trip already completed.");
                if (msg.Contains("own trip")) return BadRequest("You cannot book your own trip.");
                if (msg.Contains("Not enough seats")) return Conflict("Not enough seats available.");
                if (msg.Contains("already have an active booking"))
                    return Conflict("You already have an active booking on this trip.");

                return StatusCode(500, msg);
            }
        }

        // ----------------------------------------------------------------
        // MY BOOKINGS (as passenger) — trip summary inline
        // ----------------------------------------------------------------
        [HttpGet("mine")]
        public async Task<IActionResult> MyBookings()
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var res = await Sb.From<Booking>()
                .Where(b => b.PassengerId == user.Id)
                .Order(b => b.BookingTime, Constants.Ordering.Descending)
                .Get();

            var enriched = await EnrichWithTrips(res.Models, includePassenger: false);
            return Ok(enriched);
        }

        // ----------------------------------------------------------------
        // BOOKINGS FOR A TRIP (driver view) — passenger name/picture inline
        // ----------------------------------------------------------------
        [HttpGet("trip/{tripId:guid}")]
        public async Task<IActionResult> ForTrip(Guid tripId)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var tripRes = await Sb.From<Trip>()
                .Where(t => t.Id == tripId && t.DriverId == user.Id)
                .Limit(1)
                .Get();

            if (tripRes.Models.FirstOrDefault() == null)
                return NotFound("Trip not found or not owned by you.");

            var res = await Sb.From<Booking>()
                .Where(b => b.TripId == tripId)
                .Order(b => b.BookingTime, Constants.Ordering.Ascending)
                .Get();

            var enriched = await EnrichWithTrips(res.Models, includePassenger: true);
            return Ok(enriched);
        }

        // ----------------------------------------------------------------
        // APPROVE / REJECT (driver only)
        // ----------------------------------------------------------------
        [HttpPost("{id:guid}/approve")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveBookingRequest req)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var bookingRes = await Sb.From<Booking>()
                .Where(b => b.Id == id)
                .Limit(1)
                .Get();

            var booking = bookingRes.Models.FirstOrDefault();
            if (booking == null) return NotFound("Booking not found.");

            if (booking.CancellationTime != null)
                return BadRequest("Cannot approve a cancelled booking.");

            var tripRes = await Sb.From<Trip>()
                .Where(t => t.Id == booking.TripId && t.DriverId == user.Id)
                .Limit(1)
                .Get();

            if (tripRes.Models.FirstOrDefault() == null)
                return NotFound("Booking not found or you are not the driver.");

            booking.Approval = req.Approve ? "approved" : "rejected";
            booking.ApprovedTime = DateTime.UtcNow;

            var updated = await Sb.From<Booking>().Update(booking);

            await Sb.CreateNotification(
                booking.PassengerId,
                "booking",
                req.Approve ? "Booking approved" : "Booking rejected",
                req.Approve
                    ? "Your booking has been approved. See you on the trip!"
                    : "Your booking request was rejected by the driver.");

            return Ok(updated.Models.First());
        }

        // ----------------------------------------------------------------
        // CANCEL (passenger or driver)
        // ----------------------------------------------------------------
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var bookingRes = await Sb.From<Booking>()
                .Where(b => b.Id == id)
                .Limit(1)
                .Get();

            var booking = bookingRes.Models.FirstOrDefault();
            if (booking == null) return NotFound("Booking not found.");

            if (booking.CancellationTime != null)
                return BadRequest("Booking already cancelled.");

            var isPassenger = booking.PassengerId == user.Id;

            var tripRes = await Sb.From<Trip>()
                .Where(t => t.Id == booking.TripId && t.DriverId == user.Id)
                .Limit(1)
                .Get();
            var isDriver = tripRes.Models.FirstOrDefault() != null;

            if (!isPassenger && !isDriver)
                return NotFound("Booking not found.");

            booking.CancellationTime = DateTime.UtcNow;
            await Sb.From<Booking>().Update(booking);

            if (isDriver)
            {
                await Sb.CreateNotification(
                    booking.PassengerId,
                    "booking",
                    "Booking cancelled",
                    "The driver has cancelled your booking.");
            }
            else if (isPassenger)
            {
                var tripRes2 = await Sb.From<Trip>()
                    .Where(t => t.Id == booking.TripId).Limit(1).Get();
                var trip2 = tripRes2.Models.FirstOrDefault();
                if (trip2 != null)
                {
                    await Sb.CreateNotification(
                        trip2.DriverId,
                        "booking",
                        "Booking cancelled",
                        $"{user.Name} {user.Surname} cancelled their booking on your trip.");
                }
            }

            return Ok(new { message = "Booking cancelled.", bookingId = booking.Id });
        }

        // ----------------------------------------------------------------
        // PICKUP CONFIRM (passenger only)
        // ----------------------------------------------------------------
        [HttpPost("{id:guid}/pickup-confirm")]
        public async Task<IActionResult> ConfirmPickup(Guid id)
        {
            var user = await CurrentUser();
            if (user == null) return Unauthorized("Unknown Firebase UID.");

            var bookingRes = await Sb.From<Booking>()
                .Where(b => b.Id == id && b.PassengerId == user.Id)
                .Limit(1)
                .Get();

            var booking = bookingRes.Models.FirstOrDefault();
            if (booking == null) return NotFound("Booking not found.");

            if (booking.Approval != "approved")
                return BadRequest("Booking must be approved before confirming pickup.");

            if (booking.CancellationTime != null)
                return BadRequest("Booking is cancelled.");

            booking.PickupConfirmed = true;
            await Sb.From<Booking>().Update(booking);

            var tripRes = await Sb.From<Trip>()
                .Where(t => t.Id == booking.TripId).Limit(1).Get();
            var trip = tripRes.Models.FirstOrDefault();
            if (trip != null)
            {
                await Sb.CreateNotification(
                    trip.DriverId,
                    "booking",
                    "Passenger confirmed pickup",
                    $"{user.Name} {user.Surname} has confirmed pickup.");
            }

            return Ok(new { message = "Pickup confirmed.", bookingId = booking.Id });
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------

        /// <summary>
        /// Batch-enriches bookings with their trip summary, and optionally
        /// with the passenger's public profile (for the driver's view).
        /// </summary>
        private async Task<List<BookingWithTrip>> EnrichWithTrips(
            List<Booking> bookings,
            bool includePassenger)
        {
            if (bookings.Count == 0) return new();

            var tripIds = bookings.Select(b => b.TripId).Distinct().ToList();
            var passengerIds = includePassenger
                ? bookings.Select(b => b.PassengerId).Distinct().ToList()
                : new List<Guid>();

            var tripsById = new Dictionary<Guid, Trip>();
            var passengersById = new Dictionary<Guid, PublicUserSummary>();

            foreach (var id in tripIds)
            {
                var r = await Sb.From<Trip>().Where(t => t.Id == id).Limit(1).Get();
                var t = r.Models.FirstOrDefault();
                if (t != null) tripsById[t.Id] = t;
            }

            foreach (var id in passengerIds)
            {
                var r = await Sb.From<User>().Where(u => u.Id == id).Limit(1).Get();
                var u = r.Models.FirstOrDefault();
                if (u != null)
                    passengersById[u.Id] = new PublicUserSummary
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        ProfilePicture = u.ProfilePicture
                    };
            }

            var result = new List<BookingWithTrip>();
            foreach (var b in bookings)
            {
                tripsById.TryGetValue(b.TripId, out var trip);
                result.Add(new BookingWithTrip
                {
                    Id = b.Id,
                    TripId = b.TripId,
                    PassengerId = b.PassengerId,
                    SeatsRequested = b.SeatsRequested,
                    Approval = b.Approval,
                    ApprovedTime = b.ApprovedTime,
                    BookingTime = b.BookingTime,
                    CancellationTime = b.CancellationTime,
                    PickupConfirmed = b.PickupConfirmed,
                    FromLocation = trip?.FromLocation ?? "",
                    ToLocation = trip?.ToLocation ?? "",
                    EventTime = trip?.EventTime ?? default,
                    PricePerSeat = trip?.PricePerSeat ?? 0,
                    Passenger = includePassenger
                        ? passengersById.GetValueOrDefault(b.PassengerId)
                        : null
                });
            }
            return result;
        }
    }
}
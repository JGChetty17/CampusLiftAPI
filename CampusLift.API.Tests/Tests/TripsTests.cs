using System.Net;
using System.Net.Http.Json;
using CampusLift.API.DTOs;
using CampusLift.API.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CampusLift.API.Tests;
[Collection("Sequential")]
public class TripsTests : BaseIntegrationTest
{
    private readonly TestSeeder _seed;
    public TripsTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _seed = new TestSeeder(Client);
    }

    [Fact]
    public async Task Create_ValidTrip_ReturnsAvailability()
    {
        await _seed.CreateUser("driver");
        var trip = await _seed.CreateTrip("driver", seats: 3);

        trip.TotalSeats.Should().Be(3);
        trip.SeatsTaken.Should().Be(0);
        trip.SeatsRemaining.Should().Be(3);
        trip.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_PastEventTime_Returns400()
    {
        await _seed.CreateUser("driver");
        AsUser("driver");

        var resp = await PostJsonAsync("/api/trips", new CreateTripRequest(
            null, "A", "B", null, null, null, null,
            DateTime.UtcNow.AddHours(-1),
            50, 3, null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_Pagination_RespectsLimitOffset()
    {
        await _seed.CreateUser("driver");
        for (int i = 0; i < 5; i++)
            await _seed.CreateTrip("driver", from: $"A{i}", to: "B");

        AsUser("driver");
        var resp = await Client.GetAsync("/api/trips?limit=2&offset=0");
        var body = await resp.Content.ReadFromJsonAsync<TripListResponse>();

        body!.Items.Should().HaveCount(2);
        body.Limit.Should().Be(2);
        body.Offset.Should().Be(0);
    }

    [Fact]
    public async Task Update_ReduceSeatsBelowTaken_Returns400()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver", seats: 3);
        await _seed.BookSeat("passenger", trip.Id, seats: 2);

        AsUser("driver");
        var resp = await PatchJsonAsync($"/api/trips/{trip.Id}",
            new UpdateTripRequest(null, null, null, null, null, null, null,
                null, null, 1, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_CascadesToBookings()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("p1");
        await _seed.CreateUser("p2");
        var trip = await _seed.CreateTrip("driver", seats: 3);
        await _seed.BookSeat("p1", trip.Id);
        await _seed.BookSeat("p2", trip.Id);

        AsUser("driver");
        var resp = await Client.PostAsync($"/api/trips/{trip.Id}/cancel", null);
        resp.EnsureSuccessStatusCode();

        var list = await GetJsonAsync<List<BookingWithTrip>>($"/api/bookings/trip/{trip.Id}");
        list.Should().HaveCount(2);
        list.Should().OnlyContain(b => b.CancellationTime != null);
    }

    [Fact]
    public async Task Complete_BeforeEventTime_Returns400()
    {
        await _seed.CreateUser("driver");
        var trip = await _seed.CreateTrip("driver");

        AsUser("driver");
        var resp = await Client.PostAsync($"/api/trips/{trip.Id}/complete", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_AfterEventTime_Succeeds()
    {
        await _seed.CreateUser("driver");
        // Create a trip 1 second in future then wait
        var trip = await _seed.CreateTrip("driver",
            eventTime: DateTime.UtcNow.AddSeconds(2));
        await Task.Delay(2500);

        AsUser("driver");
        var resp = await Client.PostAsync($"/api/trips/{trip.Id}/complete", null);
        resp.EnsureSuccessStatusCode();

        var fetched = await GetJsonAsync<TripWithAvailability>($"/api/trips/{trip.Id}");
        fetched!.IsComplete.Should().BeTrue();
    }

    private class TripListResponse
    {
        public List<TripWithAvailability> Items { get; set; } = new();
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
    }
}
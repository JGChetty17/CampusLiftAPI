using System.Net;
using System.Net.Http.Json;
using CampusLift.API.DTOs;
using CampusLift.API.Models;
using CampusLift.API.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CampusLift.API.Tests;
[Collection("Sequential")]
public class BookingsTests : BaseIntegrationTest
{
    private readonly TestSeeder _seed;
    public BookingsTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _seed = new TestSeeder(Client);
    }

    [Fact]
    public async Task Book_HappyPath_ReturnsPending()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver", seats: 3);

        var booking = await _seed.BookSeat("passenger", trip.Id);

        booking.Approval.Should().Be("pending");
        booking.CancellationTime.Should().BeNull();
    }

    [Fact]
    public async Task Book_TwiceOnSameTrip_Returns409()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver");
        await _seed.BookSeat("passenger", trip.Id);

        AsUser("passenger");
        var second = await PostJsonAsync("/api/bookings",
            new CreateBookingRequest(trip.Id, 1));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Book_TooManySeats_Returns409()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver", seats: 2);

        AsUser("passenger");
        var resp = await PostJsonAsync("/api/bookings",
            new CreateBookingRequest(trip.Id, 3));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Book_DriverOwnTrip_Returns400()
    {
        await _seed.CreateUser("driver");
        var trip = await _seed.CreateTrip("driver");

        AsUser("driver");
        var resp = await PostJsonAsync("/api/bookings",
            new CreateBookingRequest(trip.Id, 1));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Approve_ByDriver_SetsApproved()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver");
        var booking = await _seed.BookSeat("passenger", trip.Id);

        AsUser("driver");
        var resp = await PostJsonAsync($"/api/bookings/{booking.Id}/approve",
            new ApproveBookingRequest(true));
        resp.EnsureSuccessStatusCode();

        var updated = await resp.Content.ReadFromJsonAsync<Booking>();
        updated!.Approval.Should().Be("approved");
        updated.ApprovedTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_ByNonDriver_Returns404()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        await _seed.CreateUser("nosy");
        var trip = await _seed.CreateTrip("driver");
        var booking = await _seed.BookSeat("passenger", trip.Id);

        AsUser("nosy");
        var resp = await PostJsonAsync($"/api/bookings/{booking.Id}/approve",
            new ApproveBookingRequest(true));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PickupConfirm_BeforeApproval_Returns400()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver");
        var booking = await _seed.BookSeat("passenger", trip.Id);

        AsUser("passenger");
        var resp = await Client.PostAsync($"/api/bookings/{booking.Id}/pickup-confirm", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PickupConfirm_AfterApproval_Succeeds()
    {
        await _seed.CreateUser("driver");
        await _seed.CreateUser("passenger");
        var trip = await _seed.CreateTrip("driver");
        var booking = await _seed.BookSeat("passenger", trip.Id);

        AsUser("driver");
        await PostJsonAsync($"/api/bookings/{booking.Id}/approve",
            new ApproveBookingRequest(true));

        AsUser("passenger");
        var resp = await Client.PostAsync($"/api/bookings/{booking.Id}/pickup-confirm", null);
        resp.EnsureSuccessStatusCode();
    }
}
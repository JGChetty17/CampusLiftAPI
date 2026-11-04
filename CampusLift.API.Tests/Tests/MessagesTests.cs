using System.Net;
using System.Net.Http.Json;
using CampusLift.API.DTOs;
using CampusLift.API.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CampusLift.API.Tests;

[Collection("Sequential")]
public class MessagesTests : BaseIntegrationTest
{
    private readonly TestSeeder _seed;

    public MessagesTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _seed = new TestSeeder(Client);
    }

    /// <summary>
    /// Creates driver + approved passenger + trip. Returns IDs needed for the tests.
    /// </summary>
    private async Task<(Guid tripId, string driverUid, string passengerUid)> SetupApprovedTrip()
    {
        var driverUid = $"driver-msg-{Guid.NewGuid():N}";
        var passengerUid = $"pass-msg-{Guid.NewGuid():N}";

        await _seed.CreateUser(driverUid);
        await _seed.CreateUser(passengerUid);
        var trip = await _seed.CreateTrip(driverUid);

        var booking = await _seed.BookSeat(passengerUid, trip.Id);

        // Approve the booking as driver
        AsUser(driverUid);
        var approveResp = await PostJsonAsync($"/api/bookings/{booking.Id}/approve",
            new ApproveBookingRequest(true));
        approveResp.EnsureSuccessStatusCode();

        return (trip.Id, driverUid, passengerUid);
    }

    // ─────────────── SUCCESS CASES ───────────────

    [Fact]
    public async Task Send_AsDriver_Succeeds()
    {
        var (tripId, driverUid, _) = await SetupApprovedTrip();

        AsUser(driverUid);
        var resp = await PostJsonAsync($"/api/trips/{tripId}/messages",
            new SendMessageRequest("Hello from driver"));

        resp.EnsureSuccessStatusCode();
        var msg = await resp.Content.ReadFromJsonAsync<MessageResponse>();
        msg!.Content.Should().Be("Hello from driver");
        msg.IsRead.Should().BeFalse();
        msg.Sender.Should().NotBeNull();
        msg.Sender!.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Send_AsApprovedPassenger_Succeeds()
    {
        var (tripId, _, passengerUid) = await SetupApprovedTrip();

        AsUser(passengerUid);
        var resp = await PostJsonAsync($"/api/trips/{tripId}/messages",
            new SendMessageRequest("Hello from passenger"));

        resp.EnsureSuccessStatusCode();
    }

    // ─────────────── VALIDATION ───────────────

    [Fact]
    public async Task Send_EmptyContent_Returns400()
    {
        var (tripId, driverUid, _) = await SetupApprovedTrip();

        AsUser(driverUid);
        var resp = await PostJsonAsync($"/api/trips/{tripId}/messages",
            new SendMessageRequest("   "));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Send_TooLongContent_Returns400()
    {
        var (tripId, driverUid, _) = await SetupApprovedTrip();

        AsUser(driverUid);
        var resp = await PostJsonAsync($"/api/trips/{tripId}/messages",
            new SendMessageRequest(new string('x', 501)));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────── AUTHORIZATION ───────────────

    [Fact]
    public async Task Send_NotOnTrip_Returns403()
    {
        var (tripId, _, _) = await SetupApprovedTrip();

        var outsiderUid = $"outsider-{Guid.NewGuid():N}";
        await _seed.CreateUser(outsiderUid);

        AsUser(outsiderUid);
        var resp = await PostJsonAsync($"/api/trips/{tripId}/messages",
            new SendMessageRequest("Not my trip"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Send_TripNotFound_Returns404()
    {
        var driverUid = $"driver-404-{Guid.NewGuid():N}";
        await _seed.CreateUser(driverUid);

        AsUser(driverUid);
        var resp = await PostJsonAsync($"/api/trips/{Guid.NewGuid()}/messages",
            new SendMessageRequest("Hello"));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────── READ-ONLY TRIP ───────────────

    [Fact]
    public async Task Send_ReadOnlyTrip_Returns400()
    {
        var (tripId, driverUid, _) = await SetupApprovedTrip();

        AsUser(driverUid);
        var cancelResp = await Client.PostAsync($"/api/trips/{tripId}/cancel", null);
        cancelResp.EnsureSuccessStatusCode();

        AsUser(driverUid);
        var resp = await PostJsonAsync($"/api/trips/{tripId}/messages",
            new SendMessageRequest("Should fail"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────── LISTING ───────────────

    [Fact]
    public async Task List_ReturnsMessagesOldestFirst()
    {
        var (tripId, driverUid, _) = await SetupApprovedTrip();

        AsUser(driverUid);
        await PostJsonAsync($"/api/trips/{tripId}/messages", new SendMessageRequest("First"));
        await Task.Delay(1100);
        await PostJsonAsync($"/api/trips/{tripId}/messages", new SendMessageRequest("Second"));

        AsUser(driverUid);
        var list = await GetJsonAsync<MessageListResponse>($"/api/trips/{tripId}/messages");

        list!.Items.Should().HaveCount(2);
        list.Items[0].Content.Should().Be("First");
        list.Items[1].Content.Should().Be("Second");
        list.ReadOnly.Should().BeFalse();
    }

    [Fact]
    public async Task List_ReadOnlyTrip_SetsFlag()
    {
        var (tripId, driverUid, _) = await SetupApprovedTrip();

        AsUser(driverUid);
        await Client.PostAsync($"/api/trips/{tripId}/cancel", null);

        AsUser(driverUid);
        var list = await GetJsonAsync<MessageListResponse>($"/api/trips/{tripId}/messages");

        list!.ReadOnly.Should().BeTrue();
    }
}
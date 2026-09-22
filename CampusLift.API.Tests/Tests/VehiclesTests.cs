using System.Net;
using System.Net.Http.Json;
using CampusLift.API.DTOs;
using CampusLift.API.Models;
using CampusLift.API.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CampusLift.API.Tests;
[Collection("Sequential")]
public class VehiclesTests : BaseIntegrationTest
{
    private readonly TestSeeder _seed;

    public VehiclesTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _seed = new TestSeeder(Client);
    }

    [Fact]
    public async Task Create_ReturnsVehicle()
    {
        await _seed.CreateUser("uid-a");
        AsUser("uid-a");

        var resp = await PostJsonAsync("/api/vehicles", new CreateVehicleRequest(
            "Toyota", "Corolla", 2020, "White", "CA-111", 4));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var v = await resp.Content.ReadFromJsonAsync<Vehicle>();
        v!.Make.Should().Be("Toyota");
        v.Seats.Should().Be(4);
    }

    [Fact]
    public async Task Create_MissingMake_Returns400()
    {
        await _seed.CreateUser("uid-a");
        AsUser("uid-a");

        var resp = await PostJsonAsync("/api/vehicles", new CreateVehicleRequest(
            "", "Corolla", 2020, "White", "CA-111", 4));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnVehicles()
    {
        await _seed.CreateUser("uid-a");
        await _seed.CreateUser("uid-b");
        await _seed.CreateVehicle("uid-a");
        await _seed.CreateVehicle("uid-b");

        AsUser("uid-a");
        var mine = await GetJsonAsync<List<Vehicle>>("/api/vehicles");

        mine.Should().HaveCount(1);
    }

    [Fact]
    public async Task Update_OtherUsersVehicle_Returns404()
    {
        await _seed.CreateUser("uid-a");
        await _seed.CreateUser("uid-b");
        var v = await _seed.CreateVehicle("uid-a");

        AsUser("uid-b");
        var resp = await PatchJsonAsync($"/api/vehicles/{v.Id}",
            new UpdateVehicleRequest(null, null, null, "Red", null, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OtherUsersVehicle_Returns404()
    {
        await _seed.CreateUser("uid-a");
        await _seed.CreateUser("uid-b");
        var v = await _seed.CreateVehicle("uid-a");

        AsUser("uid-b");
        var resp = await Client.DeleteAsync($"/api/vehicles/{v.Id}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_UsedByActiveTrip_Returns409()
    {
        await _seed.CreateUser("uid-a");
        AsUser("uid-a");

        var vehicle = await _seed.CreateVehicle("uid-a");

        // Create a trip that uses the vehicle
        var tripResp = await PostJsonAsync("/api/trips", new CreateTripRequest(
            VehicleId: vehicle.Id,
            FromLocation: "A", ToLocation: "B",
            FromLat: null, FromLng: null, ToLat: null, ToLng: null,
            EventTime: DateTime.UtcNow.AddDays(1),
            PricePerSeat: 10, TotalSeats: 3, Description: null));
        tripResp.EnsureSuccessStatusCode();

        var del = await Client.DeleteAsync($"/api/vehicles/{vehicle.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_UnusedVehicle_Succeeds()
    {
        await _seed.CreateUser("uid-a");
        var v = await _seed.CreateVehicle("uid-a");

        AsUser("uid-a");
        var resp = await Client.DeleteAsync($"/api/vehicles/{v.Id}");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
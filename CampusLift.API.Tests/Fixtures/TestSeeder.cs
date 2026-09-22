using System.Net.Http.Json;
using CampusLift.API.DTOs;
using CampusLift.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CampusLift.API.Tests.Fixtures;

public class TestSeeder
{
    private readonly HttpClient _client;

    public TestSeeder(HttpClient client)
    {
        _client = client;
    }

    public async Task<User> CreateUser(string uid)
    {
        var resp = await _client.PostAsJsonAsync("/api/users/sync",
            new SyncUserRequest(uid, $"{uid}@test.local", uid, "Tester"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<User>())!;
    }

    public async Task<Vehicle> CreateVehicle(string uid)
    {
        SetUser(uid);
        var resp = await _client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest(
            Make: "Toyota",
            Model: "Corolla",
            Year: 2020,
            Color: "White",
            LicensePlate: $"CA-{Guid.NewGuid().ToString("N")[..6]}",
            Seats: 4));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<Vehicle>())!;
    }

    public async Task<TripWithAvailability> CreateTrip(
        string driverUid,
        int seats = 3,
        DateTime? eventTime = null,
        string from = "Campus",
        string to = "City")
    {
        SetUser(driverUid);
        var resp = await _client.PostAsJsonAsync("/api/trips", new CreateTripRequest(
            VehicleId: null,
            FromLocation: from,
            ToLocation: to,
            FromLat: null, FromLng: null, ToLat: null, ToLng: null,
            EventTime: eventTime ?? DateTime.UtcNow.AddDays(1),
            PricePerSeat: 50,
            TotalSeats: seats,
            Description: null));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TripWithAvailability>())!;
    }

    public async Task<Booking> BookSeat(string passengerUid, Guid tripId, int seats = 1)
    {
        SetUser(passengerUid);
        var resp = await _client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(tripId, seats));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<Booking>())!;
    }

    private void SetUser(string uid)
    {
        _client.DefaultRequestHeaders.Remove("X-Firebase-Uid");
        _client.DefaultRequestHeaders.Add("X-Firebase-Uid", uid);
    }
}
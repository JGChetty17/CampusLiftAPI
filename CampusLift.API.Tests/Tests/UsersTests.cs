using System.Net;
using System.Net.Http.Json;
using CampusLift.API.DTOs;
using CampusLift.API.Models;
using FluentAssertions;
using Xunit;

namespace CampusLift.API.Tests;
[Collection("Sequential")]
public class UsersTests : BaseIntegrationTest
{
    public UsersTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Sync_CreatesUser_ThenReturnsSameOnSecondCall()
    {
        var req = new SyncUserRequest("uid-1", "a@b.com", "Alice", "Smith");

        var first = await PostJsonAsync("/api/users/sync", req);
        first.EnsureSuccessStatusCode();
        var user1 = await first.Content.ReadFromJsonAsync<User>();

        var second = await PostJsonAsync("/api/users/sync", req);
        second.EnsureSuccessStatusCode();
        var user2 = await second.Content.ReadFromJsonAsync<User>();

        user2!.Id.Should().Be(user1!.Id);
    }

    [Fact]
    public async Task Sync_WithoutFirebaseUid_Returns400()
    {
        var resp = await PostJsonAsync("/api/users/sync",
            new SyncUserRequest("", null, null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_WithoutHeader_Returns401()
    {
        var resp = await Client.GetAsync("/api/users/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithUnknownUid_Returns401()
    {
        AsUser("ghost-uid");
        var resp = await Client.GetAsync("/api/users/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithKnownUid_ReturnsUser()
    {
        await PostJsonAsync("/api/users/sync",
            new SyncUserRequest("uid-me", "m@x.com", "Mike", "Jones"));
        AsUser("uid-me");

        var user = await GetJsonAsync<User>("/api/users/me");

        user!.FirebaseUid.Should().Be("uid-me");
        user.Name.Should().Be("Mike");
    }

    [Fact]
    public async Task Update_OnlyModifiesProvidedFields()
    {
        await PostJsonAsync("/api/users/sync",
            new SyncUserRequest("uid-upd", "u@x.com", "Old", "Name"));
        AsUser("uid-upd");

        var resp = await PatchJsonAsync("/api/users/me", new UpdateUserRequest(
            Name: "New",
            Surname: null,
            StudentNumber: null,
            University: "UCT",
            ProfilePicture: null,
            EmergencyContact: null,
            Language: null,
            DarkMode: true,
            BiometricEnabled: null,
            NotificationEnabled: null));
        resp.EnsureSuccessStatusCode();

        var updated = await GetJsonAsync<User>("/api/users/me");

        updated!.Name.Should().Be("New");
        updated.Surname.Should().Be("Name");       // unchanged
        updated.University.Should().Be("UCT");
        updated.DarkMode.Should().BeTrue();
    }
}
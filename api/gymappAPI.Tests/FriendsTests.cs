using System.Net;
using System.Net.Http.Json;
using gymappAPI.DTOs;

namespace gymappAPI.Tests;

public class FriendsTests : IClassFixture<GymAppFactory>
{
    private readonly GymAppFactory _factory;

    public FriendsTests(GymAppFactory factory) => _factory = factory;

    [Fact]
    public async Task ARequest_CanBeAcceptedAndTheFriendshipEnded()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);

        await TestUsers.MakeFriendsAsync(sender, receiver);
        Assert.Equal(FriendshipStatus.Friends, await StatusAsync(sender, receiver));

        Assert.Equal(HttpStatusCode.NoContent, (await sender.Client.DeleteAsync($"api/friends/{receiver.Id}")).StatusCode);
        Assert.Equal(FriendshipStatus.None, await StatusAsync(sender, receiver));
    }

    [Fact]
    public async Task ARequest_CanBeDeclined()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await sender.Client.PostAsJsonAsync("api/friends/requests", new { receiverId = receiver.Id });
        Assert.Equal(FriendshipStatus.PendingReceived, await StatusAsync(receiver, sender));

        var decline = await receiver.Client.DeleteAsync($"api/friends/requests/{sender.Id}");

        Assert.Equal(HttpStatusCode.NoContent, decline.StatusCode);
        Assert.Equal(FriendshipStatus.None, await StatusAsync(sender, receiver));
    }

    [Fact]
    public async Task SendingTheSameRequestTwice_ReturnsConflict()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await sender.Client.PostAsJsonAsync("api/friends/requests", new { receiverId = receiver.Id });

        var again = await sender.Client.PostAsJsonAsync("api/friends/requests", new { receiverId = receiver.Id });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task FriendsList_ContainsTheFriend()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(sender, receiver);

        var friends = await sender.Client.GetFromJsonAsync<UserDto[]>($"api/users/{sender.Id}/friends");

        Assert.Equal(receiver.Username, Assert.Single(friends!).Username);
    }

    private static Task<FriendshipStatus> StatusAsync(TestUser user, TestUser other) =>
        user.Client.GetFromJsonAsync<FriendshipStatus>($"api/friends/{other.Id}/status");
}

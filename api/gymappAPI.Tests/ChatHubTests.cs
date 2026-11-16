using System.Net.Http.Json;
using gymappAPI.DTOs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace gymappAPI.Tests;

public class ChatHubTests : IClassFixture<GymAppFactory>
{
    private readonly GymAppFactory _factory;

    public ChatHubTests(GymAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Connecting_WithoutAToken_Fails()
    {
        await using var connection = Connect(accessToken: null);

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task SendingToANonFriend_Fails()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var stranger = await TestUsers.CreateAsync(_factory);
        await using var connection = Connect(sender.Tokens.AccessToken);
        await connection.StartAsync();

        var error = await Assert.ThrowsAsync<HubException>(() =>
            connection.InvokeAsync("SendMessage", stranger.Id, "hi"));
        Assert.Contains("friends", error.Message);
    }

    [Fact]
    public async Task SendingToAFriend_DeliversTheMessageToBoth()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(sender, receiver);

        await using var senderConnection = Connect(sender.Tokens.AccessToken);
        await using var receiverConnection = Connect(receiver.Tokens.AccessToken);
        var received = new TaskCompletionSource<ChatMessageDto>();
        receiverConnection.On<ChatMessageDto>("ReceiveMessage", message => received.TrySetResult(message));
        await senderConnection.StartAsync();
        await receiverConnection.StartAsync();

        await senderConnection.InvokeAsync("SendMessage", receiver.Id, "  hello  ");

        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("hello", message.Content);
        Assert.Equal(sender.Id, message.SenderId);
    }

    [Fact]
    public async Task SentMessages_AppearInTheHistoryOfBothUsers()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(sender, receiver);
        await using var connection = Connect(sender.Tokens.AccessToken);
        await connection.StartAsync();
        await connection.InvokeAsync("SendMessage", receiver.Id, "first");
        await connection.InvokeAsync("SendMessage", receiver.Id, "second");

        var history = await receiver.Client.GetFromJsonAsync<ChatHistoryDto>($"api/chats/{sender.Id}/messages?limit=1");

        Assert.Equal("second", Assert.Single(history!.Messages).Content);
        Assert.True(history.HasMore);
    }

    [Fact]
    public async Task HistoryTimestamps_AreMarkedAsUtc()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(sender, receiver);
        await using var connection = Connect(sender.Tokens.AccessToken);
        await connection.StartAsync();
        await connection.InvokeAsync("SendMessage", receiver.Id, "hello");

        var history = await receiver.Client.GetFromJsonAsync<ChatHistoryDto>($"api/chats/{sender.Id}/messages");

        // Without the UTC marker, browsers read the time as local and show it hours off.
        Assert.Equal(DateTimeKind.Utc, Assert.Single(history!.Messages).Timestamp.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendingAnEmptyMessage_Fails(string content)
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(sender, receiver);
        await using var connection = Connect(sender.Tokens.AccessToken);
        await connection.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("SendMessage", receiver.Id, content));
    }

    [Fact]
    public async Task SendingATooLongMessage_Fails()
    {
        var sender = await TestUsers.CreateAsync(_factory);
        var receiver = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(sender, receiver);
        await using var connection = Connect(sender.Tokens.AccessToken);
        await connection.StartAsync();

        await Assert.ThrowsAsync<HubException>(() =>
            connection.InvokeAsync("SendMessage", receiver.Id, new string('a', 2001)));
    }

    private HubConnection Connect(string? accessToken) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "chatHub"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (accessToken is not null)
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using gymappAPI.DTOs;

namespace gymappAPI.Tests;

public record TestUser(Guid Id, string Username, string Password, HttpClient Client, TokenResponseDto Tokens);

public static class TestUsers
{
    public const string Password = "secret-password";

    public static string NewUsername() => $"user_{Guid.NewGuid():N}"[..20];

    /// <summary>Registers and logs in a fresh user, returning a client that sends their access token.</summary>
    public static async Task<TestUser> CreateAsync(GymAppFactory factory)
    {
        var username = NewUsername();
        var anonymous = factory.CreateClient();

        var register = await anonymous.PostAsJsonAsync("api/auth/register", new { username, password = Password });
        register.EnsureSuccessStatusCode();

        var tokens = await LoginAsync(anonymous, username);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var profile = await client.GetFromJsonAsync<UserProfileDto>("api/users/me");
        return new TestUser(profile!.Id, username, Password, client, tokens);
    }

    public static async Task MakeFriendsAsync(TestUser sender, TestUser receiver)
    {
        (await sender.Client.PostAsJsonAsync("api/friends/requests", new { receiverId = receiver.Id })).EnsureSuccessStatusCode();
        (await receiver.Client.PostAsync($"api/friends/requests/{sender.Id}/accept", null)).EnsureSuccessStatusCode();
    }

    public static async Task<TokenResponseDto> LoginAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("api/auth/login", new { username, password = Password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<TokenResponseDto>())!;
    }
}

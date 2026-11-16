using System.Net;
using System.Net.Http.Json;
using gymappAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Tests;

public class AuthTests : IClassFixture<GymAppFactory>
{
    private readonly GymAppFactory _factory;

    public AuthTests(GymAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Profile_DoesNotExposePasswordHashOrTokens()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var json = await user.Client.GetStringAsync("api/users/me");

        Assert.Contains(user.Username, json);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithTakenUsername_ReturnsBadRequest()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var response = await _factory.CreateClient().PostAsJsonAsync("api/auth/register",
            new { username = user.Username, password = TestUsers.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("ab", "valid-password")]
    [InlineData("valid_name", "ab")]
    public async Task Register_WithTooShortCredentials_ReturnsBadRequest(string username, string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("api/auth/register", new { username, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var response = await _factory.CreateClient().PostAsJsonAsync("api/auth/login",
            new { username = user.Username, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesTheToken()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var refreshed = await RefreshAsync(user.Tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var tokens = await refreshed.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotEqual(user.Tokens.RefreshToken, tokens!.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusingAJustUsedToken_IsAccepted()
    {
        // Two tabs refreshing at the same time both send the same token.
        var user = await TestUsers.CreateAsync(_factory);

        var first = await RefreshAsync(user.Tokens.RefreshToken);
        var second = await RefreshAsync(user.Tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusingATokenAfterTheGracePeriod_IsRejected()
    {
        var user = await TestUsers.CreateAsync(_factory);
        await RefreshAsync(user.Tokens.RefreshToken);

        // Pretend the token was used a minute ago.
        await _factory.WithDbAsync(db => db.RefreshTokens
            .Where(t => t.UsedAt != null && t.UserId == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTime.UtcNow.AddMinutes(-1))));

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(user.Tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync("not-a-real-token")).StatusCode);
    }

    [Fact]
    public async Task LoggingInOnASecondDevice_KeepsTheFirstOneLoggedIn()
    {
        var user = await TestUsers.CreateAsync(_factory);

        await TestUsers.LoginAsync(_factory.CreateClient(), user.Username);

        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(user.Tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesOnlyThatDevicesRefreshToken()
    {
        var user = await TestUsers.CreateAsync(_factory);
        var otherDevice = await TestUsers.LoginAsync(_factory.CreateClient(), user.Username);

        var logout = await _factory.CreateClient().PostAsJsonAsync("api/auth/logout",
            new { refreshToken = user.Tokens.RefreshToken });

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(user.Tokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(otherDevice.RefreshToken)).StatusCode);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _factory.CreateClient().PostAsJsonAsync("api/auth/refresh", new { refreshToken });
}

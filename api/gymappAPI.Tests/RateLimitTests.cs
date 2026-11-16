using System.Net;
using System.Net.Http.Json;

namespace gymappAPI.Tests;

public class LowAuthLimitFactory : GymAppFactory
{
    protected override int AuthPermitLimit => 3;
}

public class RateLimitTests : IClassFixture<LowAuthLimitFactory>
{
    private readonly LowAuthLimitFactory _factory;

    public RateLimitTests(LowAuthLimitFactory factory) => _factory = factory;

    [Fact]
    public async Task TooManyLoginAttempts_Return429WithAMessage_WithoutBlockingOtherEndpoints()
    {
        var client = _factory.CreateClient();
        var attempt = new { username = "nobody", password = "wrong-password" };
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/auth/login", attempt)).StatusCode);

        var blocked = await client.PostAsJsonAsync("api/auth/login", attempt);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        var body = await blocked.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Contains("Too many", body!["message"]);

        var refresh = await client.PostAsJsonAsync("api/auth/refresh", new { refreshToken = "unknown" });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}

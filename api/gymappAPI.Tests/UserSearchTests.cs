using System.Net.Http.Json;
using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Tests;

public class UserSearchTests : IClassFixture<GymAppFactory>
{
    private readonly GymAppFactory _factory;

    public UserSearchTests(GymAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Search_ReturnsAtMost20Users_SortedByUsername()
    {
        var user = await TestUsers.CreateAsync(_factory);
        var prefix = $"s{Guid.NewGuid():N}"[..12];
        await _factory.WithDbAsync(async db =>
        {
            db.Users.AddRange(Enumerable.Range(0, 25).Select(i => new User { Username = $"{prefix}{i:D2}" }));
            await db.SaveChangesAsync();
        });

        var results = await user.Client.GetFromJsonAsync<UserDto[]>($"api/users?search={prefix}");

        Assert.Equal(20, results!.Length);
        Assert.Equal($"{prefix}00", results[0].Username);
        Assert.Equal($"{prefix}19", results[^1].Username);
    }
}

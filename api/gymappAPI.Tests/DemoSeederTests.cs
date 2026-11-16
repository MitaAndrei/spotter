using System.Net.Http.Headers;
using System.Net.Http.Json;
using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Tests;

/// <summary>The demo account of the live demo, created by DemoSeeder.</summary>
public class DemoSeederTests : IClassFixture<GymAppFactory>
{
    private const string DemoPassword = "demo-password";

    private readonly GymAppFactory _factory;

    public DemoSeederTests(GymAppFactory factory) => _factory = factory;

    [Fact]
    public async Task TheDemoUser_CanLogIn_AndSeesWorkoutsMealsAndAFriendsChat()
    {
        await _factory.WithDbAsync(db => DemoSeeder.SeedAsync(db, DemoPassword));

        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("api/auth/login",
            new { username = DemoSeeder.DemoUsername, password = DemoPassword });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var today = DateTime.UtcNow.Date;
        var workouts = await client.GetFromJsonAsync<Workout[]>(
            $"api/workouts?from={today.AddDays(-364):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        Assert.True(workouts!.Length > 100);

        var templates = await client.GetFromJsonAsync<Workout[]>("api/workouts/templates");
        Assert.Equal(3, templates!.Length);

        var meals = await client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?date={today:yyyy-MM-dd}");
        Assert.NotEmpty(meals!);

        var friend = await client.GetFromJsonAsync<UserProfileDto>("api/users/alex");
        var chat = await client.GetFromJsonAsync<ChatHistoryDto>($"api/chats/{friend!.Id}/messages");
        Assert.Equal(4, chat!.Messages.Count());
    }

    [Fact]
    public async Task SeedingTwice_DoesNotDuplicateTheData()
    {
        await _factory.WithDbAsync(db => DemoSeeder.SeedAsync(db, DemoPassword));
        int workoutCount = 0;
        await _factory.WithDbAsync(async db => workoutCount = await db.Workouts.CountAsync());

        await _factory.WithDbAsync(db => DemoSeeder.SeedAsync(db, DemoPassword));

        await _factory.WithDbAsync(async db =>
        {
            Assert.Equal(workoutCount, await db.Workouts.CountAsync());
            Assert.Equal(1, await db.Users.CountAsync(u => u.Username == DemoSeeder.DemoUsername));
        });
    }
}

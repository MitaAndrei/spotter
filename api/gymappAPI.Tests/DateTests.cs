using System.Net.Http.Json;
using gymappAPI.DTOs;
using gymappAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Tests;

/// <summary>Workouts and logged foods belong to a calendar day, not to a moment in time.</summary>
public class DateTests : IClassFixture<GymAppFactory>
{
    private const string Day = "2026-11-05";
    private const string NextDay = "2026-11-06";
    private const string PreviousDay = "2026-11-04";

    private readonly GymAppFactory _factory;

    public DateTests(GymAppFactory factory) => _factory = factory;

    [Fact]
    public async Task ALoggedFood_AppearsOnlyOnTheDayItWasLoggedFor()
    {
        var user = await TestUsers.CreateAsync(_factory);

        await LogFoodAsync(user, Day);

        Assert.Single(await GetFoodsForDayAsync(user, Day));
        Assert.Empty(await GetFoodsForDayAsync(user, NextDay));
        Assert.Empty(await GetFoodsForDayAsync(user, PreviousDay));
    }

    [Fact]
    public async Task ALoggedFood_IsStoredWithoutATimeOfDay()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var logged = await LogFoodAsync(user, $"{Day}T23:30:00");

        Assert.Equal(new DateTime(2026, 11, 5), logged.Date);
    }

    [Fact]
    public async Task ADateRange_IncludesAllOfItsLastDay()
    {
        var user = await TestUsers.CreateAsync(_factory);
        var logged = await LogFoodAsync(user, Day);

        // Entries saved before dates were normalized still have a time of day.
        await _factory.WithDbAsync(db => db.LoggedFoods.Where(f => f.Id == logged.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Date, new DateTime(2026, 11, 5, 20, 30, 0))));

        Assert.Single((await user.Client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?from={Day}&to={Day}"))!);
        Assert.Single(await GetFoodsForDayAsync(user, Day));
        Assert.Empty((await user.Client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?from={NextDay}&to={NextDay}"))!);
    }

    [Fact]
    public async Task AWorkout_KeepsItsDayAndIsFoundByARangeEndingThatDay()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var response = await user.Client.PostAsJsonAsync("api/workouts",
            new { description = "Legs", duration = 60, musclesTargeted = new[] { "Legs" }, date = Day });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Workout>();

        Assert.Equal(new DateTime(2026, 11, 5), created!.Date);
        Assert.Single((await user.Client.GetFromJsonAsync<Workout[]>($"api/workouts?from={Day}&to={Day}"))!);
        Assert.Empty((await user.Client.GetFromJsonAsync<Workout[]>($"api/workouts?from={NextDay}&to={NextDay}"))!);
    }

    private static async Task<LoggedFoodDto> LogFoodAsync(TestUser user, string date)
    {
        var create = await user.Client.PostAsJsonAsync("api/foods",
            new { name = $"Rice {Guid.NewGuid():N}", calories = 130, protein = 2.7, fats = 0.3, carbs = 28 });
        var food = await create.Content.ReadFromJsonAsync<Food>();

        var log = await user.Client.PostAsJsonAsync($"api/logged-foods?date={date}", new[] { new { food, grams = 100 } });
        log.EnsureSuccessStatusCode();
        return (await log.Content.ReadFromJsonAsync<LoggedFoodDto[]>())!.Single();
    }

    private static async Task<LoggedFoodDto[]> GetFoodsForDayAsync(TestUser user, string day) =>
        (await user.Client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?date={day}"))!;
}

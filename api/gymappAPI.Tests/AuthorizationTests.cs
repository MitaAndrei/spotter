using System.Net;
using System.Net.Http.Json;
using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Tests;

/// <summary>Users can only see and change their own data.</summary>
public class AuthorizationTests : IClassFixture<GymAppFactory>
{
    private readonly GymAppFactory _factory;

    public AuthorizationTests(GymAppFactory factory) => _factory = factory;

    [Theory]
    [InlineData("GET", "api/users/me")]
    [InlineData("GET", "api/users?search=a")]
    [InlineData("GET", "api/users/someone")]
    [InlineData("GET", "api/workouts")]
    [InlineData("GET", "api/foods")]
    [InlineData("DELETE", "api/images/some-file.png")]
    [InlineData("GET", "api/images/some-file.png/url")]
    public async Task ProtectedEndpoints_RequireLogin(string method, string url)
    {
        var response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeletingSomeoneElsesImage_IsForbidden()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var other = await TestUsers.CreateAsync(_factory);
        var fileKey = await UploadImageAsync(owner);

        var response = await other.Client.DeleteAsync($"api/images/{fileKey}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(fileKey, _factory.S3.DeletedKeys);
    }

    [Fact]
    public async Task UploadingANewImage_DeletesThePreviousOne()
    {
        var user = await TestUsers.CreateAsync(_factory);
        var first = await UploadImageAsync(user);

        var second = await UploadImageAsync(user);

        Assert.NotEqual(first, second);
        Assert.Contains(first, _factory.S3.DeletedKeys);
    }

    [Fact]
    public async Task EditingOrDeletingSomeoneElsesWorkout_ReturnsNotFound()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var other = await TestUsers.CreateAsync(_factory);
        var workout = NewWorkout("Leg day");
        (await owner.Client.PostAsJsonAsync("api/workouts", workout)).EnsureSuccessStatusCode();
        var id = (await owner.Client.GetFromJsonAsync<Workout[]>("api/workouts"))!.Single().Id;

        var edit = await other.Client.PutAsJsonAsync($"api/workouts/{id}", NewWorkout("Hacked"));
        var delete = await other.Client.DeleteAsync($"api/workouts/{id}");

        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var stillThere = (await owner.Client.GetFromJsonAsync<Workout[]>("api/workouts"))!.Single();
        Assert.Equal("Leg day", stillThere.Description);
    }

    [Fact]
    public async Task CreatingAWorkout_Returns201WithItsLocation()
    {
        var owner = await TestUsers.CreateAsync(_factory);

        var response = await owner.Client.PostAsJsonAsync("api/workouts", NewWorkout("Legs"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Workout>();
        Assert.Equal("Legs", created!.Description);
        Assert.Equal($"/api/workouts/{created.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task AWorkoutById_IsVisibleToFriendsButNotStrangers()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var friend = await TestUsers.CreateAsync(_factory);
        var stranger = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(owner, friend);
        var created = await (await owner.Client.PostAsJsonAsync("api/workouts", NewWorkout("Legs")))
            .Content.ReadFromJsonAsync<Workout>();
        var url = $"api/workouts/{created!.Id}";

        Assert.Equal(HttpStatusCode.OK, (await owner.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await friend.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task OwnWorkout_CanBeEditedAndDeleted()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        (await owner.Client.PostAsJsonAsync("api/workouts", NewWorkout("Push"))).EnsureSuccessStatusCode();
        var id = (await owner.Client.GetFromJsonAsync<Workout[]>("api/workouts"))!.Single().Id;

        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.PutAsJsonAsync($"api/workouts/{id}", NewWorkout("Pull"))).StatusCode);
        Assert.Equal("Pull", (await owner.Client.GetFromJsonAsync<Workout[]>("api/workouts"))!.Single().Description);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"api/workouts/{id}")).StatusCode);
        Assert.Empty((await owner.Client.GetFromJsonAsync<Workout[]>("api/workouts"))!);
    }

    [Fact]
    public async Task EditingOrDeletingSomeoneElsesLoggedFood_ReturnsNotFound()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var other = await TestUsers.CreateAsync(_factory);
        var logged = await LogFoodAsync(owner, grams: 100);

        var edit = await other.Client.PatchAsJsonAsync($"api/logged-foods/{logged.Id}", new { grams = 1 });
        var delete = await other.Client.DeleteAsync($"api/logged-foods/{logged.Id}");

        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var stillThere = (await owner.Client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?date={Today}"))!.Single();
        Assert.Equal(100, stillThere.Grams);
    }

    [Fact]
    public async Task OwnLoggedFood_CanBeEditedAndDeleted()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var logged = await LogFoodAsync(owner, grams: 100);

        var edit = await owner.Client.PatchAsJsonAsync($"api/logged-foods/{logged.Id}", new { grams = 250 });

        Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
        Assert.Equal(250, (await owner.Client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?date={Today}"))!.Single().Grams);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"api/logged-foods/{logged.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("workouts")]
    [InlineData("logged-foods")]
    public async Task SomeoneElsesActivity_IsOnlyVisibleToFriends(string resource)
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var friend = await TestUsers.CreateAsync(_factory);
        var stranger = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(owner, friend);
        var url = $"api/users/{owner.Id}/{resource}?from={Today}&to={Today}";

        Assert.Equal(HttpStatusCode.OK, (await owner.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await friend.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task SomeoneElsesFriendsList_IsOnlyVisibleToFriends()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var friend = await TestUsers.CreateAsync(_factory);
        var stranger = await TestUsers.CreateAsync(_factory);
        await TestUsers.MakeFriendsAsync(owner, friend);
        var url = $"api/users/{owner.Id}/friends";

        Assert.Equal(HttpStatusCode.OK, (await owner.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await friend.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task APendingFriendRequest_DoesNotGrantAccessToActivity()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        var requester = await TestUsers.CreateAsync(_factory);
        (await requester.Client.PostAsJsonAsync("api/friends/requests", new { receiverId = owner.Id })).EnsureSuccessStatusCode();

        var response = await requester.Client.GetAsync($"api/users/{owner.Id}/workouts?from={Today}&to={Today}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LoggedFoods_DoNotExposeTheUser()
    {
        var owner = await TestUsers.CreateAsync(_factory);
        await LogFoodAsync(owner, grams: 100);

        var json = await owner.Client.GetStringAsync($"api/logged-foods?date={Today}");

        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(owner.Username, json);
    }

    [Fact]
    public async Task LoggingAnUnknownFood_ReturnsNotFound()
    {
        var user = await TestUsers.CreateAsync(_factory);
        var unknown = new { id = Guid.NewGuid(), name = "Ghost", calories = 1, protein = 1, fats = 1, carbs = 1 };

        var response = await user.Client.PostAsJsonAsync($"api/logged-foods?date={Today}",
            new[] { new { food = unknown, grams = 100 } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LoggingSeveralFoods_SavesAllOfThem_EvenTheSameFoodTwice()
    {
        var user = await TestUsers.CreateAsync(_factory);
        var rice = await CreateFoodAsync(user);
        var oats = await CreateFoodAsync(user);

        var response = await user.Client.PostAsJsonAsync($"api/logged-foods?date={Today}", new[]
        {
            new { food = rice, grams = 100 }, new { food = oats, grams = 50 }, new { food = rice, grams = 25 }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var logged = await user.Client.GetFromJsonAsync<LoggedFoodDto[]>($"api/logged-foods?date={Today}");
        Assert.Equal([25, 50, 100], logged!.Select(f => f.Grams).Order());
        Assert.Equal(2, logged!.Count(f => f.Food.Id == rice.Id));
    }

    [Fact]
    public async Task CreatingAFoodWithNegativeMacros_ReturnsBadRequest()
    {
        var user = await TestUsers.CreateAsync(_factory);

        var response = await user.Client.PostAsJsonAsync("api/foods",
            new { name = "Bad", calories = -1, protein = 0, fats = 0, carbs = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

    private static WorkoutDto NewWorkout(string description) => new()
    {
        Description = description,
        Duration = 60,
        MusclesTargeted = ["Legs"],
        Date = DateTime.UtcNow
    };

    private static async Task<string> UploadImageAsync(TestUser user)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([1, 2, 3]), "File", "avatar.png");
        var response = await user.Client.PostAsync("api/images", content);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return body!["fileKey"];
    }

    private static async Task<Food> CreateFoodAsync(TestUser user)
    {
        var create = await user.Client.PostAsJsonAsync("api/foods",
            new { name = $"Rice {Guid.NewGuid():N}", calories = 130, protein = 2.7, fats = 0.3, carbs = 28 });
        create.EnsureSuccessStatusCode();
        return (await create.Content.ReadFromJsonAsync<Food>())!;
    }

    private static async Task<LoggedFoodDto> LogFoodAsync(TestUser user, double grams)
    {
        var food = await CreateFoodAsync(user);

        var log = await user.Client.PostAsJsonAsync($"api/logged-foods?date={Today}",
            new[] { new { food, grams } });
        log.EnsureSuccessStatusCode();
        return (await log.Content.ReadFromJsonAsync<LoggedFoodDto[]>())!.Single();
    }
}

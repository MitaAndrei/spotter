namespace gymappAPI.Services;

public static class CacheKeys
{
    public const string FoodCatalog = "foods:all";

    public static string WorkoutTemplates(Guid userId) => $"workouts:templates:{userId}";

    public static string UserProfile(string username) => $"users:profile:{username.ToLowerInvariant()}";
}

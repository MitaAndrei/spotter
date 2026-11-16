using gymappAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Data;

/// <summary>
/// Fills an empty database with a "demo" account for the live demo: a year of workouts, recently
/// logged meals, and a friend with a short chat. Runs at startup when Demo:Password is set, and
/// does nothing if the demo user already exists.
/// </summary>
public static class DemoSeeder
{
    public const string DemoUsername = "demo";
    private const string FriendUsername = "alex";

    private static readonly (string Name, double Calories, double Protein, double Fats, double Carbs)[] Foods =
    [
        ("Chicken breast", 165, 31, 3.6, 0),
        ("White rice (cooked)", 130, 2.7, 0.3, 28),
        ("Oats", 389, 17, 7, 66),
        ("Egg", 155, 13, 11, 1.1),
        ("Greek yogurt", 97, 9, 5, 3.9),
        ("Banana", 89, 1.1, 0.3, 23),
        ("Salmon", 208, 20, 13, 0),
        ("Broccoli", 34, 2.8, 0.4, 7),
        ("Whole wheat bread", 247, 13, 3.4, 41),
        ("Peanut butter", 588, 25, 50, 20),
        ("Whey protein", 400, 80, 6, 8),
        ("Olive oil", 884, 0, 100, 0),
    ];

    // Push / pull / legs, each with the muscles it targets.
    private static readonly (string Description, string[] Muscles)[] Splits =
    [
        ("Push day: bench press, overhead press, dips", ["Chest", "Arms"]),
        ("Pull day: deadlifts, rows, pull-ups, curls", ["Back", "Arms"]),
        ("Leg day: squats, lunges, leg press", ["Legs"]),
    ];

    private static readonly Dictionary<string, string> MuscleColors = new()
    {
        ["Chest"] = LabelColor.Blue,
        ["Arms"] = LabelColor.Green,
        ["Legs"] = LabelColor.Red,
        ["Back"] = LabelColor.Orange,
    };

    public static async Task SeedAsync(AppDbContext db, string password)
    {
        if (await db.Users.AnyAsync(u => u.Username == DemoUsername)) return;

        // Fixed seed, so every fresh deployment gets the same data.
        var random = new Random(42);
        var today = DateTime.UtcNow.Date;
        var hasher = new PasswordHasher<User>();

        var demo = new User { Id = Guid.NewGuid(), Username = DemoUsername };
        demo.PasswordHash = hasher.HashPassword(demo, password);
        // Nobody logs in as the friend, so its password is random and never shown.
        var friend = new User { Id = Guid.NewGuid(), Username = FriendUsername };
        friend.PasswordHash = hasher.HashPassword(friend, Guid.NewGuid().ToString());
        db.Users.AddRange(demo, friend);

        db.Workouts.AddRange(CreateWorkouts(demo.Id, today, random, sessionsPerWeek: 4, markTemplates: true));
        db.Workouts.AddRange(CreateWorkouts(friend.Id, today, random, sessionsPerWeek: 3, markTemplates: false));

        var foods = await db.Foods.ToListAsync();
        if (foods.Count == 0)
        {
            foods = Foods.Select(f => new Food
            {
                Id = Guid.NewGuid(), Name = f.Name, Calories = f.Calories,
                Protein = f.Protein, Fats = f.Fats, Carbs = f.Carbs
            }).ToList();
            db.Foods.AddRange(foods);
        }
        db.LoggedFoods.AddRange(CreateMeals(demo, foods, today, random));

        db.FriendRequests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(), SenderId = friend.Id, ReceiverId = demo.Id,
            Accepted = true, CreatedAt = today.AddDays(-60)
        });
        db.ChatMessages.AddRange(CreateChat(demo.Id, friend.Id, DateTime.UtcNow));

        await db.SaveChangesAsync();
    }

    private static IEnumerable<Workout> CreateWorkouts(Guid userId, DateTime today, Random random,
        int sessionsPerWeek, bool markTemplates)
    {
        var workouts = new List<Workout>();
        var split = 0;
        for (var day = today.AddDays(-364); day <= today; day = day.AddDays(1))
        {
            if (random.Next(7) >= sessionsPerWeek) continue;

            var (description, muscles) = Splits[split++ % Splits.Length];
            workouts.Add(new Workout
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = description,
                Duration = 45 + random.Next(7) * 5,
                Date = day,
                Labels = muscles.Select(m => new Label { Id = Guid.NewGuid(), Muscle = m, Color = MuscleColors[m] })
                    .ToList()
            });
        }

        // The latest workout of each kind is saved as a template.
        if (markTemplates)
        {
            foreach (var (description, _) in Splits)
                workouts.Last(w => w.Description == description).IsTemplate = true;
        }

        return workouts;
    }

    private static IEnumerable<LoggedFood> CreateMeals(User user, List<Food> foods, DateTime today, Random random)
    {
        for (var day = today.AddDays(-13); day <= today; day = day.AddDays(1))
        {
            foreach (var food in foods.OrderBy(_ => random.Next()).Take(4 + random.Next(3)))
            {
                yield return new LoggedFood
                {
                    Id = Guid.NewGuid(), User = user, Food = food, Date = day,
                    Grams = 50 + random.Next(6) * 25
                };
            }
        }
    }

    private static IEnumerable<ChatMessage> CreateChat(Guid demoId, Guid friendId, DateTime now)
    {
        (bool FromFriend, string Content, int MinutesAgo)[] lines =
        [
            (true, "Leg day tomorrow?", 180),
            (false, "Yes! 7am at the gym?", 175),
            (true, "Deal. Trying for a new squat PR", 170),
            (false, "Nice, I'll spot you", 160),
        ];

        return lines.Select(l => new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = l.FromFriend ? friendId : demoId,
            ReceiverId = l.FromFriend ? demoId : friendId,
            Content = l.Content,
            Timestamp = now.AddMinutes(-l.MinutesAgo)
        });
    }
}

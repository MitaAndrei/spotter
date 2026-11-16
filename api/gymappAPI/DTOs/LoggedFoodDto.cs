using gymappAPI.Models;

namespace gymappAPI.DTOs;

public class LoggedFoodDto
{
    public Guid Id { get; set; }
    public Food Food { get; set; } = null!;
    public DateTime Date { get; set; }
    public double Grams { get; set; }

    public static LoggedFoodDto From(LoggedFood loggedFood) => new()
    {
        Id = loggedFood.Id,
        Food = loggedFood.Food,
        Date = loggedFood.Date,
        Grams = loggedFood.Grams
    };
}

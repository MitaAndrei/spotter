using System.ComponentModel.DataAnnotations;

namespace gymappAPI.Models;

public class Food
{
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Calories { get; set; }

    [Range(0, double.MaxValue)]
    public double Protein { get; set; }

    [Range(0, double.MaxValue)]
    public double Fats { get; set; }

    [Range(0, double.MaxValue)]
    public double Carbs { get; set; }
}
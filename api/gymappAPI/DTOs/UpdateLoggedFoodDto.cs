using System.ComponentModel.DataAnnotations;

namespace gymappAPI.DTOs;

public class UpdateLoggedFoodDto
{
    [Range(0.01, double.MaxValue)]
    public double Grams { get; set; }
}

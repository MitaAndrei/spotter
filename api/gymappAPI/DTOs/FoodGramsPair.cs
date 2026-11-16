using System.ComponentModel.DataAnnotations;
using gymappAPI.Models;

namespace gymappAPI.DTOs;

public class FoodGramsPair
{
    public Food Food { get; set; } = null!;
    [Range(0.01, double.MaxValue)]
    public double Grams { get; set; }
}
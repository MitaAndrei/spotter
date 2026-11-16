using System.ComponentModel.DataAnnotations;
using gymappAPI.Models;

namespace gymappAPI.DTOs;

public class WorkoutDto
{
    public string Description { get; set; } = string.Empty;
    
    [Range(1, 1440)]
    public int Duration { get; set; }

    public string[] MusclesTargeted { get; set; } = [];

    public bool IsTemplate { get; set; } = false;
    
    public DateTime Date { get; set; }
}
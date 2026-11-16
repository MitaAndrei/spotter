namespace gymappAPI.Models;

public class Workout
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public bool IsTemplate { get; set; }
    public int Duration { get; set; }

    public ICollection<Label> Labels { get; set; } = [];
    
    public DateTime Date { get; set; }
}
using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Models;
using gymappAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace gymappAPI.Managers;

public class WorkoutManager : IWorkoutManager
{
    private readonly AppDbContext _context;
    private readonly IUserManager _userManager;
    private readonly ICacheService _cache;

    private static readonly TimeSpan TemplatesTtl = TimeSpan.FromMinutes(30);
    
    public WorkoutManager(AppDbContext context, IUserManager userManager, ICacheService cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task CreateAsync(Models.Workout workout)
    {

        foreach (var label in workout.Labels)
        {
            _context.Labels.Add(label);
        }
        
        _context.Workouts.Add(workout);
        
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.WorkoutTemplates(workout.UserId));
    }
    
    public async Task<Guid> CreateFromDtoAsync(WorkoutDto workoutDto)
    {
        ICollection<Label> labels = CreateLabels(workoutDto.MusclesTargeted);
        var user = await _userManager.GetCurrentUserAsync();
        Guid userId = user.Id;
        
        var workout = new Models.Workout
        {
            Id = Guid.NewGuid(),
            Description = workoutDto.Description,
            Duration = workoutDto.Duration,
            Date = workoutDto.Date.Date,
            Labels = labels,
            UserId = userId,
            IsTemplate = workoutDto.IsTemplate
        };
        
        await CreateAsync(workout);
        return workout.Id;
    }

    private ICollection<Label> CreateLabels(string[] musclesTargeted)
    {
        
        ICollection<Label> labels = new List<Label>();
        
        foreach (var muscle in musclesTargeted)
        {
            string labelColor = muscle switch
            {
                "Chest" => LabelColor.Blue,
                "Arms" => LabelColor.Green,
                "Legs" => LabelColor.Red,
                "Back" => LabelColor.Orange,
                _ => string.Empty
            };
            
            labels.Add(new Label
            {
                Id = Guid.NewGuid(),
                Color = labelColor,
                Muscle = muscle
            });
        }
        
        return labels;
    }

    public async Task<Models.Workout[]> GetAllForUserAsync()
    {
        var user = await _userManager.GetCurrentUserAsync();
        var userId = user.Id;
        return await _context.Workouts.Include(x => x.Labels)
            .Where(w => w.UserId == userId).ToArrayAsync();
    }
    
    public async Task<Models.Workout[]> GetTemplatesAsync()
    {
        var user = await _userManager.GetCurrentUserAsync();
        var userId = user.Id;
        return await _cache.GetOrSetAsync(CacheKeys.WorkoutTemplates(userId),
            () => _context.Workouts.AsNoTracking().Include(x => x.Labels)
                .Where(w => w.UserId == userId && w.IsTemplate == true).ToArrayAsync(),
            TemplatesTtl);
    }
    
    public async Task<Models.Workout[]> GetAllInDateRangeAsync(Guid userId, DateTime start, DateTime end)
    {
        // start and end are calendar days, and end is included.
        var from = start.Date;
        var toExclusive = end.Date.AddDays(1);
        return await _context.Workouts.Include(x => x.Labels)
            .Where(w =>
                w.UserId == userId &&
                w.Date >= from &&
                w.Date < toExclusive).ToArrayAsync();
    }

    public async Task<Models.Workout?> GetWorkoutByIdAsync(Guid id)
    {
        return await _context.Workouts.Include(w => w.Labels).FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _userManager.GetCurrentUserAsync();
        var workout = await _context.Workouts
            .Include(w => w.Labels)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);

        if (workout == null)
            return false;
        
        _context.Workouts.Remove(workout);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.WorkoutTemplates(workout.UserId));
        return true;
    }

    public async Task<bool> EditAsync(Guid id, WorkoutDto workoutDTO)
    {
        var user = await _userManager.GetCurrentUserAsync();
        var existingWorkout = await _context.Workouts
            .Include(w => w.Labels) 
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);
        
        if (existingWorkout == null)
            return false;
        
        existingWorkout.Description = workoutDTO.Description;
        existingWorkout.Duration = workoutDTO.Duration;
        existingWorkout.Date = workoutDTO.Date.Date;
        existingWorkout.IsTemplate = workoutDTO.IsTemplate;
        
        var created = CreateLabels(workoutDTO.MusclesTargeted);
        
        _context.Labels.RemoveRange(existingWorkout.Labels);
        
        existingWorkout.Labels = created;
        _context.Labels.AddRange(created);
        
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.WorkoutTemplates(existingWorkout.UserId));
        return true;
    }
}
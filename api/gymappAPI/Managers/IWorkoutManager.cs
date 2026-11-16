using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Managers;

public interface IWorkoutManager
{
    Task CreateAsync(Models.Workout workout);
    
    Task<Models.Workout[]> GetAllForUserAsync();
    
    Task<Models.Workout[]> GetTemplatesAsync();

    Task<Models.Workout[]> GetAllInDateRangeAsync(Guid userId, DateTime start, DateTime end);
    
    Task<Models.Workout?> GetWorkoutByIdAsync(Guid id);

    Task<bool> DeleteAsync(Guid id);
    
    Task<bool> EditAsync(Guid id, WorkoutDto workoutDto);
    
    Task<Guid> CreateFromDtoAsync(WorkoutDto workoutDto);
}
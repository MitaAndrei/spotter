using gymappAPI.DTOs;
using gymappAPI.Managers;
using gymappAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/workouts")]
public class WorkoutsController : ControllerBase
{
    private readonly IWorkoutManager _workoutManager;
    private readonly IUserManager _userManager;
    private readonly IFriendsManager _friendsManager;

    public WorkoutsController(IWorkoutManager workoutManager, IUserManager userManager, IFriendsManager friendsManager)
    {
        _workoutManager = workoutManager;
        _userManager = userManager;
        _friendsManager = friendsManager;
    }

    // GET api/workouts?from=...&to=...  (all of yours when the range is omitted)
    [HttpGet]
    public async Task<Workout[]> GetMine([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (from is null || to is null)
            return await _workoutManager.GetAllForUserAsync();

        var user = await _userManager.GetCurrentUserAsync();
        return await _workoutManager.GetAllInDateRangeAsync(user.Id, from.Value, to.Value);
    }

    [HttpGet("templates")]
    public async Task<Workout[]> GetTemplates()
    {
        return await _workoutManager.GetTemplatesAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Workout>> GetById(Guid id)
    {
        var workout = await _workoutManager.GetWorkoutByIdAsync(id);

        // Someone else's workout looks the same as a missing one.
        if (workout is null || !await _friendsManager.CanViewActivityOfAsync(workout.UserId))
            return NotFound();

        return workout;
    }

    [HttpPost]
    public async Task<ActionResult<Workout>> Create([FromBody] WorkoutDto workoutDto)
    {
        var id = await _workoutManager.CreateFromDtoAsync(workoutDto);
        var workout = await _workoutManager.GetWorkoutByIdAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, workout);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WorkoutDto workoutDto)
    {
        return await _workoutManager.EditAsync(id, workoutDto) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await _workoutManager.DeleteAsync(id) ? NoContent() : NotFound();
    }
}

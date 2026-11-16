using gymappAPI.DTOs;
using gymappAPI.Managers;
using gymappAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly IWorkoutManager _workoutManager;
    private readonly IFoodManager _foodManager;
    private readonly IFriendsManager _friendsManager;

    public UsersController(IUserManager userManager, IWorkoutManager workoutManager, IFoodManager foodManager,
        IFriendsManager friendsManager)
    {
        _userManager = userManager;
        _workoutManager = workoutManager;
        _foodManager = foodManager;
        _friendsManager = friendsManager;
    }

    [HttpGet("me")]
    public async Task<UserProfileDto> GetMe()
    {
        var user = await _userManager.GetCurrentUserAsync();
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            ProfileImageKey = user.ProfileImageKey,
            CreatedAt = user.CreatedAt
        };
    }

    // GET api/users?search=...
    [HttpGet]
    public async Task<ActionResult<UserDto[]>> Search([FromQuery] string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return BadRequest(new { message = "Pass a search term." });

        return await _userManager.SearchUsersAsync(search);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserProfileDto>> GetByUsername(string username)
    {
        var user = await _userManager.GetProfileByUsernameAsync(username);
        return user is null ? NotFound() : user;
    }

    // A user's friends, workouts and food diary are visible to them and their friends.

    [HttpGet("{userId:guid}/friends")]
    public async Task<ActionResult<UserDto[]>> GetFriends(Guid userId)
    {
        if (!await _friendsManager.CanViewActivityOfAsync(userId)) return Forbid();
        return await _friendsManager.GetFriendsAsync(userId);
    }

    [HttpGet("{userId:guid}/workouts")]
    public async Task<ActionResult<Workout[]>> GetWorkouts(Guid userId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!await _friendsManager.CanViewActivityOfAsync(userId)) return Forbid();
        return await _workoutManager.GetAllInDateRangeAsync(userId, from, to);
    }

    [HttpGet("{userId:guid}/logged-foods")]
    public async Task<ActionResult<LoggedFoodDto[]>> GetLoggedFoods(Guid userId, [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (!await _friendsManager.CanViewActivityOfAsync(userId)) return Forbid();
        var loggedFoods = await _foodManager.GetAllInDateRangeAsync(userId, from, to);
        return loggedFoods.Select(LoggedFoodDto.From).ToArray();
    }
}

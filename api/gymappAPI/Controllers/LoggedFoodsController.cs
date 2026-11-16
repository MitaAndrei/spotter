using gymappAPI.DTOs;
using gymappAPI.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

// The current user's food diary.
[ApiController]
[Authorize]
[Route("api/logged-foods")]
public class LoggedFoodsController : ControllerBase
{
    private readonly IFoodManager _foodManager;
    private readonly IUserManager _userManager;

    public LoggedFoodsController(IFoodManager foodManager, IUserManager userManager)
    {
        _foodManager = foodManager;
        _userManager = userManager;
    }

    // GET api/logged-foods?date=...  or  ?from=...&to=...
    [HttpGet]
    public async Task<ActionResult<LoggedFoodDto[]>> Get([FromQuery] DateTime? date, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (date is not null)
            return (await _foodManager.GetAllForDateAsync(date.Value)).Select(LoggedFoodDto.From).ToArray();

        if (from is null || to is null)
            return BadRequest(new { message = "Pass either date, or from and to." });

        var user = await _userManager.GetCurrentUserAsync();
        var loggedFoods = await _foodManager.GetAllInDateRangeAsync(user.Id, from.Value, to.Value);
        return loggedFoods.Select(LoggedFoodDto.From).ToArray();
    }

    // POST api/logged-foods?date=...  with the foods and grams eaten that day
    [HttpPost]
    public async Task<ActionResult<LoggedFoodDto[]>> Create([FromQuery] DateTime date, [FromBody] FoodGramsPair[] foods)
    {
        var loggedFoods = await _foodManager.ConvertToLoggedFoodsAsync(foods, date);
        await _foodManager.CreateLoggedFoodsAsync(loggedFoods);
        return StatusCode(StatusCodes.Status201Created, loggedFoods.Select(LoggedFoodDto.From).ToArray());
    }

    // Only the grams can be changed.
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateGrams(Guid id, [FromBody] UpdateLoggedFoodDto dto)
    {
        return await _foodManager.EditLoggedFoodGramsAsync(id, dto.Grams) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await _foodManager.DeleteLoggedFoodAsync(id) ? NoContent() : NotFound();
    }
}

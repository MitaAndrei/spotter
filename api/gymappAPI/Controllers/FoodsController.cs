using gymappAPI.Managers;
using gymappAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

// The shared food catalog. What a user ate is in LoggedFoodsController.
[ApiController]
[Authorize]
[Route("api/foods")]
public class FoodsController : ControllerBase
{
    private readonly IFoodManager _foodManager;

    public FoodsController(IFoodManager foodManager)
    {
        _foodManager = foodManager;
    }

    [HttpGet]
    public async Task<Food[]> GetAll()
    {
        return await _foodManager.GetAllFoodAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Food>> GetById(Guid id)
    {
        var food = await _foodManager.GetFoodByIdAsync(id);
        return food is null ? NotFound() : food;
    }

    [HttpPost]
    public async Task<ActionResult<Food>> Create([FromBody] Food food)
    {
        food.Id = Guid.NewGuid();
        await _foodManager.CreateAsync(food);
        return CreatedAtAction(nameof(GetById), new { id = food.Id }, food);
    }
}

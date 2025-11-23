using Microsoft.AspNetCore.Mvc;
using SFA_WebAPI.Models;
using SFA_WebAPI.Services;

namespace SFA_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StartPointsController : ControllerBase
{
	private readonly IStartPointRepository _repo;
	private readonly IConfiguration _config;
    private readonly ILogger<StartPointsController> _logger;

	public StartPointsController(IStartPointRepository repo, IConfiguration config, ILogger<StartPointsController> logger)
	{
		_repo = repo;
		_config = config;
        _logger = logger;
	}

	[HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
	public async Task<IActionResult> GetAll()
	{
        _logger.LogInformation("GetAll called");
		var list = await _repo.GetAllAsync();
        _logger.LogInformation($"GetAll returning {list.Count} items");
		return Ok(list);
	}

	[HttpGet("{id}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
	public async Task<IActionResult> GetById(string id)
	{
        _logger.LogInformation($"GetById called for {id}");
		var sp = await _repo.GetByIdAsync(id);
		if (sp == null) return NotFound();
		return Ok(sp);
	}

	private bool IsAuthorized()
	{
		var key = _config["AdminApiKey"];
		if (string.IsNullOrEmpty(key)) 
        {
            _logger.LogWarning("AdminApiKey not configured");
            return false;
        }
		if (!Request.Headers.TryGetValue("X-API-KEY", out var provided)) 
        {
            _logger.LogWarning("X-API-KEY header missing");
            return false;
        }
        if (provided != key)
        {
            _logger.LogWarning("Invalid API Key provided");
            return false;
        }
		return true;
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] StartPoint sp)
	{
        _logger.LogInformation("Create called");
		if (!IsAuthorized()) return Unauthorized();
		await _repo.CreateAsync(sp);
		return CreatedAtAction(nameof(GetById), new { id = sp.Id }, sp);
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> Update(string id, [FromBody] StartPoint sp)
	{
        _logger.LogInformation($"Update called for {id}");
		if (!IsAuthorized()) return Unauthorized();
		try
		{
			await _repo.UpdateAsync(id, sp);
			return NoContent();
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(string id)
	{
        _logger.LogInformation($"Delete called for {id}");
		if (!IsAuthorized()) return Unauthorized();
		try
		{
			await _repo.DeleteAsync(id);
			return NoContent();
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}
	}
}


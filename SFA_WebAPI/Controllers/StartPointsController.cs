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

	public StartPointsController(IStartPointRepository repo, IConfiguration config)
	{
		_repo = repo;
		_config = config;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		var list = await _repo.GetAllAsync();
		return Ok(list);
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetById(string id)
	{
		var sp = await _repo.GetByIdAsync(id);
		if (sp == null) return NotFound();
		return Ok(sp);
	}

	private bool IsAuthorized()
	{
		var key = _config["AdminApiKey"];
		if (string.IsNullOrEmpty(key)) return false;
		if (!Request.Headers.TryGetValue("X-API-KEY", out var provided)) return false;
		return provided == key;
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] StartPoint sp)
	{
		if (!IsAuthorized()) return Unauthorized();
		await _repo.CreateAsync(sp);
		return CreatedAtAction(nameof(GetById), new { id = sp.Id }, sp);
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> Update(string id, [FromBody] StartPoint sp)
	{
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


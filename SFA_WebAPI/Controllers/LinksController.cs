using Microsoft.AspNetCore.Mvc;
using SFA_WebAPI.Models;
using SFA_WebAPI.Services;

namespace SFA_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LinksController : ControllerBase
{
    private readonly ILinksCatalogService _linksCatalogService;
    private readonly IConfiguration _config;
    private readonly ILogger<LinksController> _logger;

    public LinksController(ILinksCatalogService linksCatalogService, IConfiguration config, ILogger<LinksController> logger)
    {
        _linksCatalogService = linksCatalogService;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var snapshot = await _linksCatalogService.GetSnapshotAsync(cancellationToken);
        Response.Headers.ETag = snapshot.ETag;
        Response.Headers.LastModified = snapshot.LastModifiedUtc.ToUniversalTime().ToString("R");
        Response.Headers.CacheControl = "public,max-age=300";

        if (Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatchHeaderValue) &&
            ifNoneMatchHeaderValue.Any(value =>
                !string.IsNullOrWhiteSpace(value) &&
                string.Equals(value.Trim(), snapshot.ETag, StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(snapshot.Links);
    }

    [HttpGet("validate-admin")]
    public IActionResult ValidateAdmin()
    {
        if (!IsAuthorized()) return Unauthorized();
        return Ok(new { valid = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LinkItem link, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return Unauthorized();
        var created = await _linksCatalogService.CreateAsync(link, cancellationToken);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpPut("{index:int}")]
    public async Task<IActionResult> Update(int index, [FromBody] LinkItem link, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return Unauthorized();
        try
        {
            await _linksCatalogService.UpdateAsync(index, link, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{index:int}")]
    public async Task<IActionResult> Delete(int index, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return Unauthorized();
        try
        {
            await _linksCatalogService.DeleteAsync(index, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private bool IsAuthorized()
    {
        var key = _config["AdminApiKey"];
        if (string.IsNullOrWhiteSpace(key))
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
            _logger.LogWarning("Invalid API key for links admin endpoint");
            return false;
        }

        return true;
    }
}

using Microsoft.AspNetCore.Mvc;
using SFA_WebAPI.Services;

namespace SFA_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LinksController : ControllerBase
{
    private readonly ILinksCatalogService _linksCatalogService;

    public LinksController(ILinksCatalogService linksCatalogService)
    {
        _linksCatalogService = linksCatalogService;
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
}

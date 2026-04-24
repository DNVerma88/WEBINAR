using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;
    private readonly IPostBookmarkService _bookmarkService;

    public FeedController(IFeedService feedService, IPostBookmarkService bookmarkService)
    {
        _feedService     = feedService;
        _bookmarkService = bookmarkService;
    }

    /// <summary>Returns a personalized feed based on followed communities, tags, and authors.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonalizedFeed([FromQuery] FeedRequest request, CancellationToken ct)
    {
        var result = await _feedService.GetPersonalizedFeedAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Returns all published posts sorted by newest first.</summary>
    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatest([FromQuery] FeedRequest request, CancellationToken ct)
    {
        var result = await _feedService.GetLatestAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Returns trending posts scored by reactions, comments, and recency.</summary>
    [HttpGet("trending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrending([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _feedService.GetTrendingAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Returns posts bookmarked by the current user.</summary>
    [HttpGet("bookmarks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookmarks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _bookmarkService.GetBookmarksAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }
}

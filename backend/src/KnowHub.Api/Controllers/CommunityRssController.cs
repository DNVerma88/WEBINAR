using KnowHub.Domain.Enums;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Xml;

namespace KnowHub.Api.Controllers;

/// <summary>Serves an RSS 2.0 feed of the latest published posts in a community.</summary>
[ApiController]
[Route("api/communities/{communityId:guid}/rss")]
public class CommunityRssController : ControllerBase
{
    private readonly KnowHubDbContext _db;

    public CommunityRssController(KnowHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces("application/rss+xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRss(Guid communityId, CancellationToken ct)
    {
        var community = await _db.Communities
            .Where(c => c.Id == communityId)
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.TenantId })
            .FirstOrDefaultAsync(ct);

        if (community is null) return NotFound();

        var posts = await _db.CommunityPosts
            .Where(p => p.CommunityId == communityId
                        && p.TenantId == community.TenantId
                        && p.Status == PostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Take(50)
            .Include(p => p.Author)
            .AsNoTracking()
            .Select(p => new
            {
                p.Title,
                p.Slug,
                p.ContentHtml,
                p.PublishedAt,
                AuthorName = p.Author!.FullName
            })
            .ToListAsync(ct);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
        {
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");

            writer.WriteStartElement("channel");
            writer.WriteElementString("title", community.Name);
            writer.WriteElementString("link", $"{baseUrl}/communities/{communityId}");
            writer.WriteElementString("description", $"Latest posts from {community.Name}");
            writer.WriteElementString("language", "en-us");

            foreach (var post in posts)
            {
                writer.WriteStartElement("item");
                writer.WriteElementString("title", post.Title);
                writer.WriteElementString("link", $"{baseUrl}/communities/{communityId}/posts/{post.Slug}");
                writer.WriteStartElement("description");
                writer.WriteCData(post.ContentHtml);
                writer.WriteEndElement();
                writer.WriteElementString("author", post.AuthorName);
                if (post.PublishedAt.HasValue)
                    writer.WriteElementString("pubDate", post.PublishedAt.Value.ToString("R"));
                writer.WriteEndElement(); // item
            }

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
        }

        return Content(sb.ToString(), "application/rss+xml", Encoding.UTF8);
    }
}

using KnowHub.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowHub.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ISessionChapterService _chapterService;
    private readonly ISessionQuizService _quizService;
    private readonly IAfterActionReviewService _aarService;
    private readonly ISkillEndorsementService _endorsementService;
    private readonly ICommentService _commentService;

    public SessionsController(
        ISessionService sessionService,
        ISessionChapterService chapterService,
        ISessionQuizService quizService,
        IAfterActionReviewService aarService,
        ISkillEndorsementService endorsementService,
        ICommentService commentService)
    {
        _sessionService = sessionService;
        _chapterService = chapterService;
        _quizService = quizService;
        _aarService = aarService;
        _endorsementService = endorsementService;
        _commentService = commentService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] GetSessionsRequest request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetSessionsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    // B13: session creation/content restricted to KnowledgeTeam or above
    // B13: session creation restricted to KnowledgeTeam or above
    [HttpPost]
    [Authorize(Policy = "KnowledgeTeamOrAbove")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.CompleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/register")]
    [ProducesResponseType(typeof(SessionRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Register(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.RegisterAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRegistration(Guid id, CancellationToken cancellationToken)
    {
        await _sessionService.CancelRegistrationAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/materials")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMaterials(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetMaterialsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/materials")]
    [ProducesResponseType(typeof(SessionMaterialDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMaterial(Guid id, [FromBody] AddSessionMaterialRequest request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.AddMaterialAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetMaterials), new { id }, result);
    }

    [HttpGet("{id:guid}/ratings")]
    [ProducesResponseType(typeof(SessionRatingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRatings(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetRatingsSummaryAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/ratings")]
    [ProducesResponseType(typeof(SessionRatingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitRating(Guid id, [FromBody] SubmitSessionRatingRequest request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.SubmitRatingAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetRatings), new { id }, result);
    }

    // ---- Chapters ----

    [HttpGet("{id:guid}/chapters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChapters(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _chapterService.GetChaptersAsync(id, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/chapters")]
    [ProducesResponseType(typeof(SessionChapterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddChapter(Guid id, [FromBody] AddChapterRequest request, CancellationToken cancellationToken)
    {
        var result = await _chapterService.AddChapterAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetChapters), new { id }, result);
    }

    [HttpDelete("{id:guid}/chapters/{chapterId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChapter(Guid id, Guid chapterId, CancellationToken cancellationToken)
    {
        await _chapterService.DeleteChapterAsync(chapterId, cancellationToken);
        return NoContent();
    }

    // ---- Quiz ----

    [HttpGet("{id:guid}/quiz")]
    [ProducesResponseType(typeof(SessionQuizDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuiz(Guid id, CancellationToken cancellationToken)
    {
        var result = await _quizService.GetQuizBySessionAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/quiz")]
    [ProducesResponseType(typeof(SessionQuizDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateQuiz(Guid id, [FromBody] CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var result = await _quizService.CreateQuizAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetQuiz), new { id }, result);
    }

    [HttpPut("{id:guid}/quiz")]
    [ProducesResponseType(typeof(SessionQuizDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuiz(Guid id, [FromBody] UpdateQuizRequest request, CancellationToken cancellationToken)
    {
        var result = await _quizService.UpdateQuizAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/quiz/attempts")]
    [ProducesResponseType(typeof(QuizAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitAttempt(Guid id, [FromBody] SubmitQuizAttemptRequest request, CancellationToken cancellationToken)
    {
        var result = await _quizService.SubmitAttemptAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/quiz/attempts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAttempts(Guid id, CancellationToken cancellationToken)
    {
        var result = await _quizService.GetMyAttemptsAsync(id, cancellationToken);
        return Ok(result);
    }

    // ---- After-Action Review ----

    [HttpGet("{id:guid}/aar")]
    [ProducesResponseType(typeof(AfterActionReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _aarService.GetBySessionAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/aar")]
    [ProducesResponseType(typeof(AfterActionReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAar(Guid id, [FromBody] CreateAarRequest request, CancellationToken cancellationToken)
    {
        var result = await _aarService.CreateAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetAar), new { id }, result);
    }

    [HttpPut("{id:guid}/aar")]
    [ProducesResponseType(typeof(AfterActionReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAar(Guid id, [FromBody] UpdateAarRequest request, CancellationToken cancellationToken)
    {
        var result = await _aarService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // ---- Endorsements ----

    [HttpPost("{id:guid}/endorsements")]
    [ProducesResponseType(typeof(SkillEndorsementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EndorseSkill(Guid id, [FromBody] EndorseSkillRequest request, CancellationToken cancellationToken)
    {
        var result = await _endorsementService.EndorseAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { id }, result);
    }

    // ---- Comments ----

    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComments(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _commentService.GetSessionCommentsAsync(id, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await _commentService.AddSessionCommentAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetComments), new { id }, result);
    }

    [HttpPost("{id:guid}/like")]
    [ProducesResponseType(typeof(LikeToggleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LikeSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _commentService.ToggleSessionLikeAsync(id, cancellationToken);
        return Ok(result);
    }
}

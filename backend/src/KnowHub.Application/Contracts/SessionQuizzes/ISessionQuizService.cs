namespace KnowHub.Application.Contracts;

public interface ISessionQuizService
{
    Task<SessionQuizDto> GetQuizBySessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<SessionQuizDto> CreateQuizAsync(Guid sessionId, CreateQuizRequest request, CancellationToken cancellationToken);
    Task<SessionQuizDto> UpdateQuizAsync(Guid sessionId, UpdateQuizRequest request, CancellationToken cancellationToken);
    Task<QuizAttemptResultDto> SubmitAttemptAsync(Guid sessionId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken);
    Task<List<UserQuizAttemptDto>> GetMyAttemptsAsync(Guid sessionId, CancellationToken cancellationToken);
}

namespace KnowHub.Application.Contracts.Talent;

public interface IResumeParserService
{
    /// <summary>
    /// Extracts text from a PDF or DOCX file and parses it into structured resume fields
    /// using AI (Gemini → OpenAI → Stub cascade).
    /// The stream is consumed in-memory; nothing is written to disk or the database.
    /// Returns <c>null</c> when the file type is invalid or text extraction fails.
    /// </summary>
    Task<ParsedResumeDto?> ParseAsync(Stream fileStream, string fileName, CancellationToken ct);
}

namespace KnowHub.Domain.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string error)
        : base($"Validation failed: {field} - {error}")
    {
        Errors = new Dictionary<string, string[]> { { field, new[] { error } } };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}

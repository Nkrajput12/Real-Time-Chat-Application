namespace ConnectHub.Authentication.Exceptions;

/// <summary>
/// Thrown when input validation fails in the authentication service.
/// Carries one or more human-readable error messages so the controller
/// can return a descriptive 400 Bad Request response.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>All validation error messages collected during validation.</summary>
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public ValidationException(string singleError)
        : base(singleError)
    {
        Errors = new List<string> { singleError }.AsReadOnly();
    }
}

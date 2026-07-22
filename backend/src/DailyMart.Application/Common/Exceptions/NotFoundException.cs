namespace DailyMart.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity doesn't exist (or is soft-deleted, which the query filter already
/// hides). Maps to 404 - the GlobalExceptionHandler mapping is added in this module's Step 8, same
/// pairing as every other Application exception type introduced so far.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}

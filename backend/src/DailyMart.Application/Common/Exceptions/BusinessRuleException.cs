namespace DailyMart.Application.Common.Exceptions;

/// <summary>
/// Thrown when a business rule is violated in a way FluentValidation can't express on its own (e.g. an
/// uploaded file's type/size). Maps to 400, distinct from AuthenticationFailedException's 401 - the
/// GlobalExceptionHandler mapping for this is added in this module's Step 8, alongside its controller,
/// matching how Module 1 introduced AuthenticationFailedException's mapping in its own Step 8.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

namespace ServiceControl.Application.Exceptions;

public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message) : base(message) { }
    protected ApplicationException(string message, Exception innerException) : base(message, innerException) { }
}

public class ValidationException : ApplicationException
{
    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base($"Falha de validação: {string.Join(", ", failures.Select(f => f.ErrorMessage))}")
    {
        Failures = failures;
    }

    public IEnumerable<FluentValidation.Results.ValidationFailure> Failures { get; }
}

public class WeatherServiceException : ApplicationException
{
    public WeatherServiceException(string message) : base(message) { }
    public WeatherServiceException(string message, Exception innerException) : base(message, innerException) { }
}
namespace ServiceControl.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidTemperatureException : DomainException
{
    public InvalidTemperatureException(string message) : base(message) { }
}

public class WorkRecordValidationException : DomainException
{
    public WorkRecordValidationException(string message) : base(message) { }
}
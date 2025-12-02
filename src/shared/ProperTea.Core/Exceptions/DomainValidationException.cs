namespace ProperTea.Core.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException()
    {
    }

    public DomainValidationException(string message) : base(message)
    {
    }
}

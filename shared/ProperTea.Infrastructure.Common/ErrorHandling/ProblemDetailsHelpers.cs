namespace ProperTea.Infrastructure.Common.ErrorHandling;

public static class ProblemDetailsHelpers
{
    public static (int StatusCode, string Title, string Details) GetExceptionDetails(Exception exception)
    {
        return (
            StatusCodeMapping.GetStatusCode(exception),
            StatusCodeMapping.GetTitle(exception),
            StatusCodeMapping.GetDetail(exception)
        );
    }
}

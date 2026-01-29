namespace ProperTea.Infrastructure.Common.ErrorHandling;

public class ErrorHandlingOptions
{
    public string? ServiceName { get; set; }
    public string? ProblemDetailsTypeBaseUrl { get; set; } = "https://httpstatuses.io";
}

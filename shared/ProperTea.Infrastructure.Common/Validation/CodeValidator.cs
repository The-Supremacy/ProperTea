using System.Text.RegularExpressions;
using ProperTea.Infrastructure.Common.Exceptions;

namespace ProperTea.Infrastructure.Common.Validation;

public static partial class CodeValidator
{
    [GeneratedRegex(@"^[A-Z0-9]+$")]
    private static partial Regex ValidCodePattern();

    public static void Validate(
        string code,
        int maxLength,
        string errorRequired,
        string errorTooLong,
        string errorInvalidFormat)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BusinessViolationException(errorRequired, "Code is required");

        if (code.Length > maxLength)
            throw new BusinessViolationException(
                errorTooLong,
                $"Code cannot exceed {maxLength} characters");

        if (!ValidCodePattern().IsMatch(code))
            throw new BusinessViolationException(
                errorInvalidFormat,
                "Code may only contain uppercase letters (A-Z) and digits (0-9)");
    }
}

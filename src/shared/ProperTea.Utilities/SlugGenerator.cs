using System.Text.RegularExpressions;

namespace ProperTea.Utilities;

public static partial class SlugGenerator
{
    public static string Generate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var slug = name.ToLowerInvariant().Trim();
        slug = ReplaceSeparatorsRegex().Replace(slug, "-");
        slug = ReplaceNonAlphabeticRegex().Replace(slug, "");
        slug = ReplaceMultipleHyphensRegex().Replace(slug, "-");
        slug = slug.Trim('-');

        return slug;
    }

    [GeneratedRegex(@"[\s\._]+")]
    private static partial Regex ReplaceSeparatorsRegex();

    [GeneratedRegex(@"[^a-z0-9-]")]
    private static partial Regex ReplaceNonAlphabeticRegex();

    [GeneratedRegex("-{2,}")]
    private static partial Regex ReplaceMultipleHyphensRegex();
}

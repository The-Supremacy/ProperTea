using System.Text.RegularExpressions;

namespace ProperTea.Utilities;

public static class SlugGenerator
{
    public static string Generate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // 1. Convert to lowercase and trim
        var slug = name.ToLowerInvariant().Trim();

        // 2. Replace spaces and other common separators with a hyphen
        slug = Regex.Replace(slug, @"[\s\._]+", "-");

        // 3. Remove all non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");

        // 4. Replace multiple hyphens with a single hyphen
        slug = Regex.Replace(slug, @"-{2,}", "-");

        // 5. Trim hyphens from the start and end
        slug = slug.Trim('-');

        return slug;
    }
}
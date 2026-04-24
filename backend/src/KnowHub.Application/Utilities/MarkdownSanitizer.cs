using System.Text.RegularExpressions;

namespace KnowHub.Application.Utilities;

/// <summary>
/// Sanitises user-supplied Markdown/HTML to prevent XSS (OWASP A03).
/// Strips script tags, iframe tags, and javascript: href/src values.
/// </summary>
public static class MarkdownSanitizer
{
    private static readonly Regex ScriptTagPattern =
        new(@"<script[\s\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IframeTagPattern =
        new(@"<iframe[\s\S]*?</iframe>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JavascriptHrefPattern =
        new(@"(href|src)\s*=\s*[""']\s*javascript:[^""']*[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OnEventAttributePattern =
        new(@"\s+on\w+\s*=\s*[""'][^""']*[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Sanitize(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return markdown;

        var result = ScriptTagPattern.Replace(markdown, string.Empty);
        result = IframeTagPattern.Replace(result, string.Empty);
        result = JavascriptHrefPattern.Replace(result, string.Empty);
        result = OnEventAttributePattern.Replace(result, string.Empty);

        return result;
    }
}

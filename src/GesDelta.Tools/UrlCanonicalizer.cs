namespace GesDelta.Tools;

public static class UrlCanonicalizer
{
    public static string Canonicalize(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return url.Trim();

        var result = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        
        var keptParams = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (keptParams.Count > 0)
            result = $"{result}?{string.Join("&", keptParams)}";
        
        return result;
    }
}
using AngleSharp.Html.Parser;

namespace GesDelta.Agents;

public static class HtmlToText
{
    public static string Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        try
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            foreach (var element in document.QuerySelectorAll("script, style"))
            {
                element.Remove();
            }
            
            
            var blockElements = document.QuerySelectorAll(
                "p, div, br, li, h1, h2, h3, h4, h5, h6, blockquote, pre"
            ).ToArray();

            foreach (var element in blockElements)
            {
                var separator = document.CreateTextNode("\n\n");

                element.Before(separator);
                element.After(document.CreateTextNode("\n\n"));
            }

            return document.Body?.TextContent.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
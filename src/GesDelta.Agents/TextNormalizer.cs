using System.Text.RegularExpressions;

namespace GesDelta.Agents;

public static class TextNormalizer
{
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    private static readonly Regex DynamicDate = new(
        @"^(?:Обновлено|Updated)\s*:?\s*(?:\d{1,2}[./-]\d{1,2}(?:[./-]\d{2,4})?|\d{4}-\d{2}-\d{2})(?:\s+\d{1,2}:\d{2})?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Counter = new(
        @"^\d+\s+(?:\**(?:просмотр\w*|комментар\w*|views?|comments?)\**)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex ReadMoreTail = new(
        @"^Читать далее(?:\.{1,3}|…)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        var lines = text.Split('\n')
            .Select(line => Spaces.Replace(line, " ").Trim());

        var paragraphs = new List<string>();
        var current = new List<string>();
        foreach (var line in lines)
        { 
            var currentLine = DynamicDate.Replace(line, "");

            currentLine = Counter.Replace(currentLine, "");
            
            if (currentLine.Length == 0)
            {
                if (current.Count > 0)
                {
                    paragraphs.Add(string.Join(" ", current));
                    current.Clear();
                }
            }
            else
            {
                current.Add(currentLine);
            }
        }
        if (current.Count > 0)
            paragraphs.Add(string.Join(" ", current));

        if (paragraphs.Count == 0)
            return string.Empty;

        var lastParagraph = paragraphs[^1];
        paragraphs.RemoveAt(paragraphs.Count - 1);

        lastParagraph = ReadMoreTail.Replace(lastParagraph, "").Trim();

        if (!string.IsNullOrWhiteSpace(lastParagraph))
            paragraphs.Add(lastParagraph);

        return string.Join("\n\n", paragraphs);
    }
}
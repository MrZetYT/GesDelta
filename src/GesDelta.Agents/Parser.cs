using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;

namespace GesDelta.Agents;

public class Parser : IParser
{
    public ParseResult Parse(FetchedItem item)
    {
        var text = HtmlToText.Convert(item.RawText);

        text = TextNormalizer.Normalize(text);

        if (String.IsNullOrWhiteSpace(text))
            return ParseResult.Failure(ParseFailureReason.EmptyText);
        
        var textHash = ContentHasher.Sha256(text);
        
        return ParseResult.Success(new ParsedItem(
            item.ExternalId,
            item.Title,
            item.Url,
            item.PublishedAt,
            text,
            textHash));
    }
}
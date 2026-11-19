using GesDelta.Core.Enums;
using GesDelta.Core.Models;

namespace GesDelta.Core.Interfaces;

public interface IParser
{
    ParseResult Parse(FetchedItem item, SourceType sourceType);
}
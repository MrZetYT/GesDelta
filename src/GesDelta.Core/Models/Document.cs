using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public class Document
{
    public long Id { get; set; }
    public Topic? Topic { get; set; }
    public long TopicId { get; set; }
    public DocumentKind Kind { get; set; }
}
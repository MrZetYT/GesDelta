namespace GesDelta.Core.Interfaces;

public interface ITextDiffer
{
    IReadOnlyList<string> GetAddedParagraphs(
        string baselineText,
        string currentText);
}
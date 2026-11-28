using DiffPlex;
using GesDelta.Core.Interfaces;

namespace GesDelta.Tools;

public sealed class DiffPlexTextDiffer : ITextDiffer
{
    private readonly Differ _differ = new();

    public IReadOnlyList<string> GetAddedParagraphs(
        string baselineText,
        string currentText)
    {
        var diff = _differ.CreateLineDiffs(
            baselineText,
            currentText,
            ignoreWhitespace: false);

        var added = new List<string>();

        foreach (var block in diff.DiffBlocks)
        {
            for (var i = 0; i < block.InsertCountB; i++)
            {
                var paragraph = diff.PiecesNew[
                    block.InsertStartB + i];

                if (!string.IsNullOrWhiteSpace(paragraph))
                {
                    added.Add(paragraph);
                }
            }
        }

        return added;
    }
}
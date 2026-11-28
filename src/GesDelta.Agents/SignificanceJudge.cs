using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using Microsoft.Extensions.Options;

namespace GesDelta.Agents;

public sealed class SignificanceJudge(
    ITextDiffer textDiffer,
    ITextSimilarity textSimilarity,
    IOptions<DetectorOptions> options)
    : ISignificanceJudge
{
    private readonly DetectorOptions _options = options.Value;

    public SignificanceResult Evaluate(
        string baselineText,
        string currentText)
    {
        var baselineParagraphs = baselineText.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        var candidates = textDiffer.GetAddedParagraphs(
            baselineText,
            currentText);

        var addedFragments = candidates
            .Where(p => p.Length >= _options.MinParagraphLength)
            .Where(p => !baselineParagraphs.Any(existing =>
                textSimilarity.Calculate(existing, p)
                >= _options.SimilarityThreshold))
            .ToArray();

        var totalAddedChars = addedFragments.Sum(p => p.Length);

        return new SignificanceResult(
            totalAddedChars >= _options.MinAddedChars,
            addedFragments);
    }
}
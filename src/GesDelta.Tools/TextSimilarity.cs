using GesDelta.Core.Interfaces;

namespace GesDelta.Tools;

public sealed class TextSimilarity : ITextSimilarity
{
    public double Calculate(string first, string second)
    {
        if (first == second)
            return 1.0;

        var maxLength = Math.Max(first.Length, second.Length);

        if (first.Length == 0 || second.Length == 0)
            return 0.0;

        var distance = GetDistance(first, second);

        return 1.0 - (double)distance / maxLength;
    }

    private static int GetDistance(string first, string second)
    {
        if (first.Length < second.Length)
            (first, second) = (second, first);

        var previous = new int[second.Length + 1];
        var current = new int[second.Length + 1];

        for (var j = 0; j <= second.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= first.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= second.Length; j++)
            {
                var cost = first[i - 1] == second[j - 1]
                    ? 0
                    : 1;

                current[j] = Math.Min(
                    Math.Min(
                        previous[j] + 1,
                        current[j - 1] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[second.Length];
    }
}
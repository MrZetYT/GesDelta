namespace GesDelta.Core.Interfaces;

public interface ITextSimilarity
{
    double Calculate(string first, string second);
}
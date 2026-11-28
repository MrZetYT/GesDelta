using GesDelta.Core.Models;

namespace GesDelta.Core.Interfaces;

public interface ISignificanceJudge
{
    SignificanceResult Evaluate(
        string baselineText,
        string currentText);
}
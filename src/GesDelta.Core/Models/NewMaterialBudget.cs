namespace GesDelta.Core.Models;

public sealed class NewMaterialBudget
{
    public int Remaining { get; private set; }

    public NewMaterialBudget(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);

        Remaining = limit;
    }

    public bool IsExhausted => Remaining == 0;

    public void Consume()
    {
        if (IsExhausted)
        {
            throw new InvalidOperationException(
                "New material budget is exhausted.");
        }

        Remaining--;
    }
}
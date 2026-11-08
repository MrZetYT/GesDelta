namespace GesDelta.Core.Options;

public class CollectorOptions
{
    public int[] PausesSec { get; set; } = [];
    public int HttpTimeoutSec { get; set; } = 30;
}
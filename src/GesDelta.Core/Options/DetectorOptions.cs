namespace GesDelta.Core.Options;

public class DetectorOptions
{
    public int MinParagraphLength { get; set; } = 40;

    public int MinAddedChars { get; set; } = 200;

    public double SimilarityThreshold { get; set; } = 0.8;

    public double MinTextLengthRatio { get; set; } = 0.2;
}
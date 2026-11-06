namespace RangeFinder.Domain.TextFileDetection;

public class TextFileDetectorDto
{
    public required List<string> KnownTextFileKeys { get; set; }
    public required List<string> KnownNonTextFileKeys { get; set; }
    public required List<string> InferredTextFileKeys { get; set; }
    public required List<string> InferredNonTextFileKeys { get; set; }
    public required List<string> AlwaysEvaluateKeys { get; set; }

    public static TextFileDetectorDto Create()
    {
        return new TextFileDetectorDto
        {
            KnownTextFileKeys = [],
            KnownNonTextFileKeys = [],
            InferredTextFileKeys = [],
            InferredNonTextFileKeys = [],
            AlwaysEvaluateKeys = []
        };
    }
}

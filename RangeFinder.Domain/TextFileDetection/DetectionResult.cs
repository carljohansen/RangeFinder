namespace RangeFinder.Domain.TextFileDetection;

public enum DetectionResult
{
    Indeterminate = 0,
    DeclaredTextFile,
    DeclaredNonTextFile,
    InferredTextFile,
    InferredNonTextFile,
    AlwaysEvaluate
}

public static class DetectionResultExtensions
{
    extension(DetectionResult detectionResult)
    {
        public bool IsConfirmedTextFile() => detectionResult is DetectionResult.DeclaredTextFile
                                                    or DetectionResult.InferredTextFile;
    }
}
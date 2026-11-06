namespace RangeFinder.Domain.TextFileDetection;

public enum DetectionResult
{
    Indeterminate = 0,
    KnownTextFileName,
    KnownTextFileExtension,
    KnownNonTextFile,
    DetectedTextFile,
    DetectedNonTextFile
}

public static class DetectionResultExtensions
{
    extension(DetectionResult detectionResult)
    {
        public bool IsConfirmedTextFile() => detectionResult is DetectionResult.KnownTextFileExtension
                                                    or DetectionResult.KnownTextFileName
                                                    or DetectionResult.DetectedTextFile;
    }
}
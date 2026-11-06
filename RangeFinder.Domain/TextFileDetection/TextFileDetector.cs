using RangeFinder.Domain.FileType;

namespace RangeFinder.Domain.TextFileDetection;

public class TextFileDetector
{
    public static TextFileDetector Instance { get; } = new TextFileDetector();

    private TextFileDetector()
    {
    }

    private readonly Dictionary<TextFileDetectionKey, DetectionResult> _cache = [];

    public DetectionResult Detect(string filePath)
    {
        var key = TextFileDetectionKey.FromFilePath(filePath);
        if (_cache.TryGetValue(key, out var cachedResult))
        {
            return cachedResult;
        }
        var result = DetectTextFileStatus(filePath);
        _cache[key] = result;

        return result;
    }

    public static DetectionResult DetectTextFileStatus(string filePath)
    {
        // Implement the logic to detect if the file is a text file or not.
        // This is a placeholder implementation. You should replace it with actual detection logic.
        if (string.IsNullOrEmpty(filePath))
        {
            return DetectionResult.Indeterminate;
        }
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (FileTypeHelper.IsKnownTextFile(extension))
        {
            return DetectionResult.DeclaredTextFile;
        }
        else if (FileTypeHelper.IsKnownBinaryFile(extension))
        {
            return DetectionResult.DeclaredNonTextFile;
        }

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        if (fileName == ".gitignore" || fileName == ".gitattributes")
        {
            return DetectionResult.DeclaredTextFile;
        }

        try
        {
            var heavyDetectionResult = UtfUnknown.CharsetDetector.DetectFromFile(filePath);

            return heavyDetectionResult.Detected == null
                                ? DetectionResult.InferredNonTextFile
                                : DetectionResult.InferredTextFile;
        }
        catch (IOException)
        {
            // Probably file is in use.
            return DetectionResult.Indeterminate;
        }
    }
}

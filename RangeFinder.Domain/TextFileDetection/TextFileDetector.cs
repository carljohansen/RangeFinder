namespace RangeFinder.Domain.TextFileDetection;

public class TextFileDetector
{
    public static TextFileDetector Instance { get; } = new TextFileDetector();

    private readonly Dictionary<TextFileDetectionKey, FileTextTreatment> _cache = [];

    private TextFileDetector()
    {
    }

    public void AddRange(IEnumerable<TextFileDetectionKey> keys, FileTextTreatment fileTextTreatment)
    {
        foreach (var key in keys)
        {
            _cache[key] = fileTextTreatment;
        }
    }

    public TextFileDetectorDto ToDto()
    {
        var dto = TextFileDetectorDto.Create();
        foreach (var kvp in _cache)
        {
            var targetList = kvp.Value switch
            {
                FileTextTreatment.DeclaredTextFile => dto.KnownTextFileKeys,
                FileTextTreatment.DeclaredNonTextFile => dto.KnownNonTextFileKeys,
                FileTextTreatment.InferredTextFile => dto.InferredTextFileKeys,
                FileTextTreatment.InferredNonTextFile => dto.InferredNonTextFileKeys,
                FileTextTreatment.AlwaysEvaluate => dto.AlwaysEvaluateKeys,
                _ => throw new InvalidOperationException($"Unexpected detection result: {kvp.Value}")
            };
            targetList.Add(kvp.Key.ToSerializedString());
        }

        return dto;
    }

    public FileTextTreatment Detect(string filePath)
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

    public bool IsKnownTextFileType(string filePath)
    {
        var key = TextFileDetectionKey.FromFilePath(filePath);
        if (_cache.TryGetValue(key, out var cachedResult))
        {
            return cachedResult.IsConfirmedTextFile();
        }

        return false;
    }

    public bool IsKnownNonTextFileType(string filePath)
    {
        var key = TextFileDetectionKey.FromFilePath(filePath);
        if (_cache.TryGetValue(key, out var cachedResult))
        {
            return !cachedResult.IsConfirmedTextFile();
        }

        return false;
    }

    private static FileTextTreatment DetectTextFileStatus(string filePath)
    {
        // Implement the logic to detect if the file is a text file or not.
        // This is a placeholder implementation. You should replace it with actual detection logic.
        if (string.IsNullOrEmpty(filePath))
        {
            return FileTextTreatment.Indeterminate;
        }

        try
        {
            var heavyDetectionResult = UtfUnknown.CharsetDetector.DetectFromFile(filePath);

            return heavyDetectionResult.Detected == null
                                ? FileTextTreatment.InferredNonTextFile
                                : FileTextTreatment.InferredTextFile;
        }
        catch (IOException)
        {
            // Probably file is in use.
            return FileTextTreatment.Indeterminate;
        }
    }
}

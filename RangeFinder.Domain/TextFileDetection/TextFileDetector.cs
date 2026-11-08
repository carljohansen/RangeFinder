namespace RangeFinder.Domain.TextFileDetection;

public class TextFileDetector
{
    public static TextFileDetector Instance { get; } = new TextFileDetector();

    public bool IsDirty { get; private set; } = false;

    private readonly Dictionary<TextFileDetectionKey, FileTextTreatment> _cache = [];

    private TextFileDetector()
    {
    }

    internal void AddRange(IEnumerable<TextFileDetectionKey> keys, FileTextTreatment fileTextTreatment)
    {
        // This should not be called from outside of the repository.
        foreach (var key in keys)
        {
            _cache[key] = fileTextTreatment;
        }
    }

    public void AddCustomEntry(TextFileDetectionKey key, FileTextTreatment fileTextTreatment)
    {
        _cache[key] = fileTextTreatment;
        IsDirty = true;
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

    public FileTextTreatment GetRegisteredTreatment(string filePath) => GetRegisteredTreatment(TextFileDetectionKey.FromFilePath(filePath));

    public FileTextTreatment GetRegisteredTreatment(TextFileDetectionKey key)
    {
        return _cache.TryGetValue(key, out var cachedResult) ? cachedResult : FileTextTreatment.Indeterminate;
    }

    public FileTextTreatment Detect(string filePath)
    {
        var key = TextFileDetectionKey.FromFilePath(filePath);
        var registeredTreatment = GetRegisteredTreatment(key);
        if (registeredTreatment is not FileTextTreatment.Indeterminate and not FileTextTreatment.AlwaysEvaluate)
        {
            return registeredTreatment;
        }

        var result = DetectTextFileStatus(filePath);
        if (registeredTreatment != FileTextTreatment.AlwaysEvaluate)
        {
            _cache[key] = result;
            IsDirty = true;
        }

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
            return !(cachedResult.IsConfirmedTextFile() || cachedResult == FileTextTreatment.AlwaysEvaluate);
        }

        return false;
    }

    public void MarkAsSaved()
    {
        IsDirty = false;
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

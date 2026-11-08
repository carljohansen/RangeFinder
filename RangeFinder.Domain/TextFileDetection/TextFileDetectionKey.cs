namespace RangeFinder.Domain.TextFileDetection;

public record TextFileDetectionKey(string? FileName, string? FileExtension)
{
    public static TextFileDetectionKey FromFilePath(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

        if (fileName == fileExtension)
        {
            return new(fileName, null);
        }

        if (string.IsNullOrEmpty(fileExtension))
        {
            return new(fileName, null);
        }

        return new TextFileDetectionKey(null, fileExtension);
    }

    public string ToDisplayString()
    {
        return FileName ?? FileExtension ?? string.Empty;
    }

    public string ToSerializedString()
    {
        return $"{(FileName == null ? "e" : "f")}:{FileName ?? FileExtension}";
    }

    public static TextFileDetectionKey FromSerializedString(string serializedString)
    {
        var parts = serializedString.Split(':', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid serialized string format.", nameof(serializedString));
        }
        var type = parts[0];
        var value = parts[1];
        return type switch
        {
            "f" => new TextFileDetectionKey(value, null),
            "e" => new TextFileDetectionKey(null, value),
            _ => throw new ArgumentException("Invalid serialized string format.", nameof(serializedString)),
        };
    }
}

namespace RangeFinder.Domain.TextFileDetection;

public record TextFileDetectionKey(string? FileName, string? FileExtension)
{
    public static TextFileDetectionKey FromFilePath(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var fileExtension = Path.GetExtension(filePath);

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
}

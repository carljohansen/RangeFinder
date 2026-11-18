using System.Diagnostics.CodeAnalysis;

namespace RangeFinder.Domain;

public class FileLocation
{
    public required string Location { get; init; }

    public string? LocationDisplay { get; private set; }

    public int CurrentDisplayLength { get; private set; }

    private string _baseDirectory;

    [SetsRequiredMembers]
    public FileLocation(string location, int initialDisplayLength, string baseDirectory)
    {
        Location = location;
        _baseDirectory = baseDirectory;
        SetDisplayLength(initialDisplayLength);
    }

    public void SetDisplayLength(int displayLength)
    {
        CurrentDisplayLength = displayLength;
        LocationDisplay = GetLocationDisplay(displayLength);
    }

    public string? GetLocationDisplay(int maxLength) => ShortenPath(Location, maxLength);

    private string? ShortenPath(string path, int maxLength)
    {
        var numCharsToRemove = path.Length - maxLength;
        if (numCharsToRemove <= 0)
        {
            return null;
        }

        if (path.Length == _baseDirectory.Length
            || path.Length == _baseDirectory.Length - 1)
        {
            return Path.DirectorySeparatorChar.ToString();
        }

        if (path.StartsWith(_baseDirectory))
        {
            path = path[_baseDirectory.Length..];
        }

        numCharsToRemove = path.Length - maxLength;
        if (numCharsToRemove <= 0)
        {
            return path;
        }

        var startPos = (path.Length - numCharsToRemove) / 2;
        return $"{path[0..startPos]}{EliderConstants.Ellipsis}{path[(startPos + numCharsToRemove)..]}";
    }

    public static IEnumerable<string> DirectorySplit(DirectoryInfo directory)
    {
        while (directory != null)
        {
            yield return directory.Name;
            directory = directory.Parent!;
        }
    }
}

namespace RangeFinder.Domain.TextSearch;

public class FileMatcher
{
    private readonly string? baseDirectory;

    public FileMatcher(string? BaseDirectory, IEnumerable<AtomicTextMatch> atomicTextMatches)
    {
        baseDirectory = BaseDirectory;
        Matchers = atomicTextMatches.Select(atm => new LineTextMatcher(atm)).ToArray();
        NumExclusiveChecks = Matchers.Count(m => m.IsExclusion);
        NumInclusiveChecks = Matchers.Length - NumExclusiveChecks;
    }

    public bool HasTextChecks => Matchers.Length > 0;

    public int NumTextChecks => NumInclusiveChecks + NumExclusiveChecks;

    public readonly int NumInclusiveChecks;

    public readonly int NumExclusiveChecks;

    public readonly LineTextMatcher[] Matchers;

    private string GetFullPath(string filePath)
    {
        if (baseDirectory is null)
        {
            return filePath; // This better be absolute!
        }
        return Path.Combine(baseDirectory, filePath);
    }

    public void GetMatches(string filePath, Action<PreviewLineGeneratedEventArgs> matchAction, int max = 200)
    {
        var numMatchingLinesFound = 0;

        var fullPath = GetFullPath(filePath);
        int lineNumber = 0;

        foreach (var line in File.ReadLines(fullPath))
        {
            PreviewLineGeneratedEventArgs? e = null;
            lineNumber++;
            var lineHasMatch = false;
            foreach (var matcher in Matchers)
            {
                var match = matcher.GetMatch(line, lineNumber);
                if (match != null)
                {
                    e = new(match);
                    matchAction(e);
                    lineHasMatch = true;
                }
            }

            if (lineHasMatch)
            {
                numMatchingLinesFound++;
                if (numMatchingLinesFound >= max)
                {
                    break;
                }
            }
            else
            {
                e = new(new FileLinePlainPreview(line, lineNumber));
                matchAction(e);
            }

            if (e?.Cancel == true)
            {
                break;
            }
        }
    }
}
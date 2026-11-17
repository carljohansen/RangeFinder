namespace RangeFinder.Domain.TextSearch;

public record FileLineTextMultiMatch(List<FileLineTextMatch> Matches) : IFileLineDisplay
{
    public int LineNumber => Matches[0].LineNumber;

    public string Line => Matches[0].Line;

    public int SecondarySortKey => Matches[0].SecondarySortKey;

    public bool HasMatches => true;

    public bool HasOverlaps()
    {
        for (int i = 1; i < Matches.Count; i++)
        {
            if (Matches[i].StartPos < Matches[i - 1].StartPos + Matches[i - 1].Length)
            {
                return true;
            }
        }
        return false;
    }

    public MatchSegmentDisplay[] GetSegments()
    {
        var result = new List<MatchSegmentDisplay>();
        // Assumptions:
        // * All matches are on the same line.
        // * Matches are sorted by StartPos.
        var lastSegmentEndPos = 0;
        foreach (var match in Matches)
        {
            var prefixText = Line[lastSegmentEndPos..match.StartPos];
            if (prefixText.Length > 0)
            {
                result.Add(new MatchSegmentDisplay(prefixText, 0));
            }
            lastSegmentEndPos = match.StartPos + match.Length;
            var matchText = Line[match.StartPos..lastSegmentEndPos];
            result.Add(new MatchSegmentDisplay(matchText, match.Source.Index + AtomicTextMatch.MatchHighlightColourIndexBase - 1));
        }

        var lastMatch = Matches[^1];
        var suffixText = Line[(lastMatch.StartPos + lastMatch.Length)..];
        if (suffixText.Length > 0)
        {
            result.Add(new MatchSegmentDisplay(suffixText, 0));
        }
        return [.. result];
    }
}

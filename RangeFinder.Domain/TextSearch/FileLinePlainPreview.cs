namespace RangeFinder.Domain.TextSearch;

public record FileLinePlainPreview(string Line, int LineNumber) : IFileLineDisplay
{
    public bool HasMatches => false;

    public MatchSegmentDisplay[] GetSegments()
    {
        return [new(Line, 1)];
    }
}
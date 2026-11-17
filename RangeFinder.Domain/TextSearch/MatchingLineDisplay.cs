namespace RangeFinder.Domain.TextSearch;

public record MatchingLineDisplay(string Line, int LineNumber, MatchSegmentDisplay[] Segments) : IFileLineDisplay
{
    public bool HasMatches => true;

    public MatchSegmentDisplay[] GetSegments() => Segments;
}
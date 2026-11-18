namespace RangeFinder.Domain.TextSearch;

public record MatchingLineDisplay(string Line, int LineNumber, MatchSegmentDisplay[] Segments) : IFileLineDisplay
{
    public bool HasMatches => true;

    public int Length => Segments.Sum(s => s.SegmentText.Length);

    public MatchSegmentDisplay[] GetSegments() => Segments;
}
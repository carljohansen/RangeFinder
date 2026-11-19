using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RangeFinder.Domain.Tests")]

namespace RangeFinder.Domain.TextSearch;

public record FileLineTextMatch(string Line, int LineNumber, int StartPos, int Length, AtomicTextMatch Source) : IFileLineDisplay
{
    public bool HasMatches => true;

    public MatchSegmentDisplay[] GetSegments()
    {
        throw new NotImplementedException("Use a MatchingLineDisplay instead.");
    }
}

public record MatchSegmentDisplay(string SegmentText, int SegmentBackgroundColourIndex)
{
    public bool IsHighlightedMatch = SegmentBackgroundColourIndex > 1;

    public bool IsInterMatchText = SegmentBackgroundColourIndex == 0;

    public int SegmentTextColourIndex => SegmentBackgroundColourIndex == 1 ? 1 : 0; // 1 = dark gray, 0 = black
}
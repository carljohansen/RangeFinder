namespace RangeFinder.Domain.TextSearch;

public record FileLinePlainPreview(string Line, int LineNumber) : IFileLineDisplay
{
    public bool HasMatches => false;

    public MatchSegmentDisplay[] GetSegments()
    {
        var previewText = MatchingLineDisplayElider.ElideStringToFit(Line, 200, ElidingAction.ElideEnd);
        return [new(previewText, 1)];
    }
}
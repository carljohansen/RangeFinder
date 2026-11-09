namespace RangeFinder.Domain.TextSearch;

public interface IFileLineDisplay
{
    public int LineNumber { get; }

    public string Line { get; }

    public int SecondarySortKey { get; }

    public bool HasMatches { get; }

    MatchSegmentDisplay[] GetSegments();

    public bool IsBlank => string.IsNullOrWhiteSpace(Line);
}
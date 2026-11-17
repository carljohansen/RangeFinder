namespace RangeFinder.Domain.TextSearch;

public static class MatchingLineDisplayFactory
{
    public static MatchingLineDisplay Create(List<FileLineTextMatch> matchList)
    {
        if (!matchList.Any())
        {
            throw new ArgumentException("The matches collection must contain at least one match.", nameof(matchList));
        }

        var firstMatch = matchList.First();
        var lineNumber = firstMatch.LineNumber;
        var lineText = firstMatch.Line;

        var unelidedSegments = matchList.Select(m => new MatchSegmentDisplay(lineText.Substring(m.StartPos, m.Length), m.Source.Index)).ToList();
        var currentPos = 0;
        var currentIndex = 0;

        // 1. Add the prefix segment
        // 2. for each item k in matchList, add a segment between [k] and [k+1].  Now [k+1] is [k+2]
        while (currentPos < lineText.Length)
        {
            if (currentPos < matchList[currentIndex].StartPos)
            {
                var prefixLength = matchList[currentIndex].StartPos - currentPos;
                var prefixText = lineText.Substring(currentPos, prefixLength);
                unelidedSegments.Insert(currentIndex, new MatchSegmentDisplay(prefixText, 0));
                currentPos = matchList[currentIndex].StartPos;
            }
            currentIndex++;
        }

        return new MatchingLineDisplay(lineText, lineNumber, [.. unelidedSegments]);
    }
}
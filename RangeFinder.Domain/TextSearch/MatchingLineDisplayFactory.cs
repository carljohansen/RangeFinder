namespace RangeFinder.Domain.TextSearch;

public static class MatchingLineDisplayFactory
{
    public static MatchingLineDisplay Create(List<FileLineTextMatch> matchList)
    {
        // TODO: return null if unable to create (e.g. because of overlaps).

        if (matchList.Count == 0)
        {
            throw new ArgumentException("The matches collection must contain at least one match.", nameof(matchList));
        }

        var firstMatch = matchList[0];
        var lineNumber = firstMatch.LineNumber;
        var lineText = firstMatch.Line;

        var unelidedSegments = matchList.Select(m => new MatchSegmentDisplay(lineText.Substring(m.StartPos, m.Length), m.Source.Index + AtomicTextMatch.MatchHighlightColourIndexBase - 1)).ToList();
        var currentIndex = 0;
        var currentInsertionPoint = 1;

        if (matchList[0].StartPos > 0)
        {
            var prefixText = lineText[..matchList[0].StartPos];
            unelidedSegments.Insert(0, new MatchSegmentDisplay(prefixText, 0));
            currentInsertionPoint++;
        }

        while (currentIndex < matchList.Count)
        {
            var endPosOfCurrentMatch = matchList[currentIndex].StartPos + matchList[currentIndex].Length;
            var startPosOfNextMatch = matchList.Count > (currentIndex + 1) ? matchList[currentIndex + 1].StartPos : lineText.Length;
            var interSegmentLength = startPosOfNextMatch - endPosOfCurrentMatch;
            if (interSegmentLength > 0)
            {
                var interSegmentText = lineText.Substring(endPosOfCurrentMatch, interSegmentLength);
                var interSegment = new MatchSegmentDisplay(interSegmentText, 0);
                unelidedSegments.Insert(currentInsertionPoint++, interSegment);
            }
            currentInsertionPoint++;
            currentIndex++;
        }

        var unelidedResult = new MatchingLineDisplay(lineText, lineNumber, [.. unelidedSegments]);
        return MatchingLineDisplayElider.Elide(unelidedResult, 200, true);
    }
}
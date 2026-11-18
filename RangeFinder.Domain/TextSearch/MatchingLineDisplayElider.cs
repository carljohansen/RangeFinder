namespace RangeFinder.Domain.TextSearch;

public static class MatchingLineDisplayElider
{
    public static MatchingLineDisplay Elide(MatchingLineDisplay rawLine, int maxLineLength, bool allowTrimming)
    {
        var initialLength = rawLine.Length;
        if (initialLength <= maxLineLength)
        {
            return rawLine;
        }

        var prefixSegment = rawLine.Segments[0];
        var suffixSegment = rawLine.Segments[^1];

        if (allowTrimming)
        {
            if (prefixSegment.IsInterMatchText)
            {
                if (prefixSegment.SegmentText.AsSpan()[..1].IsWhiteSpace())
                {
                    rawLine.Segments[0] = prefixSegment with { SegmentText = prefixSegment.SegmentText.TrimStart() };
                }

                if (rawLine.Length <= maxLineLength)
                {
                    return rawLine;
                }
            }

            if (suffixSegment.IsInterMatchText)
            {
                if (suffixSegment.SegmentText.AsSpan()[..1].IsWhiteSpace())
                {
                    rawLine.Segments[^1] = suffixSegment with { SegmentText = suffixSegment.SegmentText.TrimEnd() };
                }

                if (rawLine.Length <= maxLineLength)
                {
                    return rawLine;
                }
            }
        }

        var numSegments = rawLine.Segments.Length;
        var prefixLength = prefixSegment.SegmentText.Length;
        var suffixLength = suffixSegment.SegmentText.Length;

        const int maxPrefixLength = 20;


        if (prefixLength > maxPrefixLength)
        {
            var newPrefixText = ElideString(prefixSegment.SegmentText, prefixLength - maxPrefixLength, ElidingAction.ElideStart);
            rawLine.Segments[0] = prefixSegment with { SegmentText = newPrefixText };

            if (rawLine.Length <= maxLineLength)
            {
                return rawLine;
            }
        }

        if (numSegments > 3)
        {
            const int maxInterSegmentLength = 20;
            for (var i = 1; i < numSegments - 1; i++)
            {
                var currSegment = rawLine.Segments[i];
                if (currSegment.IsInterMatchText
                    && currSegment.SegmentText.Length > maxInterSegmentLength)
                {
                    rawLine.Segments[i] = rawLine.Segments[i] with { SegmentText = ElideString(currSegment.SegmentText, currSegment.SegmentText.Length - maxInterSegmentLength, ElidingAction.ElideMiddle) };
                }
            }
        }

        var numToRemoveFromSuffix = rawLine.Length - maxLineLength;
        if (numToRemoveFromSuffix > 0)
        {
            var newSuffixText = ElideString(suffixSegment.SegmentText, numToRemoveFromSuffix, ElidingAction.ElideEnd);
            rawLine.Segments[^1] = suffixSegment with { SegmentText = newSuffixText };

            if (rawLine.Length <= maxLineLength)
            {
                return rawLine;
            }
        }


        return rawLine;
    }

    internal static string ElideString(string s, int numCharsToRemove, ElidingAction elidingAction)
    {
        if (numCharsToRemove <= 0 || numCharsToRemove >= s.Length)
        {
            return s;
        }

        return elidingAction switch
        {
            ElidingAction.ElideStart => EliderConstants.Ellipsis + s[numCharsToRemove..],
            ElidingAction.ElideMiddle => RemoveFromMiddle(s, numCharsToRemove),
            ElidingAction.ElideEnd => s[..(s.Length - numCharsToRemove)] + EliderConstants.Ellipsis,
            _ => s,
        };
    }

    private static string RemoveFromMiddle(string s, int numCharsToRemove)
    {
        var lhsSize = (s.Length - numCharsToRemove) / 2;
        var rhsSize = numCharsToRemove - lhsSize;
        return s[..lhsSize] + EliderConstants.Ellipsis + s[rhsSize..];
    }
}

internal enum ElidingAction
{
    None,
    ElideStart,
    ElideMiddle,
    ElideEnd
}

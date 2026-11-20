using RangeFinder.Domain.TextSearch;

namespace RangeFinder.Domain.Tests;

public class FileLineTextMatchTests
{
    [Fact]
    public void SplitsIntoSegmentsCorrectly()
    {
        var line = "This is a sample line with the word hello.";
        var match = new FileLineTextMatch(line, 1, 10, 6, new AtomicTextMatch("sample", TextMatchingOptions.None, false, 0)); // "sample" starts at index 10 and has length 6
        var segments = match.GetSegments();
        Assert.Equal(3, segments.Length);
        Assert.Equal("This is a ", segments[0].SegmentText);
        Assert.Equal(0, segments[0].SegmentBackgroundColourIndex);
        Assert.Equal("sample", segments[1].SegmentText);
        Assert.Equal(1, segments[1].SegmentBackgroundColourIndex);
        Assert.Equal(" line with the word hello.", segments[2].SegmentText);
        Assert.Equal(0, segments[2].SegmentBackgroundColourIndex);
    }   
}

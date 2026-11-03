namespace RangeFinder.Domain.History;

public enum TabHistoryPopResult
{
    NoData, // Nothing to pop
    Back, // The top matched the current search, so it was popped.
    Revert // The top did not match the current search, so it was returned but not popped.
}
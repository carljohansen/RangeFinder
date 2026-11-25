namespace RangeFinder.Domain.Utilities;

public class ListIncrementalSearcher
{
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMilliseconds(1400);

    private string _currentSearch = string.Empty;

    private DateTime _lastUpdate = DateTime.MinValue;

    public ListIncrementalSearcher()
    {
    }

    private bool HasExpired => DateTime.Now - _lastUpdate > TimeToLive;

    public string AddChar(char c)
    {
        if (HasExpired)
        {
            _currentSearch = string.Empty;
        }

        _lastUpdate = DateTime.Now;
        _currentSearch += c;

        return _currentSearch;
    }

    public string ResetToChar(char c)
    {
        _currentSearch = c.ToString();
        _lastUpdate = DateTime.Now;

        return _currentSearch;
    }
}
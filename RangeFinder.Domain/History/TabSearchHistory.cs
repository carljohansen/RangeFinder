using System.Text.Json.Serialization;

namespace RangeFinder.Domain.History;

public class TabSearchHistory
{
    public Stack<UserSearchDto> History { get; }

    public Stack<UserSearchDto> ForwardHistory { get; }

    public TabSearchHistory()
    {
        History = new();
        ForwardHistory = new();
    }

    [JsonConstructor]
    public TabSearchHistory(Stack<UserSearchDto> history, Stack<UserSearchDto> forwardHistory)
    {
        History = history;
        ForwardHistory = forwardHistory;
    }

    public static TabSearchHistory Create(UserSearchDto initialSearch)
    {
        var history = new TabSearchHistory();
        history.History.Push(initialSearch);
        return history;
    }

    public UserSearchDto GetCurrent() => Peek() ?? UserSearchDto.CreateDefault();

    public UserSearchDto? Peek()
    {
        return History.Count > 0 ? History.Peek() : null;
    }

    public bool Push(UserSearchDto searchDto)
    {
        if (History.Count > 0 && History.Peek().Equals(searchDto))
        {
            return false;
        }

        History.Push(searchDto);
        ForwardHistory.Clear();
        return true;
    }

    public TabHistoryPopResult TryPop(UserSearchDto currentSearch, out UserSearchDto? previousSearch)
    {
        previousSearch = null;
        if (History.Count == 0)
        {
            return TabHistoryPopResult.NoData;
        }
        var top = History.Peek();
        if (top.Equals(currentSearch))
        {
            if (History.Count == 1)
            {
                return TabHistoryPopResult.NoData;
            }

            History.Pop();
            ForwardHistory.Push(currentSearch);

            previousSearch = History.Peek();
            return TabHistoryPopResult.Back;
        }
        else
        {
            ForwardHistory.Push(currentSearch);
            previousSearch = top;
            return TabHistoryPopResult.Revert;
        }
    }

    public TabHistoryPopResult TryGoForward(UserSearchDto currentSearch, out UserSearchDto? restoredSearch)
    {
        restoredSearch = null;
        if (ForwardHistory.Count == 0)
        {
            return TabHistoryPopResult.NoData;
        }

        restoredSearch = ForwardHistory.Pop();
        if (History.Count == 0
            || !History.Peek().Equals(currentSearch))
        {
            History.Push(currentSearch);
        }
        return TabHistoryPopResult.Forward;
    }
}

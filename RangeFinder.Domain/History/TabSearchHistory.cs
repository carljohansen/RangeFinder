namespace RangeFinder.Domain.History;

public class TabSearchHistory
{
    public Stack<UserSearchDto> History { get; } = new();

    public bool Push(UserSearchDto searchDto)
    {
        if (History.Count > 0 && History.Peek().Equals(searchDto))
        {
            return false;
        }

        History.Push(searchDto);
        return true;
    }

    public TabHistoryPopResult TryPop(UserSearchDto currentSearch, out UserSearchDto? poppedSearch)
    {
        poppedSearch = null;
        if (History.Count == 0)
        {
            return TabHistoryPopResult.NoData;
        }
        var top = History.Peek();
        if (top.Equals(currentSearch))
        {
            poppedSearch = History.Pop();
            return TabHistoryPopResult.Back;
        }
        else
        {
            poppedSearch = top;
            return TabHistoryPopResult.Revert;
        }
    }
}

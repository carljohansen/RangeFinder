using RangeFinder.Domain.TextSearch;

namespace RangeFinder.Domain.History;

public class UserTabSettings
{
    public required int Id { get; set; }

    public string? ResultSortOrder { get; set; }

    public int? SplitterDistance { get; set; }

    public required List<int> ColumnWidths { get; set; }

    public TextFilePreviewMode TextFilePreviewMode { get; set; }

    public byte ContextWindowSize { get; set; }

    public static UserTabSettings CreateDefault() => new()
    {
        Id = 0,
        ResultSortOrder = null,
        SplitterDistance = null,
        ColumnWidths = [],
        TextFilePreviewMode = TextFilePreviewMode.None
    };
}

public record UserTabState(TabSearchHistory TabSearchHistory, UserTabSettings TabSettings)
{
    public UserSearchDto SearchDto => TabSearchHistory.GetCurrent();

    public int Id => SearchDto.Id;

    public static UserTabState CreateDefault() => new(TabSearchHistory.CreateDefault(), UserTabSettings.CreateDefault());

    public static UserTabState Create(UserSearchDto searchDto, UserTabSettings tabSettings)
    {
        var result = new UserTabState(TabSearchHistory.Create(searchDto), tabSettings);
        if (tabSettings.Id != searchDto.Id)
        {
            tabSettings.Id = searchDto.Id;
        }

        return result;
    }

    private static UserTabState Create(TabSearchHistory tabSearchHistory, UserTabSettings tabSettings)
    {
        var result = new UserTabState(tabSearchHistory, tabSettings);
        if (tabSettings.Id != tabSearchHistory.Id)
        {
            tabSettings.Id = tabSearchHistory.Id;
        }

        return result;
    }

    public static UserTabState Create(TabSearchHistory tabSearchHistory, UserSearchDto currentUnappliedSearch, UserTabSettings userTabSettings)
    {
        var historyClone = UserSearchStackRepository.CloneTabSearchHistory(tabSearchHistory);
        historyClone.Push(currentUnappliedSearch);

        return Create(historyClone, userTabSettings);
    }
}
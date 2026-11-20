using RangeFinder.Domain.History;

namespace RangeFinder.Domain.Tests;

public class HistoryStackRepoTests
{
    [Fact]
    public void LoadsFromFile()
    {
        var actual = UserSearchStackRepository.LoadSavedSearches();
    }
}

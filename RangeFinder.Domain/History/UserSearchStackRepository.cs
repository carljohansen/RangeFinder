using System.Text.Json;

namespace RangeFinder.Domain.History;

public static class UserSearchStackRepository
{
    public static void SaveSearchStack(TabSearchHistory dto, string name)
    {
        var fileName = UserSearchRepository.GetDtoFileName(name);

        string jsonString = JsonSerializer.Serialize(dto, UserSearchRepository.SerializationOptionsIncludeReadOnlyProps);

        // Save JSON to file
        File.WriteAllText(fileName, jsonString);
    }

    public static void SaveSearchStack(TabSearchHistory dto)
    {
        SaveSearchStack(dto, GetFileName(dto.Id));
    }

    private static string GetFileName(int id) => $"tab_{id:000}";

    public static void DeleteSearchStack(int index)
    {
        var fileName = UserSearchRepository.GetDtoFileName(GetFileName(index));
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }
    }

    public static TabSearchHistory[] LoadSavedSearches()
    {
        var storageDir = UserSearchRepository.GetStorageDirectory();
        var searchFiles = Directory.GetFiles(storageDir, "tab_*.json");
        var savedSearches = new List<TabSearchHistory>();
        foreach (var fileName in searchFiles.OrderBy(f => f))
        {
            var dto = GetSearchStack(fileName);
            if (dto != null)
            {
                savedSearches.Add(dto);
            }
        }
        return [.. savedSearches];
    }

    private static TabSearchHistory? GetSearchStack(string name)
    {
        var fileName = UserSearchRepository.GetDtoFileName(name);
        if (!File.Exists(fileName))
        {
            return null;
        }

        var json = File.ReadAllText(fileName);

        var result = JsonSerializer.Deserialize<TabSearchHistory>(json, UserSearchRepository.SerializationOptionsIncludeReadOnlyProps);
        return result;
    }
}

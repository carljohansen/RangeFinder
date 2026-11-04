namespace RangeFinder.Domain.History;

public class DirectoryTextCriterionHistory : TextCriterionHistory
{
    public static bool CompareDirectoryPaths(string dir1, string dir2)
    {
        return string.Equals(
            Path.GetFullPath(dir1).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(dir2).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    public DirectoryTextCriterionHistory(int maxItems, string? fileName) : base(maxItems, fileName, CompareDirectoryPaths)
    {
    }

    public override string[] GetPossibleContinuations(string userInput)
    {
        var possibleContinuations = DirectoryPredictiveTexter.GetPossibleContinuations(userInput);

        if (possibleContinuations.Length == 0 && !string.IsNullOrWhiteSpace(userInput))
        {
            possibleContinuations = base.GetPossibleContinuations(userInput);
        }

        if (Path.EndsInDirectorySeparator(userInput)
            && !possibleContinuations.Contains(userInput))
        {
            possibleContinuations = [userInput, .. possibleContinuations];
        }

        return possibleContinuations;
    }
}
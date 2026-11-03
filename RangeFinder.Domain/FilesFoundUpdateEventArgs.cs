namespace RangeFinder.Domain;

public class FilesFoundUpdateEventArgs : EventArgs
{
    public int FilesFound { get; }

    public bool SearchIncludesFolders { get; }

    public FileSearchStatus SearchStatus { get; }

    public FilesFoundUpdateEventArgs(int filesFound, bool searchIncludesFolders, FileSearchStatus searchStatus)
    {
        FilesFound = filesFound;
        SearchIncludesFolders = searchIncludesFolders;
        SearchStatus = searchStatus;
    }
}
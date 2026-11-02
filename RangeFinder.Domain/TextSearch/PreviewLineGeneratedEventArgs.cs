namespace RangeFinder.Domain.TextSearch;

public class PreviewLineGeneratedEventArgs : EventArgs
{
    public bool Cancel { get; set; }

    public IFileLineDisplay FileLineDisplay { get; private init; }

    public PreviewLineGeneratedEventArgs(IFileLineDisplay fileLineDisplay)
    {
        FileLineDisplay = fileLineDisplay;
    }
}

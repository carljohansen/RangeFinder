namespace RangeFinder.Domain;

public class SearchProgressReportArgs
{
    public required string StatusMessage { get; set; }

    public int? NumFilesBeforeTextMatch { get; set; }

    public required int ProgressPercentage { get; set; }
}


public class ExtractionProgressEventArgs
{
    public required int ProgressPercentage { get; set; }
}

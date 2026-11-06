namespace RangeFinder.Domain.TextFileDetection;

public enum FileTextTreatment
{
    Indeterminate = 0,
    DeclaredTextFile,
    DeclaredNonTextFile,
    InferredTextFile,
    InferredNonTextFile,
    AlwaysEvaluate
}

public static class FileTextTreatmentExtensions
{
    extension(FileTextTreatment fileTextTreatment)
    {
        public bool IsConfirmedTextFile() => fileTextTreatment is FileTextTreatment.DeclaredTextFile
                                                    or FileTextTreatment.InferredTextFile;
    }
}
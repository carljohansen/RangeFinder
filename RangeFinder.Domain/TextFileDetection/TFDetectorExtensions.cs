namespace RangeFinder.Domain.TextFileDetection;

public static class TFDetectorExtensions
{
    extension(TextFileDetector detector)
    {
        public void UpdateAndSave(FileDisplay fileDisplay, FileTextTreatment treatment)
        {
            detector.AddCustomEntry(fileDisplay.GetTextFileDetectionKey(), treatment);
            TextFileDetectorRepository.Save(detector);
        }
    }
}
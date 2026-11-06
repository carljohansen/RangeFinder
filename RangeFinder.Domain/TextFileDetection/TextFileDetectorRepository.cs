using RangeFinder.Domain.History;

namespace RangeFinder.Domain.TextFileDetection;

public static class TextFileDetectorRepository
{
    public static string GetTextFileDetectorFilePath()
    {
        return UserSearchRepository.GetDtoFileName("textfiletypes");
    }

    public static void Save() => Save(TextFileDetector.Instance);

    public static void Save(TextFileDetector textFileDetector)
    {
        // TODO: TextFileDetector.IsDirty.  Otherwise this is very wasteful.
        var fileName = GetTextFileDetectorFilePath();

        var dto = textFileDetector.ToDto();
        var json = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(fileName, json);
    }

    public static void Populate(TextFileDetector textFileDetector)
    {
        var fileName = GetTextFileDetectorFilePath();
        if (!File.Exists(fileName))
        {
            return;
        }

        var json = File.ReadAllText(fileName);
        var dto = System.Text.Json.JsonSerializer.Deserialize<TextFileDetectorDto>(json);
        if (dto == null)
        {
            return;
        }

        (List<string>, FileTextTreatment)[] keyResultPairs =
        [
            (dto.KnownTextFileKeys, FileTextTreatment.DeclaredTextFile),
            (dto.KnownNonTextFileKeys, FileTextTreatment.DeclaredNonTextFile),
            (dto.InferredTextFileKeys, FileTextTreatment.InferredTextFile),
            (dto.InferredNonTextFileKeys, FileTextTreatment.InferredNonTextFile),
            (dto.AlwaysEvaluateKeys, FileTextTreatment.AlwaysEvaluate)
        ];

        foreach (var (keys, fileTextTreatment) in keyResultPairs)
        {
            textFileDetector.AddRange(keys.Select(TextFileDetectionKey.FromSerializedString), fileTextTreatment);
        }
    }
}
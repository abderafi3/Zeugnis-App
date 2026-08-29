namespace ZeugnisApp.Application.Import;

/// <summary>
/// One row shown on the wizard's preview/correction step (plan §6, step 3). Values are keyed by
/// <see cref="ImportField.Key"/> and stay editable; <see cref="Errors"/> is recomputed after each edit.
/// </summary>
public sealed class ImportRow(int sourceRowNumber, IReadOnlyDictionary<string, string> values)
{
    /// <summary>1-based row number in the source file, for messages ("Zeile 7").</summary>
    public int SourceRowNumber { get; } = sourceRowNumber;

    public Dictionary<string, string> Values { get; } = new(values);

    public List<string> Errors { get; } = [];

    /// <summary>Whether the teacher wants this row committed. Auto-set to false for invalid rows.</summary>
    public bool Included { get; set; } = true;

    public bool IsValid => Errors.Count == 0;

    public string Value(string fieldKey) => Values.GetValueOrDefault(fieldKey, string.Empty);

    public void Revalidate(ImportProfile profile)
    {
        Errors.Clear();
        foreach (var field in profile.Fields)
        {
            var value = Value(field.Key).Trim();

            if (field.Required && value.Length == 0)
            {
                Errors.Add($"{field.DisplayName}: Pflichtangabe fehlt");
                continue;
            }

            if (value.Length > 0 && field.Looks is not null && !field.Looks(value))
            {
                Errors.Add($"{field.DisplayName}: „{value}“ nicht erkennbar");
            }
        }

        if (!IsValid)
        {
            Included = false;
        }
    }
}

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ZeugnisApp.Reporting.Ooxml;

/// <summary>
/// Fills content controls (SDT) by tag, replacing only their inner text (plan §5.2). A control
/// routinely holds several runs (spell-check splits, tracked formatting); this collapses them to a
/// single run that carries the <em>first</em> original run's <c>rPr</c>, so font/size/weight are
/// preserved. Nothing outside the control is touched. Proven by Spike A.
/// </summary>
public static class ContentControlFiller
{
    /// <summary>
    /// Replaces the text of every SDT under <paramref name="root"/> whose tag is a key in
    /// <paramref name="valuesByTag"/>. Returns the tags that were actually found and filled.
    /// </summary>
    public static IReadOnlyCollection<string> Fill(OpenXmlElement root, IReadOnlyDictionary<string, string> valuesByTag)
    {
        var filled = new List<string>();

        foreach (var sdt in root.Descendants<SdtElement>().ToList())
        {
            var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
            if (tag is null || !valuesByTag.TryGetValue(tag, out var newText))
            {
                continue;
            }

            if (TryFillOne(sdt, newText ?? string.Empty))
            {
                filled.Add(tag);
            }
        }

        return filled;
    }

    private static bool TryFillOne(SdtElement sdt, string text)
    {
        var runContainer = (OpenXmlElement?)sdt.Descendants<SdtContentRun>().FirstOrDefault();
        if (runContainer is not null)
        {
            return FillRunContainer(runContainer, text);
        }

        var blockContainer = sdt.Descendants<SdtContentBlock>().FirstOrDefault()
                             ?? (OpenXmlElement?)sdt.Descendants<SdtContentCell>().FirstOrDefault();
        return blockContainer is not null && FillBlockContainer(blockContainer, text);
    }

    /// <summary>Inline SDT: collapse to one run with the first run's properties.</summary>
    private static bool FillRunContainer(OpenXmlElement container, string text)
    {
        var keptProps = container.Descendants<Run>().FirstOrDefault()?.RunProperties?.CloneNode(true) as RunProperties;
        container.RemoveAllChildren<Run>();
        container.AppendChild(BuildRun(text, keptProps));
        return true;
    }

    /// <summary>
    /// Block / cell SDT: keep the first paragraph (and its pPr), replace its runs with one run,
    /// drop any further paragraphs. Newlines in <paramref name="text"/> become soft line breaks.
    /// </summary>
    private static bool FillBlockContainer(OpenXmlElement container, string text)
    {
        var paragraphs = container.Elements<Paragraph>().ToList();
        var first = paragraphs.FirstOrDefault();
        if (first is null)
        {
            first = new Paragraph();
            container.AppendChild(first);
        }

        var keptProps = first.Descendants<Run>().FirstOrDefault()?.RunProperties?.CloneNode(true) as RunProperties;

        foreach (var extra in paragraphs.Skip(1))
        {
            extra.Remove();
        }

        first.RemoveAllChildren<Run>();
        first.AppendChild(BuildRun(text, keptProps));
        return true;
    }

    private static Run BuildRun(string text, RunProperties? props)
    {
        var run = new Run();
        if (props is not null)
        {
            run.RunProperties = props;
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                run.AppendChild(new Break());
            }

            run.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }

        return run;
    }
}

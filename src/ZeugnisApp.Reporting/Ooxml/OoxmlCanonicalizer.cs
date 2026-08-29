using System.Security.Cryptography;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace ZeugnisApp.Reporting.Ooxml;

/// <summary>Result of comparing two <c>.docx</c> for structural equality (plan §5.4).</summary>
public sealed record StructuralComparison(IReadOnlyList<string> Differences)
{
    public bool Identical => Differences.Count == 0;
}

/// <summary>
/// Canonical-XML comparison of everything <em>outside</em> the content controls (plan §5.4).
/// The OpenXML SDK re-serialises the whole package on save, so a byte/naive hash gives false
/// failures; instead each relevant part is parsed, attributes are sorted, insignificant whitespace
/// is dropped, the inside of every <c>&lt;w:sdtContent&gt;</c> is masked, and the trees are compared.
/// Media parts are compared by a content hash inventory. This is the method the Sprint 6 regression
/// suite and the Sprint 12 full-fidelity suite both use.
/// </summary>
public static class OoxmlCanonicalizer
{
    private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XName SdtContent = XName.Get("sdtContent", W);

    public static StructuralComparison CompareStructure(string pristinePath, string filledPath)
    {
        var diffs = new List<string>();

        using var pristine = WordprocessingDocument.Open(pristinePath, isEditable: false);
        using var filled = WordprocessingDocument.Open(filledPath, isEditable: false);

        ComparePart(diffs, "word/document.xml",
            MainXml(pristine), MainXml(filled), maskSdt: true);

        ComparePart(diffs, "word/styles.xml",
            pristine.MainDocumentPart?.StyleDefinitionsPart?.Styles?.OuterXml,
            filled.MainDocumentPart?.StyleDefinitionsPart?.Styles?.OuterXml, maskSdt: false);

        ComparePart(diffs, "word/numbering.xml",
            pristine.MainDocumentPart?.NumberingDefinitionsPart?.Numbering?.OuterXml,
            filled.MainDocumentPart?.NumberingDefinitionsPart?.Numbering?.OuterXml, maskSdt: false);

        ComparePart(diffs, "word/settings.xml",
            pristine.MainDocumentPart?.DocumentSettingsPart?.Settings?.OuterXml,
            filled.MainDocumentPart?.DocumentSettingsPart?.Settings?.OuterXml, maskSdt: false);

        ComparePart(diffs, "word/theme",
            pristine.MainDocumentPart?.ThemePart?.Theme?.OuterXml,
            filled.MainDocumentPart?.ThemePart?.Theme?.OuterXml, maskSdt: false);

        CompareHeaderFooter(diffs, "header",
            pristine.MainDocumentPart?.HeaderParts, filled.MainDocumentPart?.HeaderParts);
        CompareHeaderFooter(diffs, "footer",
            pristine.MainDocumentPart?.FooterParts, filled.MainDocumentPart?.FooterParts);

        CompareMediaInventory(diffs, pristine, filled);

        return new StructuralComparison(diffs);
    }

    /// <summary>Canonical form of an OOXML fragment — for asserting the *values* that were written.</summary>
    public static XElement Canonicalize(string xml, bool maskSdtContents)
    {
        var root = XElement.Parse(xml, LoadOptions.None);
        if (maskSdtContents)
        {
            foreach (var node in root.DescendantsAndSelf(SdtContent).ToList())
            {
                node.RemoveNodes();
                node.Add(new XElement(XName.Get("masked", "urn:zeugnisapp")));
            }
        }

        return Normalize(root);
    }

    private static string? MainXml(WordprocessingDocument doc) => doc.MainDocumentPart?.Document.OuterXml;

    private static void ComparePart(List<string> diffs, string name, string? a, string? b, bool maskSdt)
    {
        if (a is null && b is null)
        {
            return;
        }

        if (a is null || b is null)
        {
            diffs.Add($"{name}: present in only one document");
            return;
        }

        if (!XNode.DeepEquals(Canonicalize(a, maskSdt), Canonicalize(b, maskSdt)))
        {
            diffs.Add($"{name}: structure changed outside the content controls");
        }
    }

    private static void CompareHeaderFooter(List<string> diffs, string kind,
        IEnumerable<HeaderPart>? aHeaders, IEnumerable<HeaderPart>? bHeaders)
    {
        var a = (aHeaders ?? []).Select(p => p.Header.OuterXml).ToList();
        var b = (bHeaders ?? []).Select(p => p.Header.OuterXml).ToList();
        CompareXmlList(diffs, kind, a, b);
    }

    private static void CompareHeaderFooter(List<string> diffs, string kind,
        IEnumerable<FooterPart>? aFooters, IEnumerable<FooterPart>? bFooters)
    {
        var a = (aFooters ?? []).Select(p => p.Footer.OuterXml).ToList();
        var b = (bFooters ?? []).Select(p => p.Footer.OuterXml).ToList();
        CompareXmlList(diffs, kind, a, b);
    }

    private static void CompareXmlList(List<string> diffs, string kind, List<string> a, List<string> b)
    {
        if (a.Count != b.Count)
        {
            diffs.Add($"{kind}: part count changed ({a.Count} → {b.Count})");
            return;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!XNode.DeepEquals(Canonicalize(a[i], maskSdtContents: true), Canonicalize(b[i], maskSdtContents: true)))
            {
                diffs.Add($"{kind}[{i}]: structure changed outside the content controls");
            }
        }
    }

    private static void CompareMediaInventory(List<string> diffs, WordprocessingDocument a, WordprocessingDocument b)
    {
        var inventoryA = MediaHashes(a);
        var inventoryB = MediaHashes(b);

        foreach (var (uri, hash) in inventoryA)
        {
            if (!inventoryB.TryGetValue(uri, out var otherHash))
            {
                diffs.Add($"media {uri}: removed");
            }
            else if (otherHash != hash)
            {
                diffs.Add($"media {uri}: content changed");
            }
        }

        foreach (var uri in inventoryB.Keys.Where(k => !inventoryA.ContainsKey(k)))
        {
            diffs.Add($"media {uri}: added");
        }
    }

    private static Dictionary<string, string> MediaHashes(WordprocessingDocument doc)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in doc.MainDocumentPart?.ImageParts ?? [])
        {
            using var stream = part.GetStream();
            result[part.Uri.ToString()] = Convert.ToHexString(SHA256.HashData(stream));
        }

        return result;
    }

    private static XElement Normalize(XElement element)
    {
        var normalized = new XElement(element.Name,
            element.Attributes()
                .Where(attr => !attr.IsNamespaceDeclaration)
                .OrderBy(attr => attr.Name.NamespaceName, StringComparer.Ordinal)
                .ThenBy(attr => attr.Name.LocalName, StringComparer.Ordinal)
                .Select(attr => new XAttribute(attr.Name, attr.Value)));

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XElement child:
                    normalized.Add(Normalize(child));
                    break;
                case XText text when !string.IsNullOrWhiteSpace(text.Value):
                    normalized.Add(new XText(text.Value.Trim()));
                    break;
            }
        }

        return normalized;
    }
}

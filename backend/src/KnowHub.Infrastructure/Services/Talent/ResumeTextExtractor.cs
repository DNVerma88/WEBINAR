using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace KnowHub.Infrastructure.Services.Talent;

public class ResumeTextExtractor
{
    /// <summary>
    /// Validates file magic bytes against the declared extension.
    /// Must be called before processing; resets stream position to 0.
    /// </summary>
    public static bool IsValidFileType(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        byte[] header = new byte[8];
        int read = stream.Read(header, 0, 8);
        stream.Position = 0;
        if (read < 4) return false;

        return ext switch
        {
            // %PDF
            ".pdf"  => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            // ZIP/OOXML (DOCX)
            ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _       => false
        };
    }

    public static string ExtractText(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var text = ext switch
        {
            ".pdf"  => ExtractFromPdf(stream),
            ".docx" => ExtractFromDocx(stream),
            _ => throw new InvalidOperationException($"Unsupported file type: {ext}")
        };
        // PostgreSQL cannot store null bytes in text columns (Postgres error 22021 / "invalid byte
        // sequence for encoding UTF8: 0x00").  PDF and DOCX extraction can surface embedded null
        // bytes from binary font data or DOCX internal markup, so we strip them here at the single
        // extraction exit point rather than at every call site.
        return text.Replace("\0", string.Empty);
    }

    private static string ExtractFromPdf(Stream stream)
    {
        using var doc = PdfDocument.Open(stream);
        var sb = new System.Text.StringBuilder();
        foreach (var page in doc.GetPages())
        {
            var words = page.GetWords(NearestNeighbourWordExtractor.Instance).ToList();
            if (words.Count == 0) { sb.AppendLine(page.Text); continue; }

            double? colBound = FindPdfColumnBoundary(words, page.Width);
            List<string> pageLines;
            if (colBound.HasValue)
            {
                var left  = words.Where(w => w.BoundingBox.Centroid.X < colBound.Value)
                                 .OrderByDescending(w => w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left).ToList();
                var right = words.Where(w => w.BoundingBox.Centroid.X >= colBound.Value)
                                 .OrderByDescending(w => w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left).ToList();
                pageLines = [.. PdfWordsToLines(left), .. PdfWordsToLines(right)];
            }
            else
            {
                var sorted = words.OrderByDescending(w => w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left).ToList();
                pageLines = PdfWordsToLines(sorted);
            }
            foreach (var line in pageLines)
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine(line);
        }
        return sb.ToString();
    }

    private static double? FindPdfColumnBoundary(List<UglyToad.PdfPig.Content.Word> words, double pageWidth)
    {
        var xEdges = words
            .Select(w => w.BoundingBox.Left)
            .Where(x => x >= pageWidth * 0.30 && x <= pageWidth * 0.70)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (xEdges.Count < 2) return null;
        double maxGap = 0, splitAt = 0;
        for (int i = 1; i < xEdges.Count; i++)
        {
            var gap = xEdges[i] - xEdges[i - 1];
            if (gap > maxGap) { maxGap = gap; splitAt = (xEdges[i] + xEdges[i - 1]) / 2; }
        }
        return maxGap >= 15 ? splitAt : null;
    }

    private static List<string> PdfWordsToLines(List<UglyToad.PdfPig.Content.Word> words)
    {
        const double lineTol = 4.0;
        var result = new List<string>();
        if (words.Count == 0) return result;

        var curLine = new List<UglyToad.PdfPig.Content.Word> { words[0] };
        double curY = words[0].BoundingBox.Top;
        for (int i = 1; i < words.Count; i++)
        {
            if (Math.Abs(words[i].BoundingBox.Top - curY) <= lineTol)
                curLine.Add(words[i]);
            else
            {
                var t = string.Join(" ", curLine.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)).Trim();
                if (!string.IsNullOrWhiteSpace(t)) result.Add(t);
                curLine = [words[i]];
                curY = words[i].BoundingBox.Top;
            }
        }
        var last = string.Join(" ", curLine.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)).Trim();
        if (!string.IsNullOrWhiteSpace(last)) result.Add(last);
        return result;
    }

    private static string ExtractFromDocx(Stream stream)
    {
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body is null) return "";

        // Walk top-level body elements in order.
        // For plain paragraphs: emit the text directly.
        // For tables (common in two-column resume templates): collect ALL content from
        // each column first, then append column-by-column so that section headings
        // in one column stay together with their bullet points instead of being
        // interleaved row-by-row with the adjacent column.
        // Using explicit Text (w:t) descendants avoids picking up field-instruction
        // text (w:instrText), bookmark GUIDs, or other non-display XML nodes that
        // InnerText would include and that produced the garbled number artifacts.
        var lines = new List<string>();

        foreach (var element in body.Elements())
        {
            switch (element)
            {
                case Paragraph para:
                {
                    // Include w:tab as a delimiter so that two-column heading paragraphs
                    // like "EDUCATION <tab> PROFESSIONAL EXPERIENCE" become two separate lines.
                    var raw = ParaToRaw(para);
                    foreach (var part in raw.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        if (!string.IsNullOrWhiteSpace(part)) lines.Add(part);
                    break;
                }

                case Table table:
                {
                    // Gather each column's lines independently, then append column-by-column.
                    // This keeps a resume's left-column skills section together and the
                    // right-column work-experience section together rather than interleaving them.
                    var cols = new SortedDictionary<int, List<string>>();
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>().ToList();
                        for (int c = 0; c < cells.Count; c++)
                        {
                            if (!cols.ContainsKey(c)) cols[c] = [];
                            foreach (var p in cells[c].Descendants<Paragraph>())
                            {
                                var raw = ParaToRaw(p);
                                foreach (var part in raw.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                    if (!string.IsNullOrWhiteSpace(part)) cols[c].Add(part);
                            }
                        }
                    }
                    foreach (var col in cols.Values)
                        lines.AddRange(col);
                    break;
                }
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Extracts visible text from a paragraph, treating &lt;w:tab&gt; as a tab character.
    /// Only picks up &lt;w:t&gt; (visible text runs) — excludes field instructions,
    /// bookmark IDs, and other non-display XML that InnerText would include.
    /// </summary>
    private static string ParaToRaw(Paragraph para)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var node in para.Descendants())
        {
            if (node is Text t) sb.Append(t.Text);
            else if (node is TabChar) sb.Append('\t');
        }
        return sb.ToString();
    }
}

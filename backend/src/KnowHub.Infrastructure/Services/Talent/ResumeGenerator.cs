using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OxmlPkg  = DocumentFormat.OpenXml.Packaging;
using OxmlWord = DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using KnowHub.Application.Contracts.Talent;
using System.Text.Json;

namespace KnowHub.Infrastructure.Services.Talent;

/// <summary>
/// Generates PDF and DOCX resumes in the following layout (matching the sample):
///
/// ┌---------------------------------------------------------┐
/// │  NAME (large, underlined, navy)     Email:  xxx         │
/// │  Headline (grey)                    Phone:  xxx         │
/// ├---------------------------------------------------------┤
/// │                  SUMMARY (centered)                     │
/// │  [body text full width]                                 │
/// ├----------------------┬----------------------------------┤
/// │  EDUCATION           │  PROFESSIONAL EXPERIENCE        │
/// │  SKILLS              │  Company (bold)                  │
/// │  CERTIFICATIONS      │    Role | Date                   │
/// │                      │    \u2022 bullets                     │
/// ├----------------------┴----------------------------------┤
/// │  PROJECT DETAILS                                        │
/// │  ○ Company                                              │
/// │    \u2022 Project (bold)                                     │
/// │      Tech stack                                         │
/// │      - bullet                                           │
/// └---------------------------------------------------------┘
/// </summary>
public class ResumeGenerator
{
    // -- Colour palette -----------------------------------------------------
    private const string NavyHex   = "1F2D4E";   // name + headings
    private const string GreyHex   = "666666";   // subtitle / dates
    private const string RuleHex   = "AAAAAA";   // thin rules
    private const string LinkHex   = "0563C1";   // email hyperlink

    // -- Font sizes (OOXML half-points; 1pt = 2 half-points) ----------------
    private const string HpName    = "36";  // 18pt  \u2013 name
    private const string HpHead    = "22";  // 11pt  \u2013 headline
    private const string HpSection = "22";  // 11pt  \u2013 section headings
    private const string HpRole    = "21";  // 10.5pt\u2013 role/company names
    private const string HpBody    = "20";  // 10pt  \u2013 body text
    private const string HpSmall   = "19";  // 9.5pt \u2013 bullets, detail
    private const string HpDate    = "18";  // 9pt   \u2013 dates, meta

    private const string FontFace  = "Calibri";

    // ----------------------------------------------------------------------
    // PDF generation
    // ----------------------------------------------------------------------
    public byte[] GeneratePdf(ResumeProfileDto profile)
    {
        var p    = D<PersonalInfoDto>(profile.PersonalInfo);
        var exp  = D<List<WorkExperienceDto>>(profile.WorkExperience)  ?? [];
        var edu  = D<List<EducationDto>>(profile.Education)            ?? [];
        var ski  = D<List<SkillDto>>(profile.Skills)                   ?? [];
        var cer  = D<List<CertificationDto>>(profile.Certifications)   ?? [];
        var proj = D<List<ProjectDto>>(profile.Projects)               ?? [];
        var lang = D<List<LanguageDto>>(profile.Languages)             ?? [];
        var pub  = D<List<PublicationDto>>(profile.Publications)       ?? [];
        var ach  = D<List<AchievementDto>>(profile.Achievements)       ?? [];

        return Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Calibri").FontSize(10).FontColor("#222222"));

                page.Content().Column(col =>
                {
                    // -- 1. HEADER -----------------------------------------
                    col.Item().Row(hdr =>
                    {
                        // Left: Name + Headline (stretches to fill)
                        hdr.RelativeItem().Column(c =>
                        {
                            c.Item().Text(p?.FullName ?? "")
                                .FontSize(18).Bold().Underline().FontColor("#1a1a1a");
                            if (!string.IsNullOrWhiteSpace(p?.Headline))
                                c.Item().Text(p.Headline)
                                    .FontSize(11).FontColor($"#{GreyHex}");
                        });

                        // Right: Email / Phone \u2022 auto-sized so it never clips
                        hdr.AutoItem().Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(p?.Email))
                                c.Item().Text(t =>
                                {
                                    t.Span("Email:  ").FontSize(10);
                                    t.Span(p.Email).FontSize(10).FontColor($"#{LinkHex}");
                                });
                            if (!string.IsNullOrWhiteSpace(p?.Phone))
                                c.Item().Text($"Phone:  {p.Phone}").FontSize(10);
                        });
                    });

                    // Full-width dark rule after header
                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#3a3a3a");

                    // -- 2. SUMMARY (full width, centred heading) ---------
                    if (!string.IsNullOrWhiteSpace(profile.Summary))
                    {
                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.AlignCenter();
                            t.Span("SUMMARY").Bold().FontSize(11).FontColor("#1a1a1a");
                        });
                        col.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor($"#{RuleHex}");
                        col.Item().PaddingTop(3).PaddingBottom(4).Text(t =>
                        {
                            t.Justify();
                            t.Span(profile.Summary).FontSize(9.5f);
                        });
                        col.Item().LineHorizontal(0.5f).LineColor($"#{RuleHex}");
                        col.Item().PaddingBottom(3);
                    }

                    // -- 3. TWO-COLUMN BODY --------------------------------
                    col.Item().Row(body =>
                    {
                        // LEFT column ≈ 34%
                        body.RelativeItem(34).Column(lc =>
                        {
                            PdfLeftColumn(lc, edu, ski, cer, lang, ach);
                        });

                        // Vertical separator
                        body.ConstantItem(1).Background($"#{RuleHex}");
                        body.ConstantItem(8);

                        // RIGHT column ≈ 66%
                        body.RelativeItem(66).Column(rc =>
                        {
                            PdfExperienceColumn(rc, exp);
                        });
                    });
                    // Publications -- full-width on page 1, after two-column section
                    if (pub.Count > 0)
                    {
                        col.Item().PaddingTop(8);
                        PdfHeading(col, "PUBLICATIONS");
                        foreach (var item in pub)
                        {
                            col.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span(item.Title ?? "").Bold().FontSize(10f);
                                if (!string.IsNullOrWhiteSpace(item.Journal))
                                    t.Span($",  {item.Journal}").FontSize(9.5f).FontColor($"#{GreyHex}");
                                if (!string.IsNullOrWhiteSpace(item.Year))
                                    t.Span($"  ({item.Year})").FontSize(9).Italic().FontColor($"#{GreyHex}");
                            });
                            if (!string.IsNullOrWhiteSpace(item.Url))
                                col.Item().PaddingLeft(8).Text(item.Url).FontSize(9).FontColor($"#{LinkHex}");
                        }
                    }

                    // Strict page break -- Projects always start on page 2
                    if (proj.Count > 0)
                    {
                        col.Item().PageBreak();
                        PdfHeading(col, "PROJECT DETAILS");
                        PdfProjectDetails(col, proj);
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.CurrentPageNumber().FontSize(8).FontColor($"#{GreyHex}");
                    t.Span(" / ").FontSize(8).FontColor($"#{GreyHex}");
                    t.TotalPages().FontSize(8).FontColor($"#{GreyHex}");
                });
            });
        }).GeneratePdf();
    }

    // -- PDF helpers ---------------------------------------------------------

    private static void PdfHeading(ColumnDescriptor col, string title)
    {
        col.Item().Text(title).Bold().FontSize(11).FontColor($"#{NavyHex}");
        col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor($"#{RuleHex}");
    }

    private static void PdfLeftColumn(ColumnDescriptor col,
        List<EducationDto>     edu,
        List<SkillDto>         ski,
        List<CertificationDto> cer,
        List<LanguageDto>      lang,
        List<AchievementDto>   ach)
    {
        if (edu.Count > 0)
        {
            PdfHeading(col, "EDUCATION");
            foreach (var e in edu)
            {
                col.Item().PaddingTop(3).Text(e.Institution ?? "").Bold().FontSize(10.5f);
                col.Item().Text(e.Degree ?? "").FontSize(9.5f);
                col.Item().PaddingBottom(3)
                    .Text($"{e.StartYear} \u2022 {e.EndYear ?? "Present"}")
                    .FontSize(9).FontColor($"#{GreyHex}");
            }
        }

        if (ski.Count > 0)
        {
            col.Item().PaddingTop(6);
            PdfHeading(col, "SKILLS");
            foreach (var s in ski)
                col.Item().Text($"\u2022 {s.Name}").FontSize(9.5f);
        }

        if (cer.Count > 0)
        {
            col.Item().PaddingTop(6);
            PdfHeading(col, "CERTIFICATIONS /\nTRAININGS");
            foreach (var c in cer)
            {
                col.Item().PaddingTop(2).Text(t =>
                {
                    t.Span("\u2022 ").FontSize(9.5f);
                    t.Span(c.Name ?? "").FontSize(9.5f);
                });
                if (!string.IsNullOrWhiteSpace(c.Issuer))
                    col.Item().PaddingLeft(10).Text(c.Issuer).FontSize(9).FontColor($"#{GreyHex}");
            }
        }

        if (lang.Count > 0)
        {
            col.Item().PaddingTop(6);
            PdfHeading(col, "LANGUAGES");
            foreach (var l in lang)
                col.Item().Text($"\u2022 {l.Name}{(string.IsNullOrWhiteSpace(l.Proficiency) ? "" : $" ({l.Proficiency})")}").FontSize(9.5f);
        }

        if (ach.Count > 0)
        {
            col.Item().PaddingTop(6);
            PdfHeading(col, "ACHIEVEMENTS");
            foreach (var item in ach)
            {
                col.Item().PaddingTop(2).Text(t =>
                {
                    t.Span(item.Title ?? "").Bold().FontSize(9.5f);
                    if (!string.IsNullOrWhiteSpace(item.Year))
                        t.Span($"  ({item.Year})").FontSize(8.5f).Italic().FontColor($"#{GreyHex}");
                });
                if (!string.IsNullOrWhiteSpace(item.Description))
                    foreach (var line in Lines(item.Description))
                        col.Item().PaddingLeft(6).Text($"\u2022 {line}").FontSize(9f);
            }
        }
    }

    private static void PdfExperienceColumn(ColumnDescriptor col, List<WorkExperienceDto> exp)
    {
        if (exp.Count == 0) return;

        PdfHeading(col, "PROFESSIONAL EXPERIENCE");

        // Group by company: company header first, then indented roles
        foreach (var grp in exp.GroupBy(e => e.Company ?? ""))
        {
            // Company name (bold, navy)
            col.Item().PaddingTop(6).Text(grp.Key).Bold().FontSize(11).FontColor($"#{NavyHex}");

            foreach (var role in grp)
            {
                // Indented role title + date on same line
                col.Item().PaddingLeft(12).PaddingTop(2).Text(t =>
                {
                    t.Span(role.JobTitle ?? "").Bold().FontSize(10);
                    if (!string.IsNullOrWhiteSpace(role.StartDate))
                        t.Span($"  |  {role.StartDate} \u2022 {role.EndDate ?? "Present"}")
                            .FontSize(9).FontColor($"#{GreyHex}");
                });

                if (!string.IsNullOrWhiteSpace(role.Description))
                    foreach (var line in Lines(role.Description))
                        col.Item().PaddingLeft(22).PaddingTop(1).Text($"\u2022 {line}").FontSize(9.5f);
            }
        }
    }

    private static void PdfProjectDetails(ColumnDescriptor col, List<ProjectDto> proj)
    {
        // Group by Company; show ○ company header, then ● project
        foreach (var grp in proj.GroupBy(p => p.Company ?? ""))
        {
            if (!string.IsNullOrWhiteSpace(grp.Key))
                col.Item().PaddingTop(4).Text($"o  {grp.Key}").Bold().FontSize(10.5f);

            foreach (var p in grp)
            {
                col.Item().PaddingTop(2).PaddingLeft(grp.Key != "" ? 12 : 0).Text(t =>
                {
                    t.Span("\u2022 ").FontSize(10);
                    t.Span(p.Name ?? "").Bold().FontSize(10).FontColor($"#{NavyHex}");
                });
                if (!string.IsNullOrWhiteSpace(p.Technologies))
                    col.Item().PaddingLeft(grp.Key != "" ? 22 : 10)
                        .Text(p.Technologies).FontSize(9.5f).Italic().FontColor($"#{GreyHex}");
                if (!string.IsNullOrWhiteSpace(p.Description))
                    foreach (var line in Lines(p.Description))
                        col.Item().PaddingLeft(grp.Key != "" ? 28 : 16)
                            .Text($"\u2022 {line}").FontSize(9.5f);
                if (!string.IsNullOrWhiteSpace(p.Url))
                    col.Item().PaddingLeft(grp.Key != "" ? 22 : 10)
                        .Text(p.Url).FontSize(9).FontColor($"#{LinkHex}");
            }
        }
    }

    // ----------------------------------------------------------------------
    // DOCX generation
    // ----------------------------------------------------------------------
    public byte[] GenerateWord(ResumeProfileDto profile)
    {
        var p    = D<PersonalInfoDto>(profile.PersonalInfo);
        var exp  = D<List<WorkExperienceDto>>(profile.WorkExperience)  ?? [];
        var edu  = D<List<EducationDto>>(profile.Education)            ?? [];
        var ski  = D<List<SkillDto>>(profile.Skills)                   ?? [];
        var cer  = D<List<CertificationDto>>(profile.Certifications)   ?? [];
        var proj = D<List<ProjectDto>>(profile.Projects)               ?? [];
        var lang = D<List<LanguageDto>>(profile.Languages)             ?? [];
        var pub  = D<List<PublicationDto>>(profile.Publications)       ?? [];
        var ach  = D<List<AchievementDto>>(profile.Achievements)       ?? [];

        using var ms = new MemoryStream();
        using (var doc = OxmlPkg.WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new OxmlWord.Document();
            var body = main.Document.AppendChild(new OxmlWord.Body());

            // inject document defaults: Calibri 10pt
            var sp = main.AddNewPart<OxmlPkg.StyleDefinitionsPart>();
            sp.Styles = new OxmlWord.Styles(
                new OxmlWord.DocDefaults(
                    new OxmlWord.RunPropertiesDefault(
                        new OxmlWord.RunPropertiesBaseStyle(
                            WFont(FontFace), WFontSize(HpBody)))));

            // -- 1. HEADER table (Name+Headline left | Email+Phone right) --
            // Header table: 2 columns \u2014 name (5620 twips) + contact (4126 twips) = 9746 total
            var hdrTbl = WordTable(body, 9746, 5620, 4126);
            var hdrRow = hdrTbl.AppendChild(new OxmlWord.TableRow());

            // Name cell
            var nameCell = WordCell(hdrRow, 5620);
            WordPara(nameCell, p?.FullName ?? "", fontSize: HpName, bold: true,
                underline: true, colour: "1a1a1a");
            if (!string.IsNullOrWhiteSpace(p?.Headline))
                WordPara(nameCell, p.Headline, fontSize: HpHead, colour: GreyHex);

            // Contact cell
            var contactCell = WordCell(hdrRow, 4126);
            if (!string.IsNullOrWhiteSpace(p?.Email))
            {
                var ep = contactCell.AppendChild(WordEmptyPara(right: true));
                WordRunInPara(ep, "Email:  ", HpBody);
                WordRunInPara(ep, p.Email, HpBody, colour: LinkHex);
            }
            if (!string.IsNullOrWhiteSpace(p?.Phone))
                WordPara(contactCell, $"Phone:  {p.Phone}", fontSize: HpBody, right: true);

            // Full-width navy rule after header
            WordHRule(body, NavyHex, size: 18);

            // -- 2. SUMMARY (full width) ---------------------------------
            if (!string.IsNullOrWhiteSpace(profile.Summary))
            {
                WordSectionHeading(body, "SUMMARY", centred: true);
                WordBodyPara(body, profile.Summary);
                WordHRule(body, RuleHex, size: 6);
            }

            // -- 3. TWO-COLUMN table --------------------------------------
            // Left ~34% = ~3314 twips, Right ~66% = ~6432 twips (A4 usable = 9746)
            // Main two-column table: left ~34% (3314 twips) + right ~66% (6432 twips) = 9746 total
            var mainTbl = WordTable(body, 9746, 3314, 6432);
            var mainRow = mainTbl.AppendChild(new OxmlWord.TableRow());

            var leftCell  = WordCell(mainRow, 3314, rightBorder: true);
            var rightCell = WordCell(mainRow, 6432);

            // LEFT: Education -> Skills -> Certifications -> Languages -> Achievements
            WordFillLeft(leftCell, edu, ski, cer, lang, ach);

            // RIGHT: Professional Experience
            WordFillRight(rightCell, exp);

            // Publications -- full-width on page 1, after two-column section
            if (pub.Count > 0)
            {
                WordSectionHeading(body, "PUBLICATIONS");
                foreach (var item in pub)
                {
                    var pp = body.AppendChild(WordEmptyPara());
                    WordRunInPara(pp, item.Title ?? "", HpRole, bold: true);
                    if (!string.IsNullOrWhiteSpace(item.Journal))
                        WordRunInPara(pp, $",  {item.Journal}", HpBody, colour: GreyHex);
                    if (!string.IsNullOrWhiteSpace(item.Year))
                        WordRunInPara(pp, $"  ({item.Year})", HpDate, italic: true, colour: GreyHex);
                    if (!string.IsNullOrWhiteSpace(item.Url))
                        WordBodyPara(body, item.Url);
                }
            }

            // Strict page break -- Projects always start on page 2
            if (proj.Count > 0)
            {
                body.AppendChild(new OxmlWord.Paragraph(
                    new OxmlWord.ParagraphProperties(
                        new OxmlWord.PageBreakBefore()),
                    WordRun("", HpBody)));
                WordSectionHeading(body, "PROJECT DETAILS");
                WordProjectDetails(body, proj);
            }

            // page margins
            body.AppendChild(new OxmlWord.SectionProperties(
                new OxmlWord.PageSize { Width = 11906, Height = 16838 },  // A4
                new OxmlWord.PageMargin { Top = 864, Bottom = 864, Left = 864, Right = 864 }));

            main.Document.Save();
        }

        // Run OpenXmlValidator and log every schema violation to stderr (Docker logs).
        // This is non-blocking: the document is always returned regardless.
        try
        {
            ms.Position = 0;
            using var vDoc = OxmlPkg.WordprocessingDocument.Open(ms, false);
            var errors = new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(vDoc).ToList();
            if (errors.Count > 0)
            {
                Console.Error.WriteLine($"[DOCX-VALIDATE] {errors.Count} schema violation(s) found:");
                foreach (var e in errors)
                    Console.Error.WriteLine($"  [{e.ErrorType}] {e.Description} | path: {e.Path?.XPath}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DOCX-VALIDATE-EX] {ex.Message}");
        }

        return ms.ToArray();
    }

    // -- DOCX left-column content --------------------------------------------
    private static void WordFillLeft(OxmlWord.TableCell cell,
        List<EducationDto>     edu,
        List<SkillDto>         ski,
        List<CertificationDto> cer,
        List<LanguageDto>      lang,
        List<AchievementDto>   ach)
    {
        if (edu.Count > 0)
        {
            WordCellHeading(cell, "EDUCATION");
            foreach (var e in edu)
            {
                WordCellPara(cell, e.Institution ?? "", fontSize: HpRole, bold: true);
                WordCellPara(cell, e.Degree ?? "", fontSize: HpBody);
                WordCellPara(cell, $"{e.StartYear} \u2022 {e.EndYear ?? "Present"}", fontSize: HpDate, colour: GreyHex);
                WordCellSpace(cell);
            }
        }

        if (ski.Count > 0)
        {
            WordCellSpace(cell);
            WordCellHeading(cell, "SKILLS");
            foreach (var s in ski)
                WordCellPara(cell, $"\u2022 {s.Name}", fontSize: HpSmall);
        }

        if (cer.Count > 0)
        {
            WordCellSpace(cell);
            WordCellHeading(cell, "CERTIFICATIONS /\nTRAININGS");
            foreach (var c in cer)
            {
                WordCellPara(cell, $"\u2022 {c.Name ?? ""}", fontSize: HpSmall, bold: true);
                if (!string.IsNullOrWhiteSpace(c.Issuer))
                    WordCellPara(cell, $"   {c.Issuer}", fontSize: HpDate, colour: GreyHex);
            }
        }

        if (lang.Count > 0)
        {
            WordCellSpace(cell);
            WordCellHeading(cell, "LANGUAGES");
            foreach (var l in lang)
                WordCellPara(cell, $"\u2022 {l.Name}{(string.IsNullOrWhiteSpace(l.Proficiency) ? "" : $" ({l.Proficiency})")}", fontSize: HpSmall);
        }

        if (ach.Count > 0)
        {
            WordCellSpace(cell);
            WordCellHeading(cell, "ACHIEVEMENTS");
            foreach (var item in ach)
            {
                var ap = cell.AppendChild(new OxmlWord.Paragraph());
                ap.AppendChild(new OxmlWord.ParagraphProperties(
                    new OxmlWord.SpacingBetweenLines { Before = "40", After = "20" }));
                ap.AppendChild(WordRun(item.Title ?? "", HpSmall, bold: true));
                if (!string.IsNullOrWhiteSpace(item.Year))
                    ap.AppendChild(WordRun($"  ({item.Year})", HpDate, italic: true, colour: GreyHex));
                if (!string.IsNullOrWhiteSpace(item.Description))
                    foreach (var line in Lines(item.Description))
                        WordCellPara(cell, $"\u2022 {line}", fontSize: HpSmall);
            }
        }
    }

    // -- DOCX right-column (experience) -------------------------------------
    private static void WordFillRight(OxmlWord.TableCell cell, List<WorkExperienceDto> exp)
    {
        if (exp.Count == 0) return;
        WordCellHeading(cell, "PROFESSIONAL EXPERIENCE");

        // Group by company: company header first, then indented roles
        foreach (var grp in exp.GroupBy(e => e.Company ?? ""))
        {
            // Company name (bold, navy)
            WordCellSpace(cell);
            WordCellPara(cell, grp.Key, fontSize: HpRole, bold: true, colour: NavyHex, spaceBefore: "60");

            foreach (var role in grp)
            {
                // Indented role title + date on same line
                var rp = new OxmlWord.Paragraph();
                rp.AppendChild(new OxmlWord.ParagraphProperties(
                    new OxmlWord.SpacingBetweenLines { Before = "40", After = "20" },   // spacing pos 20 before ind pos 21
                    new OxmlWord.Indentation { Left = "180" }));
                rp.AppendChild(WordRun(role.JobTitle ?? "", HpBody, bold: true));
                if (!string.IsNullOrWhiteSpace(role.StartDate))
                    rp.AppendChild(WordRun($"  |  {role.StartDate} \u2022 {role.EndDate ?? "Present"}", HpDate, colour: GreyHex));
                cell.AppendChild(rp);

                if (!string.IsNullOrWhiteSpace(role.Description))
                    foreach (var line in Lines(role.Description))
                        WordCellPara(cell, $"\u2022 {line}", fontSize: HpSmall, indent: 360);
            }
        }
    }

    // -- DOCX project details ------------------------------------------------
    private static void WordProjectDetails(OxmlWord.Body body, List<ProjectDto> proj)
    {
        foreach (var grp in proj.GroupBy(p => p.Company ?? ""))
        {
            if (!string.IsNullOrWhiteSpace(grp.Key))
            {
                var cp = body.AppendChild(WordEmptyPara());
                cp.AppendChild(new OxmlWord.ParagraphProperties(
                    new OxmlWord.SpacingBetweenLines { Before = "120", After = "40" }));
                WordRunInPara(cp, $"o  {grp.Key}", HpRole, bold: true);
            }

            foreach (var item in grp)
            {
                // Project name (bold, navy \u2022 no underline)
                var pp = body.AppendChild(WordEmptyPara());
                pp.AppendChild(new OxmlWord.ParagraphProperties(
                    new OxmlWord.SpacingBetweenLines { Before = "80", After = "20" },   // spacing pos 20 before ind pos 21
                    new OxmlWord.Indentation { Left = "360" }));
                WordRunInPara(pp, $"\u2022 {item.Name ?? ""}", HpBody, bold: true, colour: NavyHex);

                // Tech stack (italic, grey)
                if (!string.IsNullOrWhiteSpace(item.Technologies))
                {
                    var tp = body.AppendChild(WordEmptyPara());
                    tp.AppendChild(new OxmlWord.ParagraphProperties(
                        new OxmlWord.SpacingBetweenLines { Before = "0", After = "20" },   // spacing pos 20 before ind pos 21
                        new OxmlWord.Indentation { Left = "540" }));
                    WordRunInPara(tp, item.Technologies, HpSmall, italic: true, colour: GreyHex);
                }

                // Description bullets
                if (!string.IsNullOrWhiteSpace(item.Description))
                    foreach (var line in Lines(item.Description))
                    {
                        var bp = body.AppendChild(WordEmptyPara());
                        bp.AppendChild(new OxmlWord.ParagraphProperties(
                            new OxmlWord.SpacingBetweenLines { Before = "0", After = "20" },   // spacing pos 20 before ind pos 21
                            new OxmlWord.Indentation { Left = "720", Hanging = "180" }));
                        WordRunInPara(bp, $"\u2022 {line}", HpSmall);
                    }
            }
        }
    }

    // ----------------------------------------------------------------------
    // Low-level OOXML helpers
    // ----------------------------------------------------------------------

    private static OxmlWord.Table WordTable(OxmlWord.Body body, uint totalWidthTwips, params uint[] columnWidths)
    {
        var tbl = body.AppendChild(new OxmlWord.Table());
        tbl.AppendChild(new OxmlWord.TableProperties(
            new OxmlWord.TableWidth { Width = totalWidthTwips.ToString(), Type = OxmlWord.TableWidthUnitValues.Dxa },
            // ECMA-376 CT_TblBorders schema sequence: top → left → bottom → right → insideH → insideV
            new OxmlWord.TableBorders(
                new OxmlWord.TopBorder               { Val = OxmlWord.BorderValues.None },
                new OxmlWord.LeftBorder              { Val = OxmlWord.BorderValues.None },
                new OxmlWord.BottomBorder            { Val = OxmlWord.BorderValues.None },
                new OxmlWord.RightBorder             { Val = OxmlWord.BorderValues.None },
                new OxmlWord.InsideHorizontalBorder  { Val = OxmlWord.BorderValues.None },
                new OxmlWord.InsideVerticalBorder    { Val = OxmlWord.BorderValues.None }),
            new OxmlWord.TableCellMarginDefault(
                new OxmlWord.TopMargin    { Width = "0", Type = OxmlWord.TableWidthUnitValues.Dxa },
                new OxmlWord.BottomMargin { Width = "0", Type = OxmlWord.TableWidthUnitValues.Dxa })));
        // w:tblGrid is REQUIRED by OOXML CT_Tbl schema: must appear after tblPr and before any w:tr rows.
        // Without it Word reports "unreadable content" and attempts recovery.
        var tblGrid = tbl.AppendChild(new OxmlWord.TableGrid());
        foreach (var w in columnWidths)
            tblGrid.AppendChild(new OxmlWord.GridColumn { Width = w.ToString() });
        return tbl;
    }

    private static OxmlWord.TableCell WordCell(OxmlWord.TableRow row,
        uint widthTwips, bool rightBorder = false)
    {
        var cell = row.AppendChild(new OxmlWord.TableCell());
        var tcp  = cell.AppendChild(new OxmlWord.TableCellProperties());
        tcp.AppendChild(new OxmlWord.TableCellWidth
            { Width = widthTwips.ToString(), Type = OxmlWord.TableWidthUnitValues.Dxa });
        // OOXML CT_TcPr schema: tcBdr (pos 6) must come before tcMar (pos 9)
        if (rightBorder)
        {
            tcp.AppendChild(new OxmlWord.TableCellBorders(
                new OxmlWord.RightBorder
                {
                    Val   = new EnumValue<OxmlWord.BorderValues>(OxmlWord.BorderValues.Single),
                    Size  = 4,
                    Color = RuleHex,
                    Space = 1,
                }));
        }
        tcp.AppendChild(new OxmlWord.TableCellMargin(
            new OxmlWord.RightMargin { Width = "180", Type = OxmlWord.TableWidthUnitValues.Dxa }));
        return cell;
    }

    /// <summary>Paragraph-level horizontal rule (paragraph with bottom border).</summary>
    private static void WordHRule(OxmlWord.Body body, string colour, uint size = 6)
    {
        body.AppendChild(new OxmlWord.Paragraph(
            new OxmlWord.ParagraphProperties(
                new OxmlWord.ParagraphBorders(
                    new OxmlWord.BottomBorder
                    {
                        Val   = new EnumValue<OxmlWord.BorderValues>(OxmlWord.BorderValues.Single),
                        Size  = size,
                        Color = colour,
                        Space = 1,
                    }),
                new OxmlWord.SpacingBetweenLines { Before = "0", After = "60" })));
    }

    /// <summary>Full-width section heading (ALL CAPS, bold, with bottom rule).</summary>
    private static void WordSectionHeading(OxmlWord.Body body, string title, bool centred = false)
    {
        var para = body.AppendChild(new OxmlWord.Paragraph());
        var pPr  = para.AppendChild(new OxmlWord.ParagraphProperties());
        pPr.AppendChild(new OxmlWord.ParagraphBorders(
            new OxmlWord.BottomBorder
            {
                Val   = new EnumValue<OxmlWord.BorderValues>(OxmlWord.BorderValues.Single),
                Size  = 6,
                Color = RuleHex,
                Space = 1,
            }));
        pPr.AppendChild(new OxmlWord.SpacingBetweenLines { Before = "160", After = "60" });
        if (centred) pPr.AppendChild(new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Center });
        para.AppendChild(WordRun(title, HpSection, bold: true, allCaps: true, colour: NavyHex));
    }

    /// <summary>In-cell section heading.</summary>
    private static void WordCellHeading(OxmlWord.TableCell cell, string title)
    {
        var para = cell.AppendChild(new OxmlWord.Paragraph());
        var pPr  = para.AppendChild(new OxmlWord.ParagraphProperties());
        pPr.AppendChild(new OxmlWord.ParagraphBorders(
            new OxmlWord.BottomBorder
            {
                Val   = new EnumValue<OxmlWord.BorderValues>(OxmlWord.BorderValues.Single),
                Size  = 6,
                Color = RuleHex,
                Space = 1,
            }));
        pPr.AppendChild(new OxmlWord.SpacingBetweenLines { Before = "120", After = "40" });
        para.AppendChild(WordRun(title, HpSection, bold: true, allCaps: true, colour: NavyHex));
    }

    private static void WordCellPara(OxmlWord.TableCell cell, string text,
        string fontSize = "20", bool bold = false, string? colour = null,
        bool italic = false, string spaceBefore = "0", int? indent = null)
    {
        var para = cell.AppendChild(new OxmlWord.Paragraph());
        var pPr  = para.AppendChild(new OxmlWord.ParagraphProperties(
            new OxmlWord.SpacingBetweenLines { Before = spaceBefore, After = "40" }));
        if (indent.HasValue)
            pPr.AppendChild(new OxmlWord.Indentation { Left = indent.Value.ToString() });
        para.AppendChild(WordRun(text, fontSize, bold, colour: colour, italic: italic));
    }

    private static void WordCellSpace(OxmlWord.TableCell cell)
    {
        cell.AppendChild(new OxmlWord.Paragraph(
            new OxmlWord.ParagraphProperties(
                new OxmlWord.SpacingBetweenLines { Before = "0", After = "60" })));
    }

    private static void WordPara(OxmlWord.TableCell cell, string text,
        string fontSize = "20", bool bold = false, bool underline = false,
        string? colour = null, bool right = false)
    {
        var para = cell.AppendChild(new OxmlWord.Paragraph());
        var pPr  = para.AppendChild(new OxmlWord.ParagraphProperties(
            new OxmlWord.SpacingBetweenLines { Before = "0", After = "40" }));
        if (right) pPr.AppendChild(new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Right });
        para.AppendChild(WordRun(text, fontSize, bold, underline: underline, colour: colour));
    }

    private static void WordBodyPara(OxmlWord.Body body, string text)
    {
        // spacing (pos 22) must precede jc (pos 27) in CT_PPrBase schema
        body.AppendChild(new OxmlWord.Paragraph(
            new OxmlWord.ParagraphProperties(
                new OxmlWord.SpacingBetweenLines { Before = "40", After = "60" },
                new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Both }),
            WordRun(text, HpBody)));
    }

    private static OxmlWord.Paragraph WordEmptyPara(bool right = false)
    {
        var para = new OxmlWord.Paragraph();
        if (right)
        {
            para.AppendChild(new OxmlWord.ParagraphProperties(
                new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Right }));
        }
        return para;
    }

    private static void WordRunInPara(OxmlWord.Paragraph para, string text,
        string fontSize, bool bold = false, bool italic = false,
        bool underline = false, string? colour = null)
        => para.AppendChild(WordRun(text, fontSize, bold, italic: italic, underline: underline, colour: colour));

    private static OxmlWord.Run WordRun(string text, string fontSize,
        bool bold = false, bool italic = false, bool underline = false,
        bool allCaps = false, string? colour = null)
    {
        var run = new OxmlWord.Run();
        run.AppendChild(WordRunProps(fontSize, bold, italic, underline, allCaps, colour));
        run.AppendChild(new OxmlWord.Text(W(text)) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }

    private static OxmlWord.RunProperties WordRunProps(string fontSize,
        bool bold = false, bool italic = false, bool underline = false,
        bool allCaps = false, string? colour = null)
    {
        // OOXML CT_RPrContents schema order must be respected to avoid Word corruption:
        // rFonts(2) ? b(3) ? i(5) ? caps(7) ? color(19) ? sz(24) ? szCs(25) ? u(27)
        var rp = new OxmlWord.RunProperties();
        rp.AppendChild(WFont(FontFace));                                                        // w:rFonts pos 2
        if (bold)    rp.AppendChild(new OxmlWord.Bold());                                       // w:b     pos 3
        if (italic)  rp.AppendChild(new OxmlWord.Italic());                                     // w:i     pos 5
        if (allCaps) rp.AppendChild(new OxmlWord.Caps());                                       // w:caps  pos 7
        if (colour != null)
            rp.AppendChild(new OxmlWord.Color { Val = colour });                                // w:color pos 19
        rp.AppendChild(WFontSize(fontSize));                                                    // w:sz    pos 24
        rp.AppendChild(new OxmlWord.FontSizeComplexScript { Val = fontSize });                  // w:szCs  pos 25
        if (underline)
            rp.AppendChild(new OxmlWord.Underline { Val = OxmlWord.UnderlineValues.Single });   // w:u     pos 27
        return rp;
    }

    private static OxmlWord.RunFonts WFont(string name) =>
        new() { Ascii = name, HighAnsi = name, ComplexScript = name, EastAsia = name };

    private static OxmlWord.FontSize WFontSize(string halfPt) =>
        new() { Val = halfPt };

    // ----------------------------------------------------------------------
    // Shared utilities
    // ----------------------------------------------------------------------

    /// <summary>
    /// Split description text into individual bullet lines.
    /// Strips any leading bullet markers (\u2022, -, *, ?, ?, ?, ?, \u2022) that the AI may have
    /// already embedded, so callers that prepend their own bullet don't produce double bullets.
    /// </summary>
    private static IEnumerable<string> Lines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (var raw in text.Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            // Strip leading bullet / list markers the AI may have included
            var line = raw.Trim()
                          .TrimStart('\u2022', '-', '*', '?', '?', '?', '?', '\u2022', '+', '\u2022', '\u2022', '?', '?')
                          .Trim()
                          .TrimEnd('.');
            if (!string.IsNullOrEmpty(line)) yield return line;
        }
    }

    /// <summary>
    /// Replace characters that can cause issues in Word XML with safe equivalents.
    /// Note: en-dash (\u2022) and em-dash (\u2022) are preserved as they are valid UTF-8 XML
    /// and render correctly in Calibri/standard Word fonts.
    /// </summary>
    private static string W(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            // Strip XML 1.0 illegal control characters: U+0000-U+0008, U+000B, U+000C, U+000E-U+001F, U+FFFE, U+FFFF
            // These are valid in .NET strings but invalid in XML and corrupt Word documents.
            if (c == '\t' || c == '\n' || c == '\r') { sb.Append(' '); continue; }
            if (c < 0x20 || c == 0xFFFE || c == 0xFFFF) continue;
            // Normalise common Unicode variants to safe equivalents
            switch (c)
            {
                case '\u25CB': sb.Append('o');  break;  // white circle ?
                case '\u2018':
                case '\u2019': sb.Append('\''); break;  // curly single quotes
                case '\u201C':
                case '\u201D': sb.Append('"');  break;  // curly double quotes
                case '\u2026': sb.Append("..."); break; // ellipsis
                case '\u00A0':
                case '\u202F':
                case '\u2009': sb.Append(' ');  break;  // non-breaking / thin spaces
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static T? D<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch (JsonException) { return null; }
    }
}



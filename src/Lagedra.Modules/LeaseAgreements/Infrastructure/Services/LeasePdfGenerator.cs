using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public interface ILeasePdfGenerator
{
    byte[] Generate(string title, string filledHtml);
}

/// <summary>
/// Renders the California lease to match the production DocuSign packet:
/// centered titles, justified body, bold run-in clause names, inspection
/// table, checkbox/write-in lines, and signature blocks.
/// </summary>
public sealed partial class LeasePdfGenerator : ILeasePdfGenerator
{
    static LeasePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(string title, string filledHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(filledHtml);

        var blocks = ParseBlocks(filledHtml);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(72);
                page.DefaultTextStyle(BodyStyle);

                page.Content().Column(col =>
                {
                    foreach (var block in blocks)
                    {
                        RenderBlock(col, block);
                    }
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => BodyStyle(x).FontSize(9)
                    .FontColor(Colors.Grey.Darken2)).Text(txt =>
                {
                    txt.CurrentPageNumber();
                    txt.Span(" of ");
                    txt.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static TextStyle BodyStyle(TextStyle style) =>
        style.FontSize(11)
            .LineHeight(1.15f)
            .FontColor(Colors.Black)
            .FontFamily(Fonts.TimesNewRoman)
            .DisableFontFeature(FontFeatures.StandardLigatures)
            .DisableFontFeature(FontFeatures.ContextualLigatures);

    private static void RenderBlock(ColumnDescriptor col, HtmlBlock block)
    {
        switch (block.Kind)
        {
            case HtmlBlockKind.Heading1:
                col.Item().PaddingBottom(14).AlignCenter()
                    .Text(block.PlainText).Bold().FontSize(18);
                break;

            case HtmlBlockKind.Heading2:
                col.Item().PaddingTop(18).PaddingBottom(10).AlignCenter().Column(inner =>
                {
                    foreach (var line in block.PlainText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        inner.Item().AlignCenter().Text(line.Trim()).Bold().FontSize(14);
                    }
                });
                break;

            case HtmlBlockKind.Paragraph:
                col.Item().PaddingBottom(10).Text(text =>
                {
                    text.Justify();
                    WriteInlines(text, block.Inlines);
                });
                break;

            case HtmlBlockKind.Bullet:
                col.Item().PaddingLeft(18).PaddingBottom(2).Row(row =>
                {
                    row.ConstantItem(14).Text("-");
                    row.RelativeItem().Text(text => WriteInlines(text, block.Inlines));
                });
                break;

            case HtmlBlockKind.Numbered:
                col.Item().PaddingLeft(18).PaddingBottom(4).Row(row =>
                {
                    row.ConstantItem(22).Text($"{block.Number}.");
                    row.RelativeItem().Text(text =>
                    {
                        text.Justify();
                        WriteInlines(text, block.Inlines);
                    });
                });
                break;

            case HtmlBlockKind.Party:
                col.Item().PaddingTop(10).Text(text =>
                {
                    text.Span(block.PlainText).Bold();
                });
                break;

            case HtmlBlockKind.PartyName:
                col.Item().PaddingBottom(4).Text(block.PlainText);
                break;

            case HtmlBlockKind.SignatureLine:
                col.Item().PaddingTop(12).Width(200).BorderBottom(0.7f).BorderColor(Colors.Black).Height(16);
                break;

            case HtmlBlockKind.DateLine:
                col.Item().PaddingTop(2).PaddingBottom(8).Text(block.PlainText);
                break;

            case HtmlBlockKind.Check:
                col.Item().PaddingLeft(10).PaddingBottom(8).Row(row =>
                {
                    row.ConstantItem(40).AlignBottom().Width(32).BorderBottom(0.7f).BorderColor(Colors.Black).Height(12);
                    row.RelativeItem().PaddingLeft(6).Text(text =>
                    {
                        text.Justify();
                        WriteInlines(text, block.Inlines);
                    });
                });
                break;

            case HtmlBlockKind.WriteIn:
                col.Item().PaddingBottom(10).BorderBottom(0.6f).BorderColor(Colors.Black).Height(16);
                break;

            case HtmlBlockKind.Checklist:
                RenderChecklist(col, block.Rows);
                break;
        }
    }

    private static void RenderChecklist(ColumnDescriptor col, IReadOnlyList<ChecklistRow> rows)
    {
        col.Item().PaddingTop(6).PaddingBottom(12).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.4f);
                c.ConstantColumn(110);
                c.RelativeColumn(3.2f);
            });

            foreach (var row in rows)
            {
                if (row.IsHeader)
                {
                    table.Cell().PaddingBottom(4).Text(string.Empty);
                    table.Cell().PaddingBottom(4).AlignCenter().Text("SATISFACTORY").Bold().FontSize(9);
                    table.Cell().PaddingBottom(4).Text("COMMENTS").Bold().FontSize(9);
                    continue;
                }

                table.Cell().PaddingVertical(5).AlignMiddle().Text(row.Item);
                table.Cell().PaddingVertical(5).AlignMiddle().AlignCenter()
                    .Width(48).BorderBottom(0.7f).BorderColor(Colors.Black).Height(12);
                table.Cell().PaddingVertical(5).AlignMiddle()
                    .BorderBottom(0.7f).BorderColor(Colors.Black).Height(12);
            }
        });
    }

    private static void WriteInlines(TextDescriptor text, IReadOnlyList<HtmlInline> inlines)
    {
        if (inlines.Count == 0)
        {
            text.Span(string.Empty);
            return;
        }

        foreach (var inline in inlines)
        {
            if (inline.LineBreak)
            {
                text.EmptyLine();
                continue;
            }

            var span = text.Span(inline.Text);
            if (inline.Bold)
            {
                span.Bold();
            }

            if (inline.Italic)
            {
                span.Italic();
            }
        }
    }

    internal static IReadOnlyList<HtmlBlock> ParseBlocks(string html)
    {
        var normalized = html
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("<br>", "<br/>", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "<br/>", StringComparison.OrdinalIgnoreCase);

        var blocks = new List<HtmlBlock>();
        foreach (Match match in BlockRegex().Matches(normalized))
        {
            var tag = match.Groups[1].Value.ToUpperInvariant();
            var cssClass = ReadClass(match.Groups[2].Value);
            var inner = match.Groups[3].Value;

            switch (tag)
            {
                case "H1":
                    blocks.Add(new HtmlBlock(HtmlBlockKind.Heading1, HeadingText(inner), [], 0, null, []));
                    break;
                case "H2":
                case "H3":
                    blocks.Add(new HtmlBlock(HtmlBlockKind.Heading2, HeadingText(inner), [], 0, null, []));
                    break;
                case "P":
                    blocks.Add(ParagraphBlock(cssClass, inner));
                    break;
                case "UL":
                    foreach (Match li in ListItemRegex().Matches(inner))
                    {
                        blocks.Add(new HtmlBlock(HtmlBlockKind.Bullet, string.Empty, ParseInlines(li.Groups[1].Value), 0, null, []));
                    }

                    break;
                case "OL":
                    var n = 0;
                    foreach (Match li in ListItemRegex().Matches(inner))
                    {
                        n++;
                        blocks.Add(new HtmlBlock(HtmlBlockKind.Numbered, string.Empty, ParseInlines(li.Groups[1].Value), n, null, []));
                    }

                    break;
                case "TABLE":
                    blocks.Add(ParseChecklist(inner));
                    break;
            }
        }

        return blocks;
    }

    private static HtmlBlock ParagraphBlock(string? cssClass, string inner)
    {
        var kind = cssClass switch
        {
            "party" => HtmlBlockKind.Party,
            "party-name" => HtmlBlockKind.PartyName,
            "sigline" => HtmlBlockKind.SignatureLine,
            "date-line" => HtmlBlockKind.DateLine,
            "check" => HtmlBlockKind.Check,
            "writein" => HtmlBlockKind.WriteIn,
            _ => HtmlBlockKind.Paragraph
        };

        var plain = kind is HtmlBlockKind.Party or HtmlBlockKind.PartyName or HtmlBlockKind.DateLine
            ? Decode(StripTags(inner)).Trim()
            : string.Empty;

        return new HtmlBlock(kind, plain, ParseInlines(inner), 0, cssClass, []);
    }

    private static HtmlBlock ParseChecklist(string inner)
    {
        var rows = new List<ChecklistRow>();
        foreach (Match tr in RowRegex().Matches(inner))
        {
            var cells = CellRegex().Matches(tr.Groups[1].Value)
                .Select(c => Decode(StripTags(c.Groups[2].Value)).Trim())
                .ToList();
            if (cells.Count == 0)
            {
                continue;
            }

            var isHeader = tr.Groups[1].Value.Contains("<th", StringComparison.OrdinalIgnoreCase);
            rows.Add(new ChecklistRow(cells[0], isHeader));
        }

        return new HtmlBlock(HtmlBlockKind.Checklist, string.Empty, [], 0, "checklist", rows);
    }

    internal static IReadOnlyList<HtmlInline> ParseInlines(string html)
    {
        var inlines = new List<HtmlInline>();
        var remaining = html;
        while (remaining.Length > 0)
        {
            var next = InlineTagRegex().Match(remaining);
            if (!next.Success)
            {
                AppendText(inlines, remaining);
                break;
            }

            if (next.Index > 0)
            {
                AppendText(inlines, remaining[..next.Index]);
            }

            if (!next.Groups[1].Success)
            {
                inlines.Add(HtmlInline.Break());
            }
            else
            {
                var tag = next.Groups[1].Value.ToUpperInvariant();
                AppendText(inlines, next.Groups[2].Value, bold: tag is "STRONG" or "B", italic: tag is "EM" or "I");
            }

            remaining = remaining[(next.Index + next.Length)..];
        }

        return inlines;
    }

    private static void AppendText(List<HtmlInline> inlines, string raw, bool bold = false, bool italic = false)
    {
        var text = Decode(StripTags(raw));
        if (text.Length == 0)
        {
            return;
        }

        inlines.Add(new HtmlInline(text, bold, italic, false));
    }

    private static string HeadingText(string html) =>
        Decode(StripTags(html
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase))).Trim();

    private static string? ReadClass(string attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes))
        {
            return null;
        }

        var match = ClassRegex().Match(attributes);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string StripTags(string html) => TagRegex().Replace(html, string.Empty);

    private static string Decode(string text) =>
        WebUtility.HtmlDecode(text).Replace('\u00a0', ' ');

    [GeneratedRegex(@"<(h1|h2|h3|p|ul|ol|table)(\s[^>]*)?>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BlockRegex();

    [GeneratedRegex(@"<li(?:\s[^>]*)?>(.*?)</li>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"<(strong|b|em|i)(?:\s[^>]*)?>(.*?)</\1>|<br\s*/?>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex InlineTagRegex();

    [GeneratedRegex(@"<tr(?:\s[^>]*)?>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex RowRegex();

    [GeneratedRegex(@"<(td|th)(?:\s[^>]*)?>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex CellRegex();

    [GeneratedRegex(@"class\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ClassRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}

internal enum HtmlBlockKind
{
    Heading1,
    Heading2,
    Paragraph,
    Bullet,
    Numbered,
    Party,
    PartyName,
    SignatureLine,
    DateLine,
    Check,
    WriteIn,
    Checklist
}

internal sealed record ChecklistRow(string Item, bool IsHeader);

internal sealed record HtmlBlock(
    HtmlBlockKind Kind,
    string PlainText,
    IReadOnlyList<HtmlInline> Inlines,
    int Number,
    string? CssClass,
    IReadOnlyList<ChecklistRow> Rows);

internal sealed record HtmlInline(string Text, bool Bold, bool Italic, bool LineBreak)
{
    public static HtmlInline Break() => new(string.Empty, false, false, true);
}

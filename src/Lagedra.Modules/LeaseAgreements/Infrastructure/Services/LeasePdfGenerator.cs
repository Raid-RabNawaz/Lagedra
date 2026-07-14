using System.Globalization;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public interface ILeasePdfGenerator
{
    byte[] Generate(string title, string filledHtml);
}

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

        var plain = HtmlToPlainText(filledHtml);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.Letter);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).SemiBold().FontSize(16);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(16).Text(text =>
                {
                    foreach (var paragraph in plain.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(paragraph))
                        {
                            text.EmptyLine();
                        }
                        else
                        {
                            text.Line(paragraph.Trim());
                        }
                    }
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Page ");
                    txt.CurrentPageNumber();
                    txt.Span(" of ");
                    txt.TotalPages();
                    txt.Span($"  ·  Generated {DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)}");
                });
            });
        }).GeneratePdf();
    }

    private static string HtmlToPlainText(string html)
    {
        var withBreaks = html
            .Replace("</p>", "\n\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</li>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</h1>", "\n\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</h2>", "\n\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</h3>", "\n\n", StringComparison.OrdinalIgnoreCase);

        var stripped = TagRegex().Replace(withBreaks, string.Empty);
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}

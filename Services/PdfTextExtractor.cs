using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace dotnet_gs2_2025.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream == null)
        {
            throw new ArgumentNullException(nameof(pdfStream));
        }

        if (!pdfStream.CanSeek)
        {
            throw new InvalidOperationException("Fluxo do PDF precisa suportar Seek para extração.");
        }

        using var document = PdfDocument.Open(pdfStream, new ParsingOptions { UseLenientParsing = true });
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendPageText(builder, page);
        }

        return Task.FromResult(builder.ToString());
    }

    private static void AppendPageText(StringBuilder builder, Page page)
    {
        var text = page.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            builder.AppendLine(text);
        }
    }
}


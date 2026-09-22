using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UniPilot.Application.Documents;

namespace UniPilot.Infrastructure.Documents;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public IReadOnlyList<ExtractedPdfPage> Extract(
        Stream pdfStream)
    {
        if (!pdfStream.CanSeek)
        {
            throw new InvalidOperationException(
                "The PDF stream must support seeking.");
        }

        pdfStream.Position = 0;

        using var document = PdfDocument.Open(pdfStream);

        var pages = new List<ExtractedPdfPage>(
            document.NumberOfPages);

        foreach (var page in document.GetPages())
        {
            var text =
                ContentOrderTextExtractor.GetText(page);

            pages.Add(new ExtractedPdfPage(
                page.Number,
                text));
        }

        return pages;
    }
}
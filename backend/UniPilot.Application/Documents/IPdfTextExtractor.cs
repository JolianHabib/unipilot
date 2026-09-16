namespace UniPilot.Application.Documents;

public interface IPdfTextExtractor
{
    IReadOnlyList<ExtractedPdfPage> Extract(
        Stream pdfStream);
}
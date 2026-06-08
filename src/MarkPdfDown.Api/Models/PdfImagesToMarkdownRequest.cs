namespace MarkPdfDown.Api.Models;

public record class PdfImagesToMarkdownRequest(string FileName, string ContentType)
{
    public IList<byte[]> Pages { get; } = [];
}

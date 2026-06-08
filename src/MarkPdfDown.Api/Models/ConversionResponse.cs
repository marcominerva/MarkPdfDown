namespace MarkPdfDown.Api.Models;

public record class ConversionResponse(string FileName, string ContentType)
{
    public IList<ConversionResult> Pages { get; set; } = [];
}
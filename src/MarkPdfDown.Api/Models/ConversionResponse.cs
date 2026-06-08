namespace MarkPdfDown.Api.Models;

public record class ConversionResponse(IEnumerable<ConversionResult> Pages);
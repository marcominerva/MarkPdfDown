using System.ComponentModel;

namespace MarkPdfDown.Api.Models;

public record class ConversionResponse(string MarkdownContent,
    [property: Description("The number of pages in the PDF")] int PageCount);

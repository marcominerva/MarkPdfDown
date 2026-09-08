using System.ComponentModel;

namespace MarkPdfDown.Api.Models;

/// <summary>
/// Represents the markdown conversion of a single PDF page.
/// </summary>
/// <param name="PageNumber">The 1-based number of the converted page.</param>
/// <param name="MarkdownContent">The markdown code corresponding to the whole content of the page.</param>
public record class PageConversion([property: Description("The 1-based number of the converted page")] int PageNumber,
    [property: Description("The complete markdown content of the page, without code fences")] string MarkdownContent);

namespace MarkPdfDown.Api.Models;

public record class PdfImageToMarkdownRequest(int PageNumber, byte[]? ImageContent);

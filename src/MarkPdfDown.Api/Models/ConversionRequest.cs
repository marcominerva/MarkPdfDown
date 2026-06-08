namespace MarkPdfDown.Api.Models;

public record class ConversionRequest(byte[] Content, string FileName, string ContentType);

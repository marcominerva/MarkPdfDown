namespace MarkPdfDown.Api.Models;

public record class RenderPageRequest(byte[] Content, int PageIndex, int PageCount);

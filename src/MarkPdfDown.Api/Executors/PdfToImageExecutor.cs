using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI.Workflows;
using PDFtoImage;
using SkiaSharp;

namespace MarkPdfDown.Api.Executors;

[SendsMessage(typeof(RenderPageRequest))]
[SendsMessage(typeof(PdfImageToMarkdownRequest))]
public partial class PdfToImageExecutor() : Executor(nameof(PdfToImageExecutor))
{
    [MessageHandler]
    private async ValueTask HandleAsync(ConversionRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        var pageCount = Conversion.GetPageCount(request.Content);

        // Start a page-by-page loop. Each page is rendered in its own superstep (via the
        // self-edge), so the markdown conversion of page N overlaps with the rendering of
        // page N + 1 instead of waiting for the whole document to be rendered first.
        await context.SendMessageAsync(new RenderPageRequest(request.Content, 0, pageCount), cancellationToken);
    }

    [MessageHandler]
    private async ValueTask HandleAsync(RenderPageRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        if (request.PageIndex >= request.PageCount)
        {
            // Every page has been dispatched; signal completion with PageNumber set to -1.
            await context.SendMessageAsync(new PdfImageToMarkdownRequest(-1, null), cancellationToken);
            return;
        }

        var options = new RenderOptions(Dpi: 100);

        await foreach (var bitmap in Conversion.ToImagesAsync(request.Content, [request.PageIndex], options: options, cancellationToken: cancellationToken))
        {
            using (bitmap)
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);

                await context.SendMessageAsync(new PdfImageToMarkdownRequest(request.PageIndex, data.ToArray()), cancellationToken);
            }
        }

        // Queue the next page so it is rendered in the following superstep.
        await context.SendMessageAsync(new RenderPageRequest(request.Content, request.PageIndex + 1, request.PageCount), cancellationToken);
    }
}

using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI.Workflows;
using PDFtoImage;
using SkiaSharp;

namespace MarkPdfDown.Api.Executors;

[SendsMessage(typeof(PdfImageToMarkdownRequest))]
public partial class PdfToImageExecutor() : Executor(nameof(PdfToImageExecutor))
{
    [MessageHandler]
    private async ValueTask HandleAsync(ConversionRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        var options = new RenderOptions(Dpi: 100);

        await foreach (var (index, bitmap) in Conversion.ToImagesAsync(request.Content, options: options, cancellationToken: cancellationToken).Index())
        {
            using (bitmap)
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);

                await context.SendMessageAsync(new PdfImageToMarkdownRequest(index, data.ToArray()), cancellationToken);
                await Task.Delay(5000);
            }
        }

        // Sends a final message to indicate completion, with PageNumber set to -1.
        await context.SendMessageAsync(new PdfImageToMarkdownRequest(-1, null), cancellationToken);
    }
}

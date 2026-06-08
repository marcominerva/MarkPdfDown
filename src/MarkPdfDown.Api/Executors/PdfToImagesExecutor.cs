using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI.Workflows;
using PDFtoImage;
using SkiaSharp;

namespace MarkPdfDown.Api.Executors;

public partial class PdfToImagesExecutor() : Executor(nameof(PdfToImagesExecutor))
{
    [MessageHandler]
    private async ValueTask<PdfImagesToMarkdownRequest> HandleAsync(ConversionRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        var result = new PdfImagesToMarkdownRequest(request.FileName, request.ContentType);

        var options = new RenderOptions(Dpi: 70);

        await foreach (var bitmap in Conversion.ToImagesAsync(request.Content, options: options, cancellationToken: cancellationToken))
        {
            using (bitmap)
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);

                result.Pages.Add(data.ToArray());
            }
        }

        return result;
    }
}

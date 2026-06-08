using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI.Workflows;

namespace MarkPdfDown.Api.Executors;

public partial class FormFileToConversionRequestExecutor() : Executor(nameof(FormFileToConversionRequestExecutor))
{
    [MessageHandler]
    private async ValueTask<ConversionRequest> HandleAsync(IFormFile file, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        // Get the byte array from the stream.
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        return new(fileBytes, Path.GetFileName(file.FileName), file.ContentType);
    }
}

using System.Net.Mime;
using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MarkPdfDown.Api.Executors;

public partial class PdfImagesToMarkdownExecutor([FromKeyedServices("PdfToMarkdownConverterAgent")] AIAgent agent) : Executor(nameof(PdfImagesToMarkdownExecutor))
{
    [MessageHandler]
    private async ValueTask<ConversionResponse> HandleAsync(PdfImagesToMarkdownRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        var result = new ConversionResponse(request.FileName, request.ContentType);

        foreach (var (index, page) in request.Pages.AsParallel().WithDegreeOfParallelism(2).WithCancellation(cancellationToken).Index())
        {
            var message = new ChatMessage(ChatRole.User, [new DataContent(page, MediaTypeNames.Image.Jpeg)]);
            var markdown = await agent.RunAsync(message, cancellationToken: cancellationToken);

            result.Pages.Add(new ConversionResult(index, markdown.Text));
        }

        return result;
    }
}

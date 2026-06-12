using System.Net.Mime;
using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MarkPdfDown.Api.Executors;

public partial class PdfToMarkdownExecutor([FromKeyedServices("PdfToMarkdownConverterAgent")] AIAgent agent) : Executor(nameof(PdfToMarkdownExecutor))
{
    [MessageHandler]
    private async ValueTask<ConversionResponse> HandleAsync(ConversionRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        var message = new ChatMessage(ChatRole.User, [new DataContent(request.Content, request.ContentType)]);
        var response = await agent.RunAsync<ConversionResponse>(message, cancellationToken: cancellationToken);

        return response.Result;
    }
}

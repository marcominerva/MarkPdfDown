using System.Net.Mime;
using System.Runtime.CompilerServices;
using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.AI;
using PDFtoImage;
using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;

namespace MarkPdfDown.Api.Executors;

[SendsMessage(typeof(ConversionResult))]
public partial class PdfImageToMarkdownExecutor([FromKeyedServices("PdfToMarkdownConverterAgent")] AIAgent agent) : Executor(nameof(PdfImageToMarkdownExecutor))
{
    [MessageHandler]
    private async ValueTask HandleAsync(PdfImageToMarkdownRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        if (request.PageNumber == -1)
        {
            await context.SendMessageAsync(new ConversionResult(request.PageNumber, "No more pages to process."), cancellationToken);
            return;
        }

        var message = new ChatMessage(ChatRole.User, [new DataContent(request.ImageContent, MediaTypeNames.Image.Jpeg)]);
        var result = await agent.RunAsync(message, cancellationToken: cancellationToken);        

        await context.SendMessageAsync(new ConversionResult(request.PageNumber, result.Text), cancellationToken);
    }
}

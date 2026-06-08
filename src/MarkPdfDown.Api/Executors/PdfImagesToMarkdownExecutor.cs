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

        //foreach (var (index, page) in request.Pages.Index())
        //{
        //    var message = new ChatMessage(ChatRole.User, [new DataContent(page, MediaTypeNames.Image.Jpeg)]);
        //    var markdown = await agent.RunAsync(message, cancellationToken: cancellationToken);

        //    result.Pages.Add(new ConversionResult(index, markdown.Text));
        //}

        using var throttler = new SemaphoreSlim(3);

        var tasks = request.Pages.Select(async (page, index) =>
            {
                await throttler.WaitAsync(cancellationToken);

                try
                {
                    var message = new ChatMessage(ChatRole.User, [new DataContent(page, MediaTypeNames.Image.Jpeg)]);
                    var markdown = await agent.RunAsync(message, cancellationToken: cancellationToken);

                    return new ConversionResult(index, markdown.Text);
                }
                finally
                {
                    throttler.Release();
                }
            })
            .ToArray();

        var pages = await Task.WhenAll(tasks);

        foreach (var page in pages.OrderBy(static page => page.PageNumber))
        {
            result.Pages.Add(page);
        }

        return result;
    }
}

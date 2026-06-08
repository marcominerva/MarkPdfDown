using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI.Workflows;

namespace MarkPdfDown.Api.Executors;

[YieldsOutput(typeof(ConversionResponse))]
public partial class ConversionAggregatingExecutor() : Executor(nameof(ConversionAggregatingExecutor))
{
    private readonly List<ConversionResult> pages = [];

    [MessageHandler]
    private async ValueTask HandleAsync(ConversionResult result, IWorkflowContext context, CancellationToken cancellationToken)
    {
        if (result.PageNumber == -1)
        {
            // This is a signal that all pages have been processed, so we can send the final response.
            await context.YieldOutputAsync(new ConversionResponse(pages.OrderBy(p => p.PageNumber)), cancellationToken);
        }
        else
        {
            // Add the page result to the list.
            pages.Add(result);
        }
    }
}

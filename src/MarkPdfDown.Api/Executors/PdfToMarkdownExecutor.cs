using System.Text;
using MarkPdfDown.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace MarkPdfDown.Api.Executors;

public partial class PdfToMarkdownExecutor([FromKeyedServices("PdfToMarkdownConverterAgent")] AIAgent agent) : Executor(nameof(PdfToMarkdownExecutor))
{
    private const int MaxConcurrentPageConversions = 4;
    private const int ContextCharacters = 800;

    [MessageHandler]
    private async ValueTask<ConversionResponse> HandleAsync(ConversionRequest request, IWorkflowContext context, CancellationToken cancellationToken)
    {
        // The whole PDF cannot be converted with a single call, because the response would be truncated by the model
        // output token limit. Each page is extracted as a standalone PDF and converted with an independent request,
        // so that the input stays small and the pages can be processed in parallel.
        var pages = ExtractPages(request.Content);
        var markdownPages = new string[pages.Count];

        await Parallel.ForEachAsync(Enumerable.Range(0, pages.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentPageConversions, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                var page = pages[index];

                var message = new ChatMessage(ChatRole.User,
                [
                    new TextContent(BuildPrompt(page, index + 1, pages.Count)),
                    new DataContent(page.Content, request.ContentType)
                ]);

                // Each request is self-contained: the single-page PDF plus the excerpt of the previous page provide
                // all the needed context, so no conversation history has to be kept across pages.
                var response = await agent.RunAsync<PageConversion>(message, cancellationToken: token);

                markdownPages[index] = response.Result.MarkdownContent.Trim();
            });

        return new(string.Join(Environment.NewLine + Environment.NewLine, markdownPages), pages.Count);
    }

    private static string BuildPrompt(PdfPage page, int pageNumber, int pageCount)
    {
        var prompt = new StringBuilder($"""
            The attached PDF contains only page {pageNumber} of {pageCount} of the original document.
            Convert it to markdown, returning all its content in the original order.
            Do not summarize, do not skip content and do not add any reference to other pages.
            """);

        if (page.PreviousPageExcerpt is { Length: > 0 } excerpt)
        {
            prompt.Append($"""

                For continuity only, this is the final part of the raw text of the previous page. Use it to keep the same
                structure and formatting, and to correctly continue elements (tables, lists, paragraphs) that started there.
                Do not convert it and do not include it in your answer:
                ---
                {excerpt}
                ---
                """);
        }

        return prompt.ToString();
    }

    private static List<PdfPage> ExtractPages(byte[] content)
    {
        using var document = PdfDocument.Open(content);

        var pages = new List<PdfPage>(document.NumberOfPages);
        string? previousPageExcerpt = null;

        for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
        {
            using var builder = new PdfDocumentBuilder();
            builder.AddPage(document, pageNumber);

            pages.Add(new(builder.Build(), previousPageExcerpt));

            var text = document.GetPage(pageNumber).Text;
            previousPageExcerpt = text.Length > ContextCharacters ? text[^ContextCharacters..] : text;
        }

        return pages;
    }

    private record class PdfPage(byte[] Content, string? PreviousPageExcerpt);
}

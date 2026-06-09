using System.ClientModel;
using System.Net.Mime;
using Markdig;
using MarkPdfDown.Api.Executors;
using MarkPdfDown.Api.Models;
using MarkPdfDown.Api.Settings;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container.
var openAISettings = builder.Services.ConfigureAndGet<AzureOpenAISettings>(builder.Configuration, "AzureOpenAI")!;
builder.Services.AddChatClient(_ =>
{
    var openAIClient = new OpenAIClient(new ApiKeyCredential(openAISettings.ApiKey), new() { Endpoint = new(openAISettings.Endpoint) });
    return openAIClient.GetResponsesClient().AsIChatClientWithStoredOutputDisabled(openAISettings.Deployment);
});

builder.Services.AddAIAgent("PdfToMarkdownConverterAgent", (services, key) =>
{
    var chatClient = services.GetRequiredService<IChatClient>();

    return chatClient.AsAIAgent(new()
    {
        Name = key,
        ChatOptions = new()
        {
            Instructions = """
                You are an assistant that converts images, which represent pages of a PDF, into the corresponding markdown code.
                Your purpose is to help users obtain markdown representations of PDF pages, making them easier to edit and share as text.
                You must return the raw markdown directly, without wrapping the response in any code blocks or markdown fences.
                """,
            Reasoning = new()
            {
                Effort = ReasoningEffort.None,
                Output = ReasoningOutput.None
            }
        }
    },
    loggerFactory: services.GetRequiredService<ILoggerFactory>(),
    services: services);
});

builder.Services.AddSingleton<FormFileToConversionRequestExecutor>();
builder.Services.AddSingleton<PdfToImagesExecutor>();
builder.Services.AddSingleton<PdfImagesToMarkdownExecutor>();

builder.AddWorkflow("PdfToMarkdownConversionWorkflow", (services, key) =>
{
    var formFileToConversionRequestExecutor = services.GetRequiredService<FormFileToConversionRequestExecutor>();
    var pdfToImageExecutor = services.GetRequiredService<PdfToImagesExecutor>();
    var pdfImageToMarkdownExecutor = services.GetRequiredService<PdfImagesToMarkdownExecutor>();

    var workflow = new WorkflowBuilder(formFileToConversionRequestExecutor).WithName(key)
        .AddEdge(formFileToConversionRequestExecutor, pdfToImageExecutor)
        .AddEdge(pdfToImageExecutor, pdfImageToMarkdownExecutor)
        .WithOutputFrom(pdfImageToMarkdownExecutor)
        .Build(validateOrphans: true);

    return workflow;
}, ServiceLifetime.Scoped);

builder.Services.AddOpenApi(options =>
{
    options.RemoveServerList();
    options.AddDefaultProblemDetailsResponse();

});

builder.Services.AddValidation();
builder.Services.AddDefaultProblemDetails();
builder.Services.AddDefaultExceptionHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapSwaggerUI(setupAction: options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", builder.Environment.ApplicationName);
});

app.UseRouting();

app.MapPost("/api/convert", async (FormFile file, [FromKeyedServices("PdfToMarkdownConversionWorkflow")] Workflow workflow, CancellationToken cancellationToken) =>
{
    await using var run = await InProcessExecution.RunAsync(workflow, file, cancellationToken: cancellationToken);

    var exception = run.NewEvents.OfType<WorkflowErrorEvent>().Select(e => e.Exception).FirstOrDefault();
    if (exception is not null)
    {
        throw exception;
    }

    var result = run.NewEvents.OfType<WorkflowOutputEvent>().Select(e => e.Data).OfType<ConversionResponse>().FirstOrDefault();

    return TypedResults.Ok(result);
})
.DisableAntiforgery()
.Produces<ConversionResponse>();

app.MapPost("/api/md-to-html", async (ConversionResponse response) =>
{
    var markdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    var markdown = response.Pages.Select(p => p.Markdown).Aggregate((a, b) => $"{a}\n{b}");
    var html = Markdown.ToHtml(markdown, markdownPipeline);

    return TypedResults.Content(html, MediaTypeNames.Text.Html);
})
.Produces<string>();

app.Run();

# MarkPdfDown

Convert PDF pages to Markdown using an ASP.NET Core API backed by Azure OpenAI.

## Overview

MarkPdfDown exposes a small HTTP API that turns a PDF into per-page Markdown. The conversion pipeline is implemented as a workflow made of three steps:

1. The uploaded PDF is read from the incoming form file.
2. The PDF is rendered into page images.
3. Each page image is sent to an AI agent that returns raw Markdown.

The result preserves the page order and returns a structured response with one Markdown block per page.

## How it works

The API composition root is `src/MarkPdfDown.Api/Program.cs`.

### Conversion flow

- `POST /api/convert` receives a PDF as multipart form data.
- The conversion is orchestrated by a `Workflow` created with the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework).
- The workflow returns a `ConversionResponse` with the file name, content type, and a list of page results.

### Markdown to HTML

- `POST /api/md-to-html` accepts a `ConversionResponse` payload.
- It concatenates all page Markdown and renders it to HTML with `Markdig`.

## Requirements

- .NET 10 SDK
- Azure OpenAI endpoint, deployment name, and API key

## Configuration

The API reads Azure OpenAI settings from the `AzureOpenAI` section.

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "Deployment": "<your-deployment-name>",
    "ApiKey": "<your-api-key>"
  }
}
```

You can place local overrides in `src/MarkPdfDown.Api/appsettings.local.json`.

## Running locally

```powershell
dotnet run --project src/MarkPdfDown.Api
```

The application enables OpenAPI and Swagger UI on startup.

## API endpoints

### Convert PDF to Markdown

```http
POST /api/convert
Content-Type: multipart/form-data
```

Form fields:

- `file`: the PDF document to convert

Response:

```json
{
  "fileName": "sample.pdf",
  "contentType": "application/pdf",
  "pages": [
    {
      "pageNumber": 0,
      "markdown": "# Page 1..."
    }
  ]
}
```

### Render Markdown as HTML

```http
POST /api/md-to-html
Content-Type: application/json
```

Body: a `ConversionResponse` object.

Response: HTML rendered from the Markdown content.

## Example usage

### cURL

```bash
curl -X POST "https://localhost:5001/api/convert" \
  -F "file=@sample.pdf"
```

### Convert response to HTML

```bash
curl -X POST "https://localhost:5001/api/md-to-html" \
  -H "Content-Type: application/json" \
  -d '{
    "fileName": "sample.pdf",
    "contentType": "application/pdf",
    "pages": [
      { "pageNumber": 0, "markdown": "# Page 1" }
    ]
  }'
```

## Notes

- Page conversion is throttled so only a few page requests run at the same time.
- The workflow keeps the original page order when returning the final response.

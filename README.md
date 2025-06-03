# PDF Text Analyzer

A C# console application that extracts text from PDF files using PdfPig and sends it to Azure AI models (including GitHub models) for analysis and summarization.

## Features

- Extract text from PDF files using PdfPig
- Send extracted text to Azure AI Inference API
- Support for GitHub models via Azure AI
- Configurable prompts and models
- Clean separation of concerns with dependency injection

## Setup

### 1. Prerequisites

- .NET 8.0 SDK
- Azure AI account or GitHub personal access token for GitHub models

### 2. Configuration

#### Option A: Using GitHub Models (Recommended)

1. Get a GitHub personal access token with appropriate permissions
2. Set your API key using user secrets:

```bash
dotnet user-secrets set "AzureAI:ApiKey" "your-github-token"
```

The endpoint `https://models.inference.ai.azure.com` is already configured for GitHub models.

#### Option B: Using Azure AI directly

1. Create an Azure AI resource
2. Get your endpoint and API key
3. Configure using user secrets:

```bash
dotnet user-secrets set "AzureAI:Endpoint" "your-azure-endpoint"
dotnet user-secrets set "AzureAI:ApiKey" "your-azure-api-key"
```

### 3. Available Models

Common GitHub models you can use:
- `gpt-4o-mini` (default, fast and cost-effective)
- `gpt-4o`
- `gpt-3.5-turbo`
- `claude-3-haiku`
- `claude-3-sonnet`

Update the model in `appsettings.json` or via user secrets:

```bash
dotnet user-secrets set "AzureAI:ModelName" "gpt-4o"
```

## Usage

### Build the application:

```bash
dotnet build
```

### Run the application:

```bash
dotnet run -- path/to/your/file.pdf
```

### Example:

```bash
dotnet run -- sample-document.pdf
```

## Project Structure

```
PdfTextAnalyzer/
├── Program.cs                      # Application entry point
├── appsettings.json               # Configuration file
├── PdfTextAnalyzer.csproj         # Project file
├── Services/
│   ├── IPdfTextExtractor.cs       # PDF extraction interface
│   ├── PdfTextExtractor.cs        # PDF extraction implementation
│   ├── IAzureAiService.cs         # AI service interface
│   ├── AzureAiService.cs          # AI service implementation
│   ├── ITextAnalysisService.cs    # Main service interface
│   └── TextAnalysisService.cs     # Main service implementation
└── README.md                      # This file
```

## Customization

### System Message Configuration

You can customize the AI system message in `appsettings.json`:

```json
{
  "Analysis": {
    "SystemMessage": "You are an expert document analyst specializing in technical documentation. Provide detailed analysis with structured insights."
  }
}
```

### Custom User Prompts

You can customize the analysis prompt in `appsettings.json`:

```json
{
  "Analysis": {
    "DefaultPrompt": "Extract the main topics and create a bullet-point summary:"
  }
}
```

### Error Handling

The application includes comprehensive error handling for:
- Missing or invalid PDF files
- API authentication issues
- Network connectivity problems
- Invalid model configurations

## Troubleshooting

1. **Authentication Error**: Ensure your API key is correctly set in user secrets
2. **Model Not Found**: Verify the model name is supported by your chosen service
3. **PDF Reading Error**: Ensure the PDF is not encrypted or corrupted
4. **Network Issues**: Check your internet connection and endpoint URL

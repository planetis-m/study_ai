# PDF Text Analyzer

A C# console application that extracts text from PDF files using PdfPig and analyzes it using Azure AI models, including support for GitHub-hosted models. The application features a configurable pipeline with optional text preprocessing and analysis stages.

## Features

- **PDF Text Extraction**: Extract text from PDF files using PdfPig with advanced extraction options
- **AI-Powered Text Cleaning**: Optional preprocessing stage to clean and format extracted text
- **AI-Powered Analysis**: Analyze processed text using configurable AI models
- **Flexible AI Model Support**: Works with Azure AI services and GitHub-hosted models
- **Configurable Pipeline**: Enable/disable preprocessing and analysis stages independently
- **Clean Architecture**: Dependency injection and separation of concerns
- **Comprehensive Configuration**: Configurable models, prompts, and extraction settings

## Prerequisites

- .NET 9.0 SDK
- Azure AI account or GitHub personal access token (for GitHub-hosted models)

## Setup

### 1. Clone and Build

```bash
git clone https://github.com/planetis-m/study_ai.git
cd study_ai/PdfTextAnalyzer
dotnet build
```

### 2. Configure API Access

The application uses .NET user secrets to store sensitive configuration.

#### Option A: Using GitHub Models (Recommended)

1. Get a GitHub personal access token with appropriate permissions
2. Set your API key:
   ```bash
   # For Azure AI (GitHub Models)
   dotnet user-secrets set "AI:AzureAI:ApiKey" "your-github-token"

   # For Google Generative AI
   dotnet user-secrets set "AI:GoogleAI:ApiKey" "your-google-api-key"

   # For OpenAI
   dotnet user-secrets set "AI:OpenAI:ApiKey" "your-openai-api-key"
   ```

The endpoint `https://models.github.ai/inference` is pre-configured for GitHub models.

#### Option B: Using Azure AI Directly

1. Create an Azure AI resource and get your endpoint and API key
2. Configure using user secrets:
   ```bash
   dotnet user-secrets set "AI:AzureAI:Endpoint" "your-azure-endpoint"
   dotnet user-secrets set "AI:AzureAI:ApiKey" "your-azure-api-key"
   ```

### 3. Configure Models (Optional)

The application comes with sensible defaults, but you can customize the models in `appsettings.json`:

- **Preprocessing Model**: `mistral-ai/mistral-medium-2505` (for text cleaning)
- **Analysis Model**: `openai/gpt-4.1` (for main analysis)

## Usage

Run the application from the `PdfTextAnalyzer` directory:

```bash
dotnet run -- path/to/your/document.pdf
```

Example:

```bash
dotnet run -- "research-paper.pdf"
```

## Configuration

The application behavior is controlled through `appsettings.json`:

### Pipeline Settings

```json
{
  "Pipeline": {
    "Preprocessing": true,  // Enable/disable text cleaning
    "Analysis": true        // Enable/disable AI analysis
  }
}
```

### PDF Extraction Settings

```json
{
  "PdfExtraction": {
    "UseAdvancedExtraction": true,
    "ExcludeHeaderFooter": true,
    "UseReadingOrderDetection": false,
  }
}
```

### AI Model Configuration

```json
{
  "Preprocessing": {
    "Model": {
      "Provider": "AzureAI",
      "ModelName": "mistral-ai/mistral-medium-2505",
      "MaxTokens": 2000,
      "Temperature": 0.0,
      "TopP": 1.0
    }
  },
  "Analysis": {
    "Model": {
      "Provider": "OpenAI",
      "ModelName": "gpt-4.1",
      "MaxTokens": 4000,
      "Temperature": 0.0,
      "TopP": 1.0
    }
  }
}
```

## Project Structure

```
study_ai/
├── PdfTextAnalyzer/
│   ├── Program.cs                     # Application entry point
│   ├── appsettings.json               # Configuration file
│   ├── PdfTextAnalyzer.csproj         # Project file
│   ├── Configuration/                 # Configuration models
│   │   ├── AiSettings.cs
│   │   ├── AnalysisSettings.cs
│   │   ├── AzureAiSettings.cs
│   │   ├── GoogleAiSettings.cs
│   │   ├── ModelSettings.cs
│   │   ├── OpenAiSettings.cs
│   │   ├── PdfExtractionSettings.cs
│   │   ├── PipelineSettings.cs
│   │   └── PreprocessorSettings.cs
│   ├── Models/                        # Data/result models
│   │   └── PipelineResults.cs
│   ├── Services/                      # Service implementations
│   │   ├── AiServiceBase.cs
│   │   ├── AiServiceFactory.cs
│   │   ├── IPdfAnalysisPipelineCore.cs
│   │   ├── IPdfAnalysisPipelinePresenter.cs
│   │   ├── IPdfTextExtractor.cs
│   │   ├── ITextAnalysisAiService.cs
│   │   ├── ITextCleaningService.cs
│   │   ├── PdfAnalysisPipelineCore.cs
│   │   ├── PdfAnalysisPipelinePresenter.cs
│   │   ├── PdfTextExtractor.cs
│   │   ├── TextAnalysisService.cs
│   │   └── TextCleaningService.cs
│   ├── setup.sh                       # Setup script
│   └── README.md
```

## Error Handling

The application includes comprehensive error handling for:
- Missing or invalid PDF files
- API authentication issues
- Network connectivity problems
- Invalid model configurations
- PDF parsing errors

## Troubleshooting

**Authentication Error**: Ensure your API key is correctly set in user secrets and valid for the configured endpoint.

**Model Not Found**: Verify the model name in `appsettings.json` is supported by your AI service.

**PDF Reading Error**: Ensure the PDF file is not encrypted, corrupted, or inaccessible.

**Network Issues**: Check your internet connection and AI service endpoint availability.

## License

This project is licensed under the GPLv3 License - see the [LICENSE](LICENSE) file for details.

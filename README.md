# Lecture Slide Analyzer for Exam Prep

A C# console application designed to extract and process text from PDF lecture slides. It uses PdfPig for text extraction, followed by AI-driven cleaning and analysis to help students prepare for exams. The processed information is obtained via Azure AI models, including those hosted by GitHub.

## Features

- Extract text from PDF lecture slides using PdfPig, with options for advanced extraction and exclusion of headers/footers.
- **AI-Powered Preprocessing**: Cleans extracted text by removing metadata, instructor details, contact information, formatting artifacts, and repeated course codes, preserving essential educational content. This step utilizes a configurable AI model.
- **AI-Powered Analysis for Exam Prep**: Analyzes the cleaned slide content to identify key topics, provide detailed explanations of concepts, and extract essential facts or definitions crucial for exam preparation. This step also uses a configurable AI model.
- Send cleaned text to Azure AI Inference API for analysis.
- Support for GitHub-hosted models via Azure AI.
- Configurable AI models, prompts, and temperature settings for both preprocessing and analysis stages, detailed in `appsettings.json`.
- Clean separation of concerns with dependency injection.
- Comprehensive error handling.

## Setup

### 1. Prerequisites

- .NET 9.0 SDK
- Azure AI account or a GitHub personal access token (for GitHub-hosted models).

### 2. Configuration

The application uses `appsettings.json` for configuration and supports user secrets for sensitive data like API keys.

#### API Key and Endpoint:

Set your API key and endpoint using .NET user secrets.

**Option A: Using GitHub Models (Recommended)**

1.  Get a GitHub personal access token with appropriate permissions.
2.  Set your API key:
    ```bash
    dotnet user-secrets set "AzureAI:ApiKey" "your-github-token"
    ```
    The endpoint `https://models.github.ai/inference` is pre-configured in `appsettings.json`.

**Option B: Using Azure AI directly**

1.  Create an Azure AI resource.
2.  Get your endpoint and API key.
3.  Configure using user secrets:
    ```bash
    dotnet user-secrets set "AzureAI:Endpoint" "your-azure-endpoint"
    dotnet user-secrets set "AzureAI:ApiKey" "your-azure-api-key"
    ```

#### Model Configuration:

You can configure the models for preprocessing and analysis in `appsettings.json`. This includes the model name, max tokens, and temperature.

Default Preprocessing Model (`appsettings.json`): `mistral-ai/mistral-medium-2505`
Default Analysis Model (`appsettings.json`): `openai/gpt-4.1`

Example for Analysis Model configuration in `appsettings.json`:

```json
{
  "Analysis": {
    "Model": {
      "ModelName": "openai/gpt-4.1",
      "MaxTokens": 4000,
      "Temperature": 0.2
    },
    // ... other settings
  }
}
```

Refer to `appsettings.json` for the full structure and default values for `PdfExtraction`, `Preprocessing`, and `Analysis` sections.

### 3. Build the application:

Navigate to the `PdfTextAnalyzer` directory:
```bash
cd PdfTextAnalyzer
dotnet build
```

## Usage

Run the application from within the `PdfTextAnalyzer` directory:

```bash
dotnet run -- path/to/your/lecture_slides.pdf
```

Example:

```bash
dotnet run -- "Introduction to AI - Week 1.pdf"
```

## Project Structure

```
study_ai/
├── PdfTextAnalyzer/
│   ├── PdfTextAnalyzer.csproj         # Project file
│   ├── Program.cs                      # Application entry point
│   ├── appsettings.json               # Configuration for models, prompts, PDF extraction, etc.
│   ├── Configuration/                 # (Contains configuration models - if any, based on current structure)
│   │   └── # Configuration-related .cs files
│   ├── Services/
│   │   ├── AiServiceBase.cs           # Base class for AI service interactions
│   │   ├── IPdfAnalysisPipeline.cs    # Interface for the PDF analysis pipeline
│   │   ├── IPdfTextExtractor.cs       # Interface for PDF text extraction
│   │   ├── ITextAnalysisAiService.cs  # Interface for the AI text analysis service
│   │   ├── ITextCleaningService.cs    # Interface for text cleaning service
│   │   ├── PdfAnalysisPipeline.cs     # Implements the PDF analysis pipeline
│   │   ├── PdfTextExtractor.cs        # Implements PDF text extraction using PdfPig
│   │   ├── TextAnalysisService.cs     # Implements AI-based text analysis for exam prep
│   │   └── TextCleaningService.cs     # Implements AI-based text cleaning for slides
│   └── ... (other files like .cs,.json)
├── .gitignore
├── LICENSE
├── README.md                      # This file
└── setup.sh                       # Shell script for initial project setup
```

## Customization

### System and Task Prompts:

The AI's behavior is guided by system messages and task prompts defined in `appsettings.json`.

**Preprocessing Prompts (from `appsettings.json`):**
-   **System Message**: "You are a content extractor specialized in cleaning PDF lecture slides. You remove metadata, contact information, and formatting artifacts while preserving all educational content..."
-   **Task Prompt**: "Clean this PDF slide text by removing:\n- Instructor details, emails, contact info\n- Headers, footers, page numbers, timestamps\n- Repeated course codes and metadata\n\nPreserve..."

**Analysis Prompts (from `appsettings.json`):**
-   **System Message**: "You are an expert teaching assistant. You analyze slide decks and extract key information for exam preparation. You only work with the content provided and do not add external information."
-   **Task Prompt**: "Analyze the following slide content. For each key topic covered, provide:\n- A detailed explanation of the concept\n- The essential facts or definitions students need for the final exam..."

You can modify these in `appsettings.json` to tailor the AI's output.

### PDF Extraction Options:

Configure PDF extraction behavior in `appsettings.json`:
```json
{
  "PdfExtraction": {
    "UseAdvancedExtraction": true,
    "ExcludeHeaderFooter": true
  }
}
```

## Error Handling

The application includes error handling for:
- Missing or invalid PDF files
- API authentication issues
- Network connectivity problems
- Invalid model configurations

## Troubleshooting

1.  **Authentication Error**: Ensure your `AzureAI:ApiKey` is correctly set in user secrets and is valid for the configured `AzureAI:Endpoint`.
2.  **Model Not Found**: Verify the `ModelName` in `appsettings.json` is correct and supported by your chosen AI service/endpoint.
3.  **PDF Reading Error**: Ensure the PDF is not encrypted, corrupted, or inaccessible.
4.  **Network Issues**: Check your internet connection and the AI service endpoint URL.

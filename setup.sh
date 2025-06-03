#!/bin/bash

# PDF Text Analyzer Setup Script

echo "Setting up PDF Text Analyzer..."

# Create project
echo "Creating new console application..."
dotnet new console -n PdfTextAnalyzer
cd PdfTextAnalyzer

# Add NuGet packages
echo "Adding NuGet packages..."
dotnet add package PdfPig
dotnet add package Azure.AI.Inference
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.UserSecrets
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting

# Initialize user secrets
echo "Initializing user secrets..."
dotnet user-secrets init

echo ""
echo "Setup complete! Next steps:"
echo "1. Set your API key: dotnet user-secrets set \"AzureAI:ApiKey\" \"your-api-key\""
echo "2. Place your PDF files in the project directory"
echo "3. Run: dotnet run -- your-file.pdf"
echo ""
echo "For GitHub models, use endpoint: https://models.github.ai/inference"
echo "Get your GitHub token at: https://github.com/settings/tokens"

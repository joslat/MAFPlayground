// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure;

namespace AGUI.Server;

/// <summary>
/// Configuration for Azure OpenAI for AGUI.Server.
/// Uses API Key authentication via environment variables.
/// </summary>
public static class AIConfig
{
    private static readonly Lazy<(Uri Endpoint, AzureKeyCredential KeyCredential, string ModelDeployment)> s_values =
        new(() =>
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var modelDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") 
                ?? "gpt-4o-mini"; // Default deployment name

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "Environment variables 'AZURE_OPENAI_ENDPOINT' and 'AZURE_OPENAI_API_KEY' must be set.");
            }

            return (new Uri(endpoint), new AzureKeyCredential(key), modelDeployment);
        }, isThreadSafe: true);

    /// <summary>
    /// Gets the Azure OpenAI endpoint URI.
    /// </summary>
    public static Uri Endpoint => s_values.Value.Endpoint;

    /// <summary>
    /// Gets the Azure Key Credential for API key authentication.
    /// </summary>
    public static AzureKeyCredential KeyCredential => s_values.Value.KeyCredential;

    /// <summary>
    /// Gets the model deployment name to use.
    /// </summary>
    public static string ModelDeployment => s_values.Value.ModelDeployment;

    /// <summary>
    /// Gets all configuration values as a tuple.
    /// </summary>
    public static (Uri Endpoint, AzureKeyCredential KeyCredential, string ModelDeployment) GetValues() 
        => s_values.Value;
}

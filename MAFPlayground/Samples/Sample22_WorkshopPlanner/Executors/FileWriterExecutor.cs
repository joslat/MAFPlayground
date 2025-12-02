// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that writes the markdown deliverable to disk and provides completion feedback.
/// </summary>
public static class FileWriterExecutor
{
    public static async ValueTask<string> ExecuteAsync(
        MarkdownDeliverable deliverable,
        IWorkflowContext ctx,
        CancellationToken ct,
        string outputDirectory)
    {
        Console.WriteLine("\n💾 [FileWriter] Writing workshop plan to disk...");
        
        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);
        
        var filePath = Path.Combine(outputDirectory, deliverable.Filename);
        await File.WriteAllTextAsync(filePath, deliverable.Content, ct);
        
        Console.WriteLine($"   ✅ Workshop plan saved to: {filePath}");
        Console.WriteLine($"\n{new string('=', 60)}");
        Console.WriteLine("   WORKSHOP GENERATION COMPLETE");
        Console.WriteLine($"{new string('=', 60)}");
        Console.WriteLine($"   File: {deliverable.Filename}");
        Console.WriteLine($"   Location: {Path.GetFullPath(filePath)}");
        Console.WriteLine($"   Size: {new FileInfo(filePath).Length:N0} bytes");
        Console.WriteLine($"{new string('=', 60)}\n");
        
        return filePath;
    }
}

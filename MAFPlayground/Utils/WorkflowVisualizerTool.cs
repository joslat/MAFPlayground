// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH

// Copyright (c) 2025 Jose Luis Latorre
using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Utils;

/// <summary>
/// Utility class for visualizing workflows in Mermaid and DOT (Graphviz) formats.
/// </summary>
public static class WorkflowVisualizerTool
{
    /// <summary>
    /// Prints the workflow as a Mermaid diagram to the console.
    /// </summary>
    /// <param name="workflow">The workflow to visualize</param>
    /// <param name="title">Optional title to display before the diagram</param>
    public static void PrintMermaid(Workflow workflow, string? title = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine(title);
        }

        Console.WriteLine("Mermaid Diagram:");
        Console.WriteLine("=======");
        // ISSUE: Edges do not have labels, so they cannot be rendered properly with a label explaining the condition.
        // Solution: Mermaid syntax for conditional edges should use -->|condition| instead of -.conditional.-->. 
        // also, enable adding a label (or annotation) to the edge in the WorkflowBuilder API, when the edge is created, for conditional edges and also, for non conditional edges. as it can help with self documentation of the workflow.

        // ISSUE: support DevUI for labels on edges. (or annotated edges)

        // ISSUE: conditional edges are not rendered correctly in Mermaid
        //The issue with this Mermaid flowchart is the syntax for conditional edges. Mermaid doesn't use -. conditional .--> for conditional branches.
        // correct syntax is -->|condition| for conditional edges.
        // Example: A -->|yes| B
        // Rendered mermaid on conditional example:
        //flowchart TD
        //    SpamDetectionExecutor["SpamDetectionExecutor (Start)"];
        //    EmailAssistantExecutor["EmailAssistantExecutor"];
        //    SendEmailExecutor["SendEmailExecutor"];
        //    HandleSpamExecutor["HandleSpamExecutor"];
        //    SpamDetectionExecutor -.conditional.-- > EmailAssistantExecutor;
        //    SpamDetectionExecutor -.conditional.-- > HandleSpamExecutor;
        //    EmailAssistantExecutor-- > SendEmailExecutor;

        // Corrected Mermaid syntax for conditional edges:
        //flowchart TD
        //    SpamDetectionExecutor["SpamDetectionExecutor (Start)"];
        //    EmailAssistantExecutor["EmailAssistantExecutor"];
        //    SendEmailExecutor["SendEmailExecutor"];
        //    HandleSpamExecutor["HandleSpamExecutor"];
        //    SpamDetectionExecutor-- >| "not spam" | EmailAssistantExecutor;
        //    SpamDetectionExecutor-- >| "spam" | HandleSpamExecutor;
        //    EmailAssistantExecutor-- > SendEmailExecutor;



        var mermaid = workflow.ToMermaidString();
        Console.WriteLine(mermaid);
        Console.WriteLine("=======");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints the workflow as a DOT (Graphviz) diagram to the console.
    /// </summary>
    /// <param name="workflow">The workflow to visualize</param>
    /// <param name="title">Optional title to display before the diagram</param>
    public static void PrintDot(Workflow workflow, string? title = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine(title);
        }

        Console.WriteLine("DOT Diagram (Graphviz):");
        Console.WriteLine("*** Tip: To export DOT as an image, install Graphviz and pipe the DOT output to 'dot -Tsvg', 'dot -Tpng', etc. ***");
        Console.WriteLine("=======");
        var dotString = workflow.ToDotString();
        Console.WriteLine(dotString);
        Console.WriteLine("=======");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints both Mermaid and DOT diagrams to the console.
    /// </summary>
    /// <param name="workflow">The workflow to visualize</param>
    /// <param name="title">Optional title to display before the diagrams</param>
    public static void PrintAll(Workflow workflow, string? title = null)
    {
        PrintMermaid(workflow, title);
        PrintDot(workflow);
    }

    /// <summary>
    /// Saves the Mermaid diagram to a file.
    /// </summary>
    /// <param name="workflow">The workflow to visualize</param>
    /// <param name="filePath">The file path where the Mermaid diagram will be saved</param>
    public static void SaveMermaid(Workflow workflow, string filePath)
    {
        var mermaid = workflow.ToMermaidString();
        File.WriteAllText(filePath, mermaid);
        Console.WriteLine($"Mermaid diagram saved to: {filePath}");
    }

    /// <summary>
    /// Saves the DOT diagram to a file.
    /// </summary>
    /// <param name="workflow">The workflow to visualize</param>
    /// <param name="filePath">The file path where the DOT diagram will be saved</param>
    public static void SaveDot(Workflow workflow, string filePath)
    {
        var dotString = workflow.ToDotString();
        File.WriteAllText(filePath, dotString);
        Console.WriteLine($"DOT diagram saved to: {filePath}");
    }

    /// <summary>
    /// Saves both Mermaid and DOT diagrams to files.
    /// </summary>
    /// <param name="workflow">The workflow to visualize</param>
    /// <param name="basePath">The base file path (without extension) for saving diagrams</param>
    public static void SaveAll(Workflow workflow, string basePath)
    {
        SaveMermaid(workflow, $"{basePath}.mmd");
        SaveDot(workflow, $"{basePath}.dot");
    }
}
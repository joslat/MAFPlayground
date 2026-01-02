// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using CourseSamples;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                         COURSE SAMPLES - CHAPTER 2                            ║");
Console.WriteLine("║                 Feature Planning Copilot Agent Progression                    ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

while (true)
{
    Console.WriteLine("Select a sample to run:");
    Console.WriteLine();
    Console.WriteLine("  Chapter 2: Feature Planning Copilot");
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  1. Sample 01 - Basic Agent");
    Console.WriteLine("  2. Sample 01 - Interactive Mode");
    Console.WriteLine("  3. Sample 01 - DevUI Mode");
    Console.WriteLine();
    Console.WriteLine("  4. Sample 02 - With Memory");
    Console.WriteLine("  5. Sample 02 - Interactive Mode");
    Console.WriteLine("  6. Sample 02 - DevUI Mode");
    Console.WriteLine();
    Console.WriteLine("  7. Sample 03 - With Tools");
    Console.WriteLine("  8. Sample 03 - Interactive Mode");
    Console.WriteLine("  9. Sample 03 - DevUI Mode");
    Console.WriteLine();
    Console.WriteLine("  10. Sample 04 - With MCP (GitHub)");
    Console.WriteLine("  11. Sample 04 - Interactive Mode");
    Console.WriteLine("  12. Sample 04 - DevUI Mode");
    Console.WriteLine();
    Console.WriteLine("  q. Quit");
    Console.WriteLine();
    Console.Write("Enter your choice: ");

    var input = Console.ReadLine()?.Trim().ToLowerInvariant();

    if (string.IsNullOrEmpty(input))
        continue;

    if (input == "q" || input == "quit")
    {
        Console.WriteLine("\n👋 Goodbye!");
        break;
    }

    Console.WriteLine();

    try
    {
        switch (input)
        {
            case "1":
                await Chapter2_Sample01_FeaturePlanningCopilot.Execute();
                break;
            case "2":
                await Chapter2_Sample01_FeaturePlanningCopilot.ExecuteInteractive();
                break;
            case "3":
                Chapter2_Sample01_FeaturePlanningCopilot.ExecuteWithDevUI();
                break;

            case "4":
                await Chapter2_Sample02_FeaturePlanningWithMemory.Execute();
                break;
            case "5":
                await Chapter2_Sample02_FeaturePlanningWithMemory.ExecuteInteractive();
                break;
            case "6":
                Chapter2_Sample02_FeaturePlanningWithMemory.ExecuteWithDevUI();
                break;

            case "7":
                await Chapter2_Sample03_FeaturePlanningWithTools.Execute();
                break;
            case "8":
                await Chapter2_Sample03_FeaturePlanningWithTools.ExecuteInteractive();
                break;
            case "9":
                Chapter2_Sample03_FeaturePlanningWithTools.ExecuteWithDevUI();
                break;

            case "10":
                await Chapter2_Sample04_FeaturePlanningWithMCP.Execute();
                break;
            case "11":
                await Chapter2_Sample04_FeaturePlanningWithMCP.ExecuteInteractive();
                break;
            case "12":
                Chapter2_Sample04_FeaturePlanningWithMCP.ExecuteWithDevUI();
                break;

            default:
                Console.WriteLine("❌ Invalid choice. Please try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ Error: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(true);
    Console.Clear();
}

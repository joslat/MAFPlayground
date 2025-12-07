// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 11: Claims Processing Workflow
/// 
/// Demonstrates a three-agent claims workflow with:
/// 1. ClaimsUserFacingAgent - Conversational intake to gather claim details
/// 2. ClaimsReadyForProcessingAgent - Validation and enrichment
/// 3. ClaimsProcessingAgent - Final processing and confirmation
/// 
/// Workflow Flow (Self-Contained):
/// ┌──────────────────────┐
/// │ UserInput (Executor) │ → Prompts user for information
/// └──────────┬───────────┘
///            ↓
/// ┌──────────────────────┐
/// │ ClaimsUserFacing     │ → Gathers customer & claim details
/// └──────────┬───────────┘
///            ↓
///    [Has enough info?]
///        ├─ Yes → ClaimsReadyForProcessing
///        └─ No  → UserInput (loop for more details)
///            ↓
/// ┌────────────────────────┐
/// │ ClaimsReadyForProc     │ → Validates & enriches claim
/// └──────────┬─────────────┘
///            ↓
///    [Claim complete?]
///        ├─ Yes → ClaimsProcessing
///        └─ No  → ClaimsIntake (feedback + more details)
///            ↓
/// ┌──────────────────────┐
/// │ ClaimsProcessing     │ → Final confirmation & handoff
/// └──────────────────────┘
/// 
/// Key Features:
/// - Fully self-contained workflow (no external chat loop needed)
/// - UserInputExecutor handles all user interaction
/// - Conversational claim intake with natural language
/// - Customer identification (by ID or name)
/// - Contract resolution and validation
/// - Structured feedback loops
/// - Mock tools for customer and contract data
/// - DevUI compatible (pure workflow orchestration)
/// </summary>
internal static class Demo11_ClaimsWorkflow
{
    private const int MaxIntakeIterations = 15;

    // --------------------- Shared state ---------------------
    private sealed class ClaimWorkflowState
    {
        public int IntakeIteration { get; set; } = 1;
        public ClaimReadinessStatus Status { get; set; } = ClaimReadinessStatus.Draft;
        public CustomerInfo? Customer { get; set; }
        public ClaimDraft ClaimDraft { get; set; } = new();
        public List<ChatMessage> ConversationHistory { get; } = new();
        public string? ContractId { get; set; }
    }

    private static class ClaimStateShared
    {
        public const string Scope = "ClaimStateScope";
        public const string Key = "singleton";
    }

    private static async Task<ClaimWorkflowState> ReadClaimStateAsync(IWorkflowContext context)
    {
        var state = await context.ReadStateAsync<ClaimWorkflowState>(ClaimStateShared.Key, scopeName: ClaimStateShared.Scope);
        return state ?? new ClaimWorkflowState();
    }

    private static ValueTask SaveClaimStateAsync(IWorkflowContext context, ClaimWorkflowState state)
        => context.QueueStateUpdateAsync(ClaimStateShared.Key, state, scopeName: ClaimStateShared.Scope);

    // --------------------- Data contracts ---------------------
    public enum ClaimReadinessStatus
    {
        Draft,
        PendingValidation,
        Ready,
        NeedsMoreInfo
    }

    public sealed class CustomerInfo
    {
        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; } = "";
        
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = "";
        
        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = "";
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
    }

    public sealed class ClaimDraft
    {
        [JsonPropertyName("claim_type")]
        public string ClaimType { get; set; } = ""; // e.g., "Property"
        
        [JsonPropertyName("claim_sub_type")]
        public string ClaimSubType { get; set; } = ""; // e.g., "BikeTheft"
        
        [JsonPropertyName("date_of_loss")]
        public string DateOfLoss { get; set; } = "";
        
        [JsonPropertyName("date_reported")]
        public string DateReported { get; set; } = "";
        
        [JsonPropertyName("short_description")]
        public string ShortDescription { get; set; } = "";
        
        [JsonPropertyName("item_description")]
        public string ItemDescription { get; set; } = ""; // MANDATORY: e.g., "Trek X-Caliber 8, red mountain bike"
        
        [JsonPropertyName("detailed_description")]
        public string DetailedDescription { get; set; } = "";
        
        [JsonPropertyName("purchase_price")]
        public decimal? PurchasePrice { get; set; }
    }

    [Description("Result from the intake agent deciding if ready to proceed")]
    public sealed class IntakeDecision
    {
        [JsonPropertyName("ready_for_validation")]
        [Description("True if enough information gathered to start validation")]
        public bool ReadyForValidation { get; set; }
        
        [JsonPropertyName("response_to_user")]
        [Description("Message to show the user (question if more info needed, or confirmation if ready)")]
        public string ResponseToUser { get; set; } = "";
        
        [JsonPropertyName("customer_id")]
        [Description("Customer ID if provided")]
        public string? CustomerId { get; set; }
        
        [JsonPropertyName("first_name")]
        [Description("Customer first name if provided")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("last_name")]
        [Description("Customer last name if provided")]
        public string? LastName { get; set; }
        
        [JsonPropertyName("claim_draft")]
        [Description("Claim details extracted so far")]
        public ClaimDraft? ClaimDraft { get; set; }
    }

    [Description("Validation result from the ready-for-processing agent")]
    public sealed class ValidationResult
    {
        [JsonPropertyName("ready")]
        [Description("True if claim is complete and ready for processing")]
        public bool Ready { get; set; }
        
        [JsonPropertyName("missing_fields")]
        [Description("List of missing or incomplete fields")]
        public List<string> MissingFields { get; set; } = new();
        
        [JsonPropertyName("blocking_issues")]
        [Description("Critical issues that block processing")]
        public List<string> BlockingIssues { get; set; } = new();
        
        [JsonPropertyName("suggested_questions")]
        [Description("Natural language questions to ask the user to fill gaps")]
        public List<string> SuggestedQuestions { get; set; } = new();
        
        [JsonPropertyName("customer_id")]
        [Description("Resolved customer ID")]
        public string? CustomerId { get; set; }
        
        [JsonPropertyName("contract_id")]
        [Description("Resolved contract ID")]
        public string? ContractId { get; set; }
        
        [JsonPropertyName("normalized_claim_type")]
        [Description("Normalized claim type")]
        public string? NormalizedClaimType { get; set; }
        
        [JsonPropertyName("normalized_claim_sub_type")]
        [Description("Normalized claim sub-type")]
        public string? NormalizedClaimSubType { get; set; }
    }

    public sealed class ProcessedClaim
    {
        public string ClaimId { get; set; } = "";
        public string CustomerId { get; set; } = "";
        public string ContractId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Summary { get; set; } = "";
    }

    // --------------------- Entry point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Demo 11: Claims Processing Workflow ===\n");
        Console.WriteLine("This demo simulates a claims intake and processing workflow.\n");
        Console.WriteLine("The workflow is fully self-contained with a UserInputExecutor for conversation.\n");
        Console.WriteLine("Type 'quit' to exit at any time.\n");

        // Azure OpenAI setup
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = "gpt-4o"; //AIConfig.ModelDeployment; "gpt-4o-mini";
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Build workflow
        var workflow = BuildClaimsWorkflow(chatClient);

        WorkflowVisualizerTool.PrintAll(workflow, "Demo 11: Claims Processing Workflow (Self-Contained)");

        // Execute workflow - it's self-contained now!
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("CLAIMS INTAKE - Interactive Workflow");
        Console.WriteLine(new string('=', 80) + "\n");
        Console.WriteLine("💡 The workflow will prompt you for information as needed.");
        Console.WriteLine("   Simply respond to the agent's questions.\n");

        // Start with "START" signal - UserInputExecutor will prompt
        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, "START");

        bool shouldExit = false;

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentUpdate:
                    // Stream agent output in real-time
                    if (!string.IsNullOrEmpty(agentUpdate.Update.Text))
                    {
                        Console.Write(agentUpdate.Update.Text);
                    }
                    break;

                case WorkflowOutputEvent output:
                    Console.WriteLine("\n\n" + new string('=', 80));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ CLAIM PROCESSED SUCCESSFULLY");
                    Console.ResetColor();
                    Console.WriteLine(new string('=', 80));
                    Console.WriteLine();
                    Console.WriteLine(output.Data);
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    shouldExit = true;
                    break;
            }

            if (shouldExit) break;
        }

        Console.WriteLine("\n✅ Demo 11 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Self-contained workflow with UserInputExecutor");
        Console.WriteLine("  ✓ Conversational claims intake with iterative refinement");
        Console.WriteLine("  ✓ Customer identification (by ID or name lookup)");
        Console.WriteLine("  ✓ Contract resolution and validation");
        Console.WriteLine("  ✓ Structured feedback loops between agents");
        Console.WriteLine($"  ✓ Max iteration safety cap ({MaxIntakeIterations})");
        Console.WriteLine("  ✓ Mock tools for customer and contract services");
        Console.WriteLine("  ✓ DevUI compatible (pure workflow orchestration)\n");
    }

    /// <summary>
    /// Builds the claims workflow with all executors and routing logic.
    /// This method is shared between console mode and DevUI mode.
    /// </summary>
    private static Workflow BuildClaimsWorkflow(IChatClient chatClient, string? workflowName = "claims-workflow")
    {
        // Register mock tools for customer and contract lookup
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(ClaimsMockTools.GetCurrentDate),
            AIFunctionFactory.Create(ClaimsMockTools.GetCustomerProfile),
            AIFunctionFactory.Create(ClaimsMockTools.GetContract)
        };

        // Agents
        var intakeAgent = GetClaimsUserFacingAgent(chatClient, tools);
        var validationAgent = GetClaimsReadyForProcessingAgent(chatClient, tools);
        var processingAgent = GetClaimsProcessingAgent(chatClient);

        // Executors
        var userInputExec = new UserInputExecutor();
        var intakeExec = new ClaimsIntakeExecutor(intakeAgent);
        var validationExec = new ClaimsValidationExecutor(validationAgent);
        var processingExec = new ClaimsProcessingExecutor(processingAgent);

        // Build workflow with UserInputExecutor as entry point
        return new WorkflowBuilder(userInputExec)
            .AddEdge(userInputExec, intakeExec)
            .AddSwitch(intakeExec, sw => sw
                .AddCase<IntakeDecision>(d => d is not null && d.ReadyForValidation, validationExec)
                .AddCase<IntakeDecision>(d => d is not null && !d.ReadyForValidation, userInputExec)) // Loop back for more input
            .AddSwitch(validationExec, sw => sw
                .AddCase<ValidationResult>(v => v is not null && v.Ready, processingExec)
                .AddCase<ValidationResult>(v => v is not null && !v.Ready, intakeExec)) // Loop back to intake (not user input!)
            .WithOutputFrom(processingExec)
            .WithName(workflowName)
            .Build();
    }

    // --------------------- Agent factories ---------------------
    private static AIAgent GetClaimsUserFacingAgent(IChatClient chat, List<AITool> tools) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "ClaimsUserFacingAgent",
            Instructions = """
                You are a friendly and professional claims intake specialist.
                
                Your goal is to gather enough information to start the claims process:
                
                REQUIRED INFORMATION:
                1. Customer Identification:
                   - Either: customer_id (if they know it)
                   - Or: first_name AND last_name (for lookup)
                
                2. Claim Details:
                   - claim_type (e.g., Property, Auto, Health)
                   - claim_sub_type (e.g., BikeTheft, WaterDamage, Accident)
                   - date_of_loss (when the incident occurred)
                   - date_reported (when reporting - ALWAYS call get_current_date tool to get today's date)
                   - short_description (1-2 sentences)
                   - item_description (MANDATORY: specific description of the item - e.g., "Trek X-Caliber 8, red mountain bike")
                   - detailed_description (what happened, including circumstances and purchase price if applicable)
                
                TOOLS AVAILABLE:
                - get_current_date: ALWAYS call this at the start to get today's date for date_reported field
                  Also use it when user says "today", "yesterday", "this morning", etc. for date_of_loss
                - get_customer_profile: Look up customer by name if they don't know their ID
                - get_contract: DON'T use this - let the validation agent handle it
                
                CONVERSATION APPROACH:
                - Start by calling get_current_date to establish today's date
                - Be conversational and empathetic
                - Ask clarifying questions one or two at a time
                - Don't overwhelm the customer with a long list
                - If they provide partial info, acknowledge it and ask for what's missing
                - When user mentions dates relative to today (today, yesterday, last Tuesday), 
                  use get_current_date to calculate the exact date
                - Once you have all required information, confirm and proceed
                - ALWAYS ask for the specific item description (brand, model, color, etc.)
                - Ask for a description of the incident in detail (what happened, where, how)
                - Remember to ask for the purchase price of the item if applicable
                
                EXAMPLE INTERACTION:
                User: "My bike was stolen today"
                You: [Call get_current_date tool]
                You: "I'm sorry to hear your bike was stolen. I've noted that it happened on 
                      Tuesday, January 28, 2025. Could you tell me your name so I can look up your account?"
                
                OUTPUT FORMAT:
                When you have enough information, output a JSON decision with:
                - ready_for_validation: true
                - customer_id, first_name, last_name (what was provided)
                - claim_draft: all the claim details
                - response_to_user: confirmation message
                
                If information is still missing:
                - ready_for_validation: false
                - response_to_user: natural question to gather missing info
                
                Always use the structured JSON format for decision-making.
                """,
            ChatOptions = new()
            {
                Tools = tools,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<IntakeDecision>()
            }
        })
        {

        };
    private static AIAgent GetClaimsReadyForProcessingAgent(IChatClient chat, List<AITool> tools) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "ClaimsReadyForProcessingAgent",
            Instructions = """
                You are a claims validation and enrichment specialist.
                
                Your job is to:
                1. Resolve the customer ID (if only name was provided, use get_customer_profile tool)
                2. Fetch the relevant contract (use get_contract tool)
                3. Normalize claim_type and claim_sub_type
                4. Validate that all mandatory fields are present:
                   - customer_id
                   - contract_id
                   - date_of_loss
                   - date_reported
                   - item_description (MANDATORY - must describe the specific item)
                   - detailed_description
                   - purchase_price (if applicable)
                5. CRITICAL: Verify item_description is NOT empty and contains specific details
                   (e.g., brand, model, color for a bike; make/model for electronics)
                6. Check that detailed_description explains what happened (the incident)
                7. VERIFY that item_description and detailed_description are different:
                   - item_description: WHAT (the item itself)
                   - detailed_description: HOW (what happened to it)
                
                OUTPUT FORMAT:
                Return a ValidationResult JSON with:
                - ready: true/false
                - missing_fields: list of what's missing
                - blocking_issues: critical problems
                - suggested_questions: natural questions for the intake agent to ask user
                - customer_id, contract_id: resolved IDs
                - normalized_claim_type, normalized_claim_sub_type
                
                If everything is complete and valid, set ready=true.
                Otherwise, set ready=false and provide clear feedback.
                
                Use tools to fetch customer and contract data as needed.
                """,
            ChatOptions = new()
            {
                Tools = tools,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<ValidationResult>()
            }
        });

    private static AIAgent GetClaimsProcessingAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are a claims processing agent.
            
            The claim has been validated and is ready for processing.
            
            Your job is to:
            1. Generate a claim ID (format: CLM-YYYYMMDD-XXXX)
            2. Confirm the claim details to the user
            3. Provide next steps
            4. Set status to "ReadyForBackOffice"
            
            Provide a friendly confirmation message with:
            - Claim ID
            - Customer name
            - Claim type
            - Date of loss
            - Brief summary
            - What happens next
            
            Keep it professional but warm.
            """);

    // --------------------- Executors ---------------------

    /// <summary>
    /// UserInputExecutor - Prompts user for input and handles conversation flow.
    /// This makes the workflow self-contained and DevUI compatible.
    /// </summary>
    private sealed class UserInputExecutor :
        ReflectingExecutor<UserInputExecutor>,
        IMessageHandler<string, ChatMessage>,              // Initial start
        IMessageHandler<IntakeDecision, ChatMessage>,      // After intake response
        IMessageHandler<ValidationResult, ChatMessage>     // After validation feedback
    {
        public UserInputExecutor() : base("UserInput") { }

        // Initial kickoff
        public ValueTask<ChatMessage> HandleAsync(
            string _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("👋 Welcome to Claims Intake!");
            Console.WriteLine("Please describe your situation, and I'll help you file a claim.\n");
            return PromptUserAsync();
        }

        // After intake agent responds
        public async ValueTask<ChatMessage> HandleAsync(
            IntakeDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            // Display agent's response
            Console.WriteLine($"\n💬 Agent: {decision.ResponseToUser}\n");

            // If ready for validation, no need for more input - return system message
            if (decision.ReadyForValidation)
            {
                Console.WriteLine("✅ Information complete. Proceeding to validation...\n");
                return new ChatMessage(ChatRole.System, "PROCEED_TO_VALIDATION");
            }

            // Otherwise, prompt for more input
            return await PromptUserAsync();
        }

        // After validation feedback
        public async ValueTask<ChatMessage> HandleAsync(
            ValidationResult validation,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            // Display validation feedback
            Console.WriteLine("\n⚠️  Validation found some missing information:");
            foreach (var field in validation.MissingFields)
            {
                Console.WriteLine($"   • {field}");
            }

            if (validation.SuggestedQuestions.Count > 0)
            {
                Console.WriteLine("\n💬 Agent: " + validation.SuggestedQuestions[0]);
                Console.WriteLine();
            }

            // Prompt for more input to fill gaps
            return await PromptUserAsync();
        }

        private ValueTask<ChatMessage> PromptUserAsync()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("You: ");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim() ?? "";

            // Check for quit
            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\n👋 Goodbye! Your claim was not completed.");
                Environment.Exit(0);
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("⚠️  Please provide some information.");
                return PromptUserAsync();
            }

            return ValueTask.FromResult(new ChatMessage(ChatRole.User, input));
        }
    }

    private sealed class ClaimsIntakeExecutor :
        ReflectingExecutor<ClaimsIntakeExecutor>,
        IMessageHandler<ChatMessage, IntakeDecision>,
        IMessageHandler<ValidationResult, IntakeDecision>
    {
        private readonly AIAgent _agent;
        private readonly AgentThread _thread;
        
        public ClaimsIntakeExecutor(AIAgent agent) : base("ClaimsIntakeExecutor")
        {
            _agent = agent;
            _thread = _agent.GetNewThread(); // Create thread for conversation memory
        }

        // Initial intake
        public async ValueTask<IntakeDecision> HandleAsync(
            ChatMessage message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return await ProcessIntakeAsync(message, null, context, cancellationToken);
        }

        // Feedback from validation
        public async ValueTask<IntakeDecision> HandleAsync(
            ValidationResult validation,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var feedback = $"Validation feedback:\n" +
                          $"Missing: {string.Join(", ", validation.MissingFields)}\n" +
                          $"Questions to ask:\n{string.Join("\n", validation.SuggestedQuestions)}";
            
            return await ProcessIntakeAsync(new ChatMessage(ChatRole.User, feedback), validation, context, cancellationToken);
        }

        private async Task<IntakeDecision> ProcessIntakeAsync(
            ChatMessage message,
            ValidationResult? validation,
            IWorkflowContext context,
            CancellationToken cancellationToken)
        {
            var state = await ReadClaimStateAsync(context);
            
            Console.WriteLine($"\n=== ClaimsIntake (Iteration {state.IntakeIteration}) ===\n");

            // Build context for the agent
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Current state:");
            contextBuilder.AppendLine($"Customer: {(state.Customer != null ? $"{state.Customer.FirstName} {state.Customer.LastName} (ID: {state.Customer.CustomerId})" : "Unknown")}");
            contextBuilder.AppendLine($"Claim Type: {state.ClaimDraft.ClaimType}");
            contextBuilder.AppendLine($"Date of Loss: {state.ClaimDraft.DateOfLoss}");
            
            if (validation != null)
            {
                contextBuilder.AppendLine("\nValidation Feedback:");
                contextBuilder.AppendLine(message.Text);
            }
            else
            {
                contextBuilder.AppendLine("\nUser Message:");
                contextBuilder.AppendLine(message.Text);
            }

            var prompt = contextBuilder.ToString();

            // Use non-streaming with thread for conversation memory
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[Processing with conversation memory...]");
            Console.ResetColor();
            
            var response = await _agent.RunAsync(prompt, _thread, cancellationToken: cancellationToken);
            var decision = response.Deserialize<IntakeDecision>(System.Text.Json.JsonSerializerOptions.Web);
            
            // Display only the human-friendly response (not the raw JSON)
            if (!string.IsNullOrEmpty(decision.ResponseToUser))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Agent: {decision.ResponseToUser}");
                Console.ResetColor();
                Console.WriteLine();
            }

            // Update state
            if (decision.CustomerId != null)
            {
                state.Customer = new CustomerInfo
                {
                    CustomerId = decision.CustomerId,
                    FirstName = decision.FirstName ?? "",
                    LastName = decision.LastName ?? ""
                };
            }
            
            if (decision.ClaimDraft != null)
            {
                state.ClaimDraft = decision.ClaimDraft;
            }

            if (decision.ReadyForValidation)
            {
                state.Status = ClaimReadinessStatus.PendingValidation;
            }
            else
            {
                state.IntakeIteration++;
                if (state.IntakeIteration >= MaxIntakeIterations)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️ Max intake iterations ({MaxIntakeIterations}) reached");
                    Console.ResetColor();
                    decision.ReadyForValidation = true; // Force proceed
                }
            }

            state.ConversationHistory.Add(message);
            await SaveClaimStateAsync(context, state);

            return decision;
        }
    }

    private sealed class ClaimsValidationExecutor :
        ReflectingExecutor<ClaimsValidationExecutor>,
        IMessageHandler<IntakeDecision, ValidationResult>
    {
        private readonly AIAgent _agent;
        private readonly AgentThread _thread;
        
        public ClaimsValidationExecutor(AIAgent agent) : base("ClaimsValidationExecutor")
        {
            _agent = agent;
            _thread = _agent.GetNewThread(); // Create thread for conversation memory
        }

        public async ValueTask<ValidationResult> HandleAsync(
            IntakeDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadClaimStateAsync(context);
            
            Console.WriteLine("=== ClaimsValidation ===\n");

            var prompt = $"""
                Validate this claim:
                
                Customer ID: {decision.CustomerId}
                Customer Name: {decision.FirstName} {decision.LastName}
                
                Claim Details:
                {JsonSerializer.Serialize(decision.ClaimDraft, new JsonSerializerOptions { WriteIndented = true })}
                
                Tasks:
                1. If customer_id is missing, look up by name using get_customer_profile
                2. Fetch contract using get_contract
                3. Validate all mandatory fields
                4. Normalize claim types
                5. Generate ValidationResult
                """;

            // Use non-streaming with thread for conversation memory
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[Validating with conversation memory...]");
            Console.ResetColor();
            
            var response = await _agent.RunAsync(prompt, _thread, cancellationToken: cancellationToken);
            var validation = response.Deserialize<ValidationResult>(System.Text.Json.JsonSerializerOptions.Web);
            
            // Display validation summary (not the raw JSON)
            Console.ForegroundColor = ConsoleColor.Magenta;
            if (validation.Ready)
            {
                Console.WriteLine("✅ Validation passed! Claim is complete.");
                Console.ResetColor();
                Console.WriteLine();
                
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                // ========== 1. CLAIM COMPOSITION (What we've built) ==========
                Console.WriteLine(new string('─', 80));
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("📋 CLAIM COMPOSITION (Gathered Information)");
                Console.ResetColor();
                Console.WriteLine(new string('─', 80));
                Console.WriteLine();
                
                // Build the complete claim from state
                var completeClaim = new
                {
                    customer = state.Customer,
                    contract_id = state.ContractId,
                    claim_details = state.ClaimDraft,
                    status = state.Status.ToString(),
                    intake_iterations = state.IntakeIteration
                };
                
                var claimJson = JsonSerializer.Serialize(completeClaim, jsonOptions);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(claimJson);
                Console.ResetColor();
                Console.WriteLine();
                
                // ========== 2. CLAIM VALIDATION (What the validator resolved) ==========
                Console.WriteLine(new string('─', 80));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ CLAIM VALIDATION (Validator Output)");
                Console.ResetColor();
                Console.WriteLine(new string('─', 80));
                Console.WriteLine();
                
                var validationJson = JsonSerializer.Serialize(validation, jsonOptions);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(validationJson);
                Console.ResetColor();
                Console.WriteLine();
                
                // ========== 3. CONVERSATION HISTORY (Legal/Audit Trail) ==========
                Console.WriteLine(new string('─', 80));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("💬 CONVERSATION HISTORY (Audit Trail)");
                Console.ResetColor();
                Console.WriteLine(new string('─', 80));
                Console.WriteLine();
                
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Total messages exchanged: {state.ConversationHistory.Count}");
                Console.WriteLine();
                
                int msgNum = 1;
                foreach (var msg in state.ConversationHistory)
                {
                    var roleColor = msg.Role.Value switch
                    {
                        "user" => ConsoleColor.Cyan,
                        "assistant" => ConsoleColor.Yellow,
                        "system" => ConsoleColor.DarkGray,
                        _ => ConsoleColor.White
                    };
                    
                    Console.ForegroundColor = roleColor;
                    Console.WriteLine($"[{msgNum}] {msg.Role.Value.ToUpperInvariant()}:");
                    Console.ResetColor();
                    Console.WriteLine($"    {msg.Text}");
                    Console.WriteLine();
                    msgNum++;
                }
                Console.ResetColor();
                
                Console.WriteLine(new string('─', 80));
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"⚠️  Validation found {validation.MissingFields.Count} missing fields.");
                if (validation.BlockingIssues.Count > 0)
                {
                    Console.WriteLine($"🚫 Blocking issues: {string.Join(", ", validation.BlockingIssues)}");
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            // Update state with resolved data
            if (validation.CustomerId != null && state.Customer != null)
            {
                state.Customer.CustomerId = validation.CustomerId;
            }
            
            state.ContractId = validation.ContractId;

            if (validation.Ready)
            {
                state.Status = ClaimReadinessStatus.Ready;
            }
            else
            {
                state.Status = ClaimReadinessStatus.NeedsMoreInfo;
            }

            await SaveClaimStateAsync(context, state);

            return validation;
        }
    }

    private sealed class ClaimsProcessingExecutor :
        ReflectingExecutor<ClaimsProcessingExecutor>,
        IMessageHandler<ValidationResult, ChatMessage>
    {
        private readonly AIAgent _agent;
        public ClaimsProcessingExecutor(AIAgent agent) : base("ClaimsProcessingExecutor") => _agent = agent;

        public async ValueTask<ChatMessage> HandleAsync(
            ValidationResult validation,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadClaimStateAsync(context);
            
            Console.WriteLine("=== ClaimsProcessing ===\n");

            var claimId = $"CLM-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var prompt = $"""
                Process this approved claim:
                
                Claim ID: {claimId}
                Customer: {state.Customer?.FirstName} {state.Customer?.LastName} (ID: {state.Customer?.CustomerId})
                Contract ID: {state.ContractId}
                Claim Type: {validation.NormalizedClaimType} - {validation.NormalizedClaimSubType}
                Date of Loss: {state.ClaimDraft.DateOfLoss}
                
                Description:
                {state.ClaimDraft.DetailedDescription}
                
                Provide a confirmation message for the customer.
                """;

            var sb = new StringBuilder();
            await foreach (var up in _agent.RunStreamingAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                }
            }

            return new ChatMessage(ChatRole.Assistant, sb.ToString());
        }
    }
}

// --------------------- Mock Tools ---------------------
internal static class ClaimsMockTools
{
    [Description("Get the current date and time")]
    public static string GetCurrentDate()
    {
        Console.WriteLine($"🔧 Tool called: get_current_date()");
        
        var now = DateTime.Now;
        return JsonSerializer.Serialize(new
        {
            current_date = now.ToString("yyyy-MM-dd"),
            current_time = now.ToString("HH:mm:ss"),
            day_of_week = now.DayOfWeek.ToString(),
            formatted = now.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt")
        });
    }

    [Description("Get customer profile by first and last name")]
    public static string GetCustomerProfile(
        [Description("Customer's first name")] string firstName,
        [Description("Customer's last name")] string lastName)
    {
        Console.WriteLine($"🔧 Tool called: get_customer_profile('{firstName}', '{lastName}')");
        
        // Mock data
        var mockCustomers = new Dictionary<string, (string id, string email)>
        {
            ["john smith"] = ("CUST-10001", "john.smith@example.com"),
            ["jane doe"] = ("CUST-10002", "jane.doe@example.com"),
            ["alice johnson"] = ("CUST-10003", "alice.johnson@example.com")
        };

        var key = $"{firstName} {lastName}".ToLowerInvariant();
        if (mockCustomers.TryGetValue(key, out var customer))
        {
            return JsonSerializer.Serialize(new
            {
                customer_id = customer.id,
                first_name = firstName,
                last_name = lastName,
                email = customer.email
            });
        }

        return JsonSerializer.Serialize(new { error = "Customer not found" });
    }

    [Description("Get insurance contract details for a customer")]
    public static string GetContract(
        [Description("Customer ID")] string customerId)
    {
        Console.WriteLine($"🔧 Tool called: get_contract('{customerId}')");
        
        // Mock contract data
        var mockContracts = new Dictionary<string, object>
        {
            ["CUST-10001"] = new
            {
                contract_id = "CONTRACT-P-5001",
                customer_id = "CUST-10001",
                contract_type = "Property",
                coverage = new[] { "BikeTheft", "WaterDamage", "Fire" },
                status = "Active",
                start_date = "2023-01-01"
            },
            ["CUST-10002"] = new
            {
                contract_id = "CONTRACT-A-5002",
                customer_id = "CUST-10002",
                contract_type = "Auto",
                coverage = new[] { "Collision", "Theft" },
                status = "Active",
                start_date = "2022-06-15"
            },
            ["CUST-10003"] = new
            {
                contract_id = "CONTRACT-P-5003",
                customer_id = "CUST-10003",
                contract_type = "Property",
                coverage = new[] { "BikeTheft", "Burglary" },
                status = "Active",
                start_date = "2023-03-10"
            }
        };

        if (mockContracts.TryGetValue(customerId, out var contract))
        {
            return JsonSerializer.Serialize(contract);
        }

        return JsonSerializer.Serialize(new { error = "Contract not found" });
    }
}

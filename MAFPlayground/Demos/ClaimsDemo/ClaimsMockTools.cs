// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text.Json;

namespace MAFPlayground.Demos.ClaimsDemo;

/// <summary>
/// Mock tools shared across Claims demos (Demo11, Demo12, etc.)
/// 
/// These tools simulate real backend services:
/// - Claims Intake (Demo11): Customer lookup, contract retrieval, date services
/// - Fraud Detection (Demo12): Marketplace checks, claim history, risk profiling
/// 
/// In production, these would be replaced with actual API calls.
/// </summary>
internal static class ClaimsMockTools
{
    // =====================================================================
    // CLAIMS INTAKE TOOLS (Used by Demo11)
    // =====================================================================
    
    [Description("Get the current date and time")]
    public static string GetCurrentDate()
    {
        Console.WriteLine($"?? Tool called: get_current_date()");
        
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
        Console.WriteLine($"?? Tool called: get_customer_profile('{firstName}', '{lastName}')");
        
        // Mock customer database
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
        Console.WriteLine($"?? Tool called: get_contract('{customerId}')");
        
        // Mock contract database
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
    
    // =====================================================================
    // FRAUD DETECTION TOOLS (Used by Demo12)
    // =====================================================================
    
    [Description("Check if stolen property is listed for sale on online marketplaces")]
    public static string CheckOnlineMarketplaces(
        [Description("Description of the stolen item")] string itemDescription,
        [Description("Approximate value of the item")] decimal itemValue)
    {
        Console.WriteLine($"?? Tool called: check_online_marketplaces('{itemDescription}', {itemValue})");
        
        // Mock logic: High-value items are "found" online (suspicious!)
        var found = itemValue > 1000;
        var fraudIndicator = found ? 85 : 15;

        var marketplaces = new[] { "ricardo.ch", "anibis.ch", "ebay.ch", "facebook_marketplace" };
        var result = new
        {
            marketplaces_checked = marketplaces,
            item_found = found,
            matching_listings = found
                ? new[] { $"Ricardo.ch listing: Similar item, CHF {itemValue * 0.8m:F0} - Listed 3 days after reported theft" }
                : Array.Empty<string>(),
            fraud_indicator = fraudIndicator
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Retrieve customer's claim history and fraud score")]
    public static string GetCustomerClaimHistory(
        [Description("Customer ID")] string customerId)
    {
        Console.WriteLine($"?? Tool called: get_customer_claim_history('{customerId}')");
        
        // Mock customer data
        var result = customerId switch
        {
            "CUST-10001" => new
            {
                total_claims = 5,
                claims_last_12_months = 3,
                previous_fraud_flags = 1,
                customer_fraud_score = 65,
                claim_history = new[]
                {
                    "2024-12: BikeTheft - APPROVED - $800",
                    "2024-09: WaterDamage - APPROVED - $1,500",
                    "2024-06: BikeTheft - FLAGGED (duplicate pattern) - $900",
                    "2023-12: Burglary - APPROVED - $2,000",
                    "2023-08: BikeTheft - APPROVED - $600"
                }
            },
            "CUST-10002" => new
            {
                total_claims = 2,
                claims_last_12_months = 1,
                previous_fraud_flags = 0,
                customer_fraud_score = 20,
                claim_history = new[]
                {
                    "2024-10: Collision - APPROVED - $3,000",
                    "2022-03: Theft - APPROVED - $500"
                }
            },
            _ => new
            {
                total_claims = 1,
                claims_last_12_months = 1,
                previous_fraud_flags = 0,
                customer_fraud_score = 10,
                claim_history = new[] { "First claim" }
            }
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Analyze transaction risk profile for fraud indicators")]
    public static string GetTransactionRiskProfile(
        [Description("Claim amount")] decimal claimAmount,
        [Description("Date of loss")] string dateOfLoss)
    {
        Console.WriteLine($"?? Tool called: get_transaction_risk_profile({claimAmount}, '{dateOfLoss}')");
        
        // Mock logic: High value + recent = high risk
        var redFlags = new List<string>();
        var highValue = claimAmount > 1000;
        var recent = DateTime.TryParse(dateOfLoss, out var lossDate) &&
                    (DateTime.Now - lossDate).TotalDays < 7;

        if (highValue) redFlags.Add("High value claim (>$1000)");
        if (recent) redFlags.Add("Claim filed immediately after incident");
        if (highValue && recent) redFlags.Add("High value + immediate filing pattern");

        var riskScore = redFlags.Count * 25;

        var result = new
        {
            amount_percentile = highValue ? 85 : 35,
            timing_anomaly = recent,
            red_flags = redFlags.ToArray(),
            transaction_risk_score = riskScore
        };

        return JsonSerializer.Serialize(result);
    }
}

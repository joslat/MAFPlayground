// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text.Json;

namespace MAFPlayground.Demos.ClaimsDemo;

/// <summary>
/// Mock tools shared across Claims demos (Demo11, Demo12, etc.)
/// 
/// These tools simulate real backend services:
/// - GetCurrentDate: Returns current date/time
/// - GetCustomerProfile: Looks up customer by name
/// - GetContract: Retrieves insurance contract details
/// 
/// In production, these would be replaced with actual API calls.
/// </summary>
internal static class ClaimsMockTools
{
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
}

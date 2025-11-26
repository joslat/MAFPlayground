// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AGUI.Server.Agents;

/// <summary>
/// AI agent with function tools - demonstrates backend tool rendering.
/// </summary>
public static class AgentWithTools
{
    public static AIAgent Create(IChatClient chatClient, Microsoft.AspNetCore.Http.Json.JsonOptions jsonOptions)
    {
        // Create tools with JSON serializer options
        AITool[] tools =
        [
            AIFunctionFactory.Create(GetWeather, serializerOptions: jsonOptions.SerializerOptions),
            AIFunctionFactory.Create(SearchRestaurants, serializerOptions: jsonOptions.SerializerOptions),
            AIFunctionFactory.Create(GetCurrentTime, serializerOptions: jsonOptions.SerializerOptions)
        ];

        return chatClient.CreateAIAgent(
            name: "TravelAssistant",
            instructions: """
                You are a helpful travel assistant with access to weather information, restaurant search, and time services.
                Use the available tools to help users plan their trips and answer their questions.
                Be friendly, informative, and proactive in using the tools when relevant.
                """,
            tools: tools);
    }

    public static string GetDescription()
    {
        return "Travel assistant with weather, restaurant search, and time tools";
    }

    // ====================================
    // Tool Definitions
    // ====================================

    /// <summary>
    /// Get the current weather for a location.
    /// </summary>
    [Description("Get the current weather for a location")]
    private static WeatherResponse GetWeather(
        [Description("The city and country, e.g., 'Paris, France'")] string location)
    {
        // Simulated weather data
        var random = new Random();
        var conditions = new[] { "Sunny", "Partly cloudy", "Cloudy", "Rainy", "Windy", "Stormy with chance of meatballs" };
        var condition = conditions[random.Next(conditions.Length)];
        var temperature = random.Next(15, 30);

        return new WeatherResponse
        {
            Location = location,
            Condition = condition,
            Temperature = temperature,
            TemperatureUnit = "Celsius",
            Humidity = random.Next(40, 80),
            WindSpeed = random.Next(5, 25),
            WindSpeedUnit = "km/h"
        };
    }

    /// <summary>
    /// Search for restaurants in a specific location.
    /// </summary>
    [Description("Search for restaurants in a location with optional cuisine filter")]
    private static RestaurantSearchResponse SearchRestaurants(
        [Description("The restaurant search request")] RestaurantSearchRequest request)
    {
        // Simulated restaurant data
        var cuisineType = string.IsNullOrEmpty(request.Cuisine) || request.Cuisine.Equals("any", StringComparison.OrdinalIgnoreCase)
            ? "International"
            : request.Cuisine;

        var restaurants = new List<RestaurantInfo>();

        // Generate some sample restaurants
        var random = new Random();
        var names = new[]
        {
            "The Golden Fork", "Bella Italia", "Spice Garden", "Le Petit Bistro",
            "Sushi Master", "The Green Leaf", "Steakhouse Prime", "Curry Palace"
        };

        for (int i = 0; i < 3; i++)
        {
            restaurants.Add(new RestaurantInfo
            {
                Name = names[random.Next(names.Length)],
                Cuisine = i == 0 ? cuisineType : names[random.Next(names.Length)].Split(' ')[0],
                Rating = Math.Round(3.5 + random.NextDouble() * 1.5, 1),
                PriceRange = new string('$', random.Next(1, 4)),
                Address = $"{random.Next(100, 999)} Main St, {request.Location}"
            });
        }

        return new RestaurantSearchResponse
        {
            Location = request.Location,
            Cuisine = request.Cuisine ?? "any",
            ResultCount = restaurants.Count,
            Results = restaurants.ToArray()
        };
    }

    /// <summary>
    /// Get the current time in a specific timezone or location.
    /// </summary>
    [Description("Get the current time for a location or timezone")]
    private static TimeResponse GetCurrentTime(
        [Description("The city name or timezone (e.g., 'New York' or 'America/New_York')")] string location)
    {
        // For simplicity, using UTC and adding some offset based on location
        var utcNow = DateTime.UtcNow;
        var offset = GetTimezoneOffset(location);
        var localTime = utcNow.AddHours(offset);

        return new TimeResponse
        {
            Location = location,
            CurrentTime = localTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Timezone = GetTimezoneName(location),
            UtcOffset = offset > 0 ? $"+{offset}" : offset.ToString(),
            IsDaylightSaving = false // Simplified
        };
    }

    // Helper methods
    private static int GetTimezoneOffset(string location)
    {
        // Simplified timezone mapping
        return location.ToLowerInvariant() switch
        {
            var l when l.Contains("new york") || l.Contains("america/new_york") => -5,
            var l when l.Contains("los angeles") || l.Contains("america/los_angeles") => -8,
            var l when l.Contains("london") || l.Contains("europe/london") => 0,
            var l when l.Contains("paris") || l.Contains("europe/paris") => 1,
            var l when l.Contains("tokyo") || l.Contains("asia/tokyo") => 9,
            var l when l.Contains("sydney") || l.Contains("australia/sydney") => 10,
            _ => 0 // Default to UTC
        };
    }

    private static string GetTimezoneName(string location)
    {
        return location.ToLowerInvariant() switch
        {
            var l when l.Contains("new york") => "America/New_York (EST/EDT)",
            var l when l.Contains("los angeles") => "America/Los_Angeles (PST/PDT)",
            var l when l.Contains("london") => "Europe/London (GMT/BST)",
            var l when l.Contains("paris") => "Europe/Paris (CET/CEST)",
            var l when l.Contains("tokyo") => "Asia/Tokyo (JST)",
            var l when l.Contains("sydney") => "Australia/Sydney (AEDT/AEST)",
            _ => "UTC"
        };
    }
}

// ====================================
// Request/Response Types
// ====================================

/// <summary>
/// Weather information response.
/// </summary>
public sealed class WeatherResponse
{
    public string Location { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public int Temperature { get; set; }
    public string TemperatureUnit { get; set; } = "Celsius";
    public int Humidity { get; set; }
    public int WindSpeed { get; set; }
    public string WindSpeedUnit { get; set; } = "km/h";
}

/// <summary>
/// Restaurant search request.
/// </summary>
public sealed class RestaurantSearchRequest
{
    public string Location { get; set; } = string.Empty;
    public string? Cuisine { get; set; } = "any";
}

/// <summary>
/// Restaurant search response.
/// </summary>
public sealed class RestaurantSearchResponse
{
    public string Location { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public RestaurantInfo[] Results { get; set; } = [];
}

/// <summary>
/// Restaurant information.
/// </summary>
public sealed class RestaurantInfo
{
    public string Name { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string PriceRange { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

/// <summary>
/// Time information response.
/// </summary>
public sealed class TimeResponse
{
    public string Location { get; set; } = string.Empty;
    public string CurrentTime { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string UtcOffset { get; set; } = string.Empty;
    public bool IsDaylightSaving { get; set; }
}

// ====================================
// JSON Serialization Context
// ====================================

/// <summary>
/// JSON serialization context for source generation (required for complex types).
/// </summary>
[JsonSerializable(typeof(WeatherResponse))]
[JsonSerializable(typeof(RestaurantSearchRequest))]
[JsonSerializable(typeof(RestaurantSearchResponse))]
[JsonSerializable(typeof(RestaurantInfo))]
[JsonSerializable(typeof(TimeResponse))]
public sealed partial class ToolsJsonSerializerContext : JsonSerializerContext
{
}

// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using System.ComponentModel;
using System.Text.Json;

namespace MAFPlayground.Demos;

/// <summary>
/// Tools for the Super-Powered Personal Assistant demo.
/// Provides functions for calendar management, restaurant booking, weather checking, and transportation.
/// </summary>
internal static class SuperPoweredAssistantTools
{
    // Mock restaurant database
    private static List<Restaurant> _restaurants = new()
    {
        new("La Terrazza", "Italian", true, "Authentic pasta & pizza with rooftop terrace"),
        new("Sushi Haven", "Japanese", false, "Fresh sushi and sashimi, modern interior"),
        new("Garden Bistro", "French", true, "Classic French cuisine with garden seating"),
        new("Curry House", "Indian", false, "Spicy curries and tandoori specialties")
    };

    private static List<CalendarEvent> _calendar = new()
    {
        new("09:00", "Team Standup"),
        new("10:00", "Code Review"),
        new("14:00", "Client Demo")
    };

    // ====================================
    // Tool Functions
    // ====================================

    [Description("Gets the current date")]
    public static string GetCurrentDate()
    {
        var result = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
        Console.WriteLine($"🔧 Tool called: GetCurrentDate() → {result}");
        return result;
    }

    [Description("Gets today's weather forecast")]
    public static string GetWeather()
    {
        var result = "Sunny and warm, 24°C. Perfect for outdoor dining!";
        Console.WriteLine($"🔧 Tool called: GetWeather() → {result}");
        return result;
    }

    [Description("Gets today's agenda with all scheduled events")]
    public static string GetTodayAgenda()
    {
        var agenda = JsonSerializer.Serialize(new[]
        {
            new { Time = "09:00", Event = "Team Standup", Duration = "30 min" },
            new { Time = "10:00", Event = "Code Review", Duration = "1 hour" },
            new { Time = "14:00", Event = "Client Demo", Duration = "1 hour" }
        });
        Console.WriteLine($"🔧 Tool called: GetTodayAgenda()");
        return agenda;
    }

    [Description("Search for restaurants by food type and seating preference")]
    public static string SearchRestaurants(
        [Description("Type of food (e.g., Italian, Japanese, French, Indian, or 'any')")] string foodType = "any",
        [Description("Whether outdoor seating is required")] bool outdoorSeating = false)
    {
        Console.WriteLine($"🔧 Tool called: SearchRestaurants(foodType: '{foodType}', outdoorSeating: {outdoorSeating})");

        var results = _restaurants
            .Where(r => foodType == "any" || r.FoodType.Contains(foodType, StringComparison.OrdinalIgnoreCase))
            .Where(r => !outdoorSeating || r.HasOutdoorSeating)
            .Select(r => $"🍽️ {r.Name} - {r.FoodType}\n   {(r.HasOutdoorSeating ? "☀️ Outdoor seating" : "🏠 Indoor only")}\n   {r.Description}")
            .ToList();

        return results.Any()
            ? string.Join("\n\n", results)
            : "No restaurants found matching your criteria.";
    }

    [Description("Book a table at a restaurant for the specified time")]
    public static string BookRestaurant(
        [Description("Name of the restaurant to book")] string restaurantName,
        [Description("Time for the booking (e.g., '12:30')")] string time)
    {
        Console.WriteLine($"🔧 Tool called: BookRestaurant(restaurant: '{restaurantName}', time: '{time}')");

        return $"✅ Booking confirmed!\n" +
               $"Restaurant: {restaurantName}\n" +
               $"Time: {time}\n" +
               $"Confirmation #: {Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    [Description("Book transportation (taxi, Uber, or Lyft) for a specific pickup time")]
    public static string BookTransport(
        [Description("Type of transport: 'taxi', 'uber', or 'lyft'")] string transportType,
        [Description("Pickup time (e.g., '12:15')")] string pickupTime)
    {
        Console.WriteLine($"🔧 Tool called: BookTransport(type: '{transportType}', pickupTime: '{pickupTime}')");

        // 80% success rate, 20% failure rate
        var random = new Random();
        var isSuccessful = random.Next(100) < 80;

        if (isSuccessful)
        {
            return $"🚗 Reserved - Your {transportType} will arrive at {pickupTime}!\n" +
                   $"Confirmation #: {Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
        else
        {
            return $"❌ Sorry, I found no possible pickup for {transportType} at {pickupTime}.\n" +
                   $"Please try a different time or transport type.";
        }
    }

    [Description("Add an event to today's calendar")]
    public static string AddToCalendar(
        [Description("Name of the event to add")] string eventName,
        [Description("Time for the event (e.g., '12:30')")] string time)
    {
        Console.WriteLine($"🔧 Tool called: AddToCalendar(event: '{eventName}', time: '{time}')");

        _calendar.Add(new(time, eventName));
        _calendar = _calendar.OrderBy(e => e.Time).ToList();
        return $"✅ Added '{eventName}' to your calendar at {time}";
    }

    [Description("Display today's full calendar in markdown format")]
    public static string PrintCalendarAsMarkdown()
    {
        Console.WriteLine($"🔧 Tool called: PrintCalendarAsMarkdown()");

        var markdown = $"# 📅 Today's Schedule - {GetCurrentDate()}\n\n";
        markdown += $"Weather: {GetWeather()}\n\n";
        markdown += "## Events\n\n";

        foreach (var evt in _calendar)
        {
            markdown += $"- **{evt.Time}** - {evt.Event}\n";
        }

        return markdown;
    }

    // ====================================
    // Helper Records
    // ====================================

    private record Restaurant(string Name, string FoodType, bool HasOutdoorSeating, string Description);
    private record CalendarEvent(string Time, string Event);
}
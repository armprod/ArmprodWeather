using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ArmprodWeather.Models;

namespace ArmprodWeather.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient = new();

    public WeatherService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ArmprodWeatherApp/1.0");
    }

    // Loads the current weather forecast for the specified coordinates.
    public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon)
    {
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        string url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&hourly=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<WeatherResponse>(json, options);
    }

    // It searches for towns by name, taking into account the selected API language.
    public async Task<GeocodingResponse?> SearchLocationsAsync(string query, string langCode = "cs")
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=5&language={langCode}&format=json";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<GeocodingResponse>(json, options);
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ArmprodWeather.Models;

namespace ArmprodWeather.Services;

public class LocationService
{
    private readonly HttpClient _httpClient = new();

    public LocationService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ArmprodWeatherApp/1.0");
    }

    // Searching cities with open-meteo
    public async Task<List<LocationItem>> SearchCityAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) 
            return new List<LocationItem>();

        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=5&language=cs&format=json";
        
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<LocationItem>();

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<GeocodingResponse>(json, options);

        return data?.Results ?? new List<LocationItem>();
    }
}
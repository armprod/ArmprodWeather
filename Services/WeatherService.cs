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

    public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon)
    {
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        // Current state, hour forecast, 7-day forecast
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&hourly=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<WeatherResponse>(json, options);
    }
}
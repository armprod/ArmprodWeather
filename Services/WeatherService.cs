using System;
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

    // Loads Weather and Air values in written coordinates
    public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon)
    {
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        // URL for Main Weather API
        string weatherUrl = $"https://api.open-meteo.com/v1/forecast?" +
                    $"latitude={latStr}&" +
                    $"longitude={lonStr}&" +
                    $"current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,cloud_cover,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m,uv_index,visibility,dew_point_2m&" +
                    $"hourly=temperature_2m,apparent_temperature,weather_code,precipitation_probability,uv_index,surface_pressure&" +
                    $"daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,daylight_duration,sunshine_duration,uv_index_max,precipitation_probability_max,precipitation_sum,moonrise,moonset,moon_phase&" +
                    $"timezone=auto";

        // URL for Air Quality API
        string airQualityUrl = $"https://air-quality-api.open-meteo.com/v1/air-quality?" +
                               $"latitude={latStr}&" +
                               $"longitude={lonStr}&" +
                               $"current=us_aqi,pm2_5,pm10";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var weatherTask = _httpClient.GetAsync(weatherUrl);
            var airQualityTask = _httpClient.GetAsync(airQualityUrl);

            await Task.WhenAll(weatherTask, airQualityTask);

            var weatherResp = await weatherTask;
            var airQualityResp = await airQualityTask;

            weatherResp.EnsureSuccessStatusCode();

            var weatherJson = await weatherResp.Content.ReadAsStringAsync();
            var weatherResult = JsonSerializer.Deserialize<WeatherResponse>(weatherJson, options);

            if (weatherResult != null && airQualityResp.IsSuccessStatusCode)
            {
                var aqJson = await airQualityResp.Content.ReadAsStringAsync();
                var aqResult = JsonSerializer.Deserialize<AirQualityResponse>(aqJson, options);
                weatherResult.AirQuality = aqResult?.Current;
            }

            return weatherResult;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
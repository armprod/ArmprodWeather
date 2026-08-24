using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ArmprodWeather.Models;

public class WeatherResponse
{
    [JsonPropertyName("current")]
    public CurrentData? Current { get; set; }

    [JsonPropertyName("hourly")]
    public HourlyData? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public DailyData? Daily { get; set; }
}

public class CurrentData
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public int Humidity { get; set; }
}

public class HourlyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = new();
}

public class DailyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double> TempMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double> TempMin { get; set; } = new();
}
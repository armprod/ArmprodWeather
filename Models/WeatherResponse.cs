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

    [JsonPropertyName("air_quality")]
    public AirQualityCurrentData? AirQuality { get; set; }
}

public class CurrentData
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }
    
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public double ApparentTemperature { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("wind_direction_10m")]
    public int WindDirection { get; set; }

    [JsonPropertyName("wind_gusts_10m")]
    public double WindGusts { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public int Humidity { get; set; }

    [JsonPropertyName("surface_pressure")]
    public double SurfacePressure { get; set; }

    [JsonPropertyName("is_day")]
    public int IsDay { get; set; }

    [JsonPropertyName("dew_point_2m")]
    public double DewPoint { get; set; }

    [JsonPropertyName("visibility")]
    public double Visibility { get; set; }

    [JsonPropertyName("cloud_cover")]
    public int CloudCover { get; set; }
}

public class HourlyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature { get; set; } = new();

    [JsonPropertyName("apparent_temperature")]
    public List<double> ApparentTemperature { get; set; } = new();

    [JsonPropertyName("precipitation_probability")]
    public List<int> PrecipitationProbability { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = new();

    [JsonPropertyName("relative_humidity_2m")]
    public List<int> Humidity { get; set; } = new();

    [JsonPropertyName("uv_index")]
    public List<double> UvIndex { get; set; } = new();

    [JsonPropertyName("surface_pressure")]
    public List<double>? SurfacePressure { get; set; }

    [JsonPropertyName("is_day")]
    public List<int>? IsDay { get; set; }
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

    [JsonPropertyName("sunrise")]
    public List<string> Sunrise { get; set; } = new();

    [JsonPropertyName("sunset")]
    public List<string> Sunset { get; set; } = new();

    [JsonPropertyName("uv_index_max")]
    public List<double> UvIndexMax { get; set; } = new();

    [JsonPropertyName("precipitation_probability_max")]
    public List<int> PrecipitationProbabilityMax { get; set; } = new();

    [JsonPropertyName("precipitation_sum")]
    public List<double>? PrecipitationSum { get; set; }

    [JsonPropertyName("daylight_duration")]
    public List<double>? DaylightDuration { get; set; }

    [JsonPropertyName("sunshine_duration")]
    public List<double>? SunshineDuration { get; set; }

    [JsonPropertyName("moonrise")]
    public List<string>? Moonrise { get; set; }

    [JsonPropertyName("moonset")]
    public List<string>? Moonset { get; set; }

    [JsonPropertyName("moon_phase")]
    public List<double>? MoonPhase { get; set; }
}

public class AirQualityResponse
{
    [JsonPropertyName("current")]
    public AirQualityCurrentData? Current { get; set; }
}

public class AirQualityCurrentData
{
    [JsonPropertyName("us_aqi")]
    public int UsAqi { get; set; }

    [JsonPropertyName("pm2_5")]
    public double Pm25 { get; set; }

    [JsonPropertyName("pm10")]
    public double Pm10 { get; set; }
}
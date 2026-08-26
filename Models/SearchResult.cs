using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ArmprodWeather.Models;

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<LocationItem>? Results { get; set; }
}

public class LocationItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("admin1")]
    public string Admin1 { get; set; } = string.Empty;

    // Clear overview in UI: "Brno (South Moravian Region), Czechia"
    public string DisplayName => string.IsNullOrEmpty(Admin1)
        ? $"{Name}, {Country}"
        : $"{Name} ({Admin1}), {Country}";
}
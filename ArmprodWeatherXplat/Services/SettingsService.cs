using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ArmprodWeatherXplat.Services;

public record FavoriteLocation(string Name, double Latitude, double Longitude, string? Country = null);

public enum TimeFormatSetting
{
    System,
    TwentyFourHour,
    TwelveHour
}

public class UserSettings
{
    public string CityName { get; set; } = "Brno";
    public double Latitude { get; set; } = 49.1951;
    public double Longitude { get; set; } = 16.6077;
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "System";
    public string TemperatureUnit { get; set; } = "System";
    public string WindSpeedUnit { get; set; } = "System";

    public List<FavoriteLocation> Favorites { get; set; } = new();

    public DateTime LastUpdated { get; set; } = DateTime.MinValue;
    public string? RawWeatherJson { get; set; }

    public TimeFormatSetting TimeFormat { get; set; } = TimeFormatSetting.System;
}

public class SettingsService
{
    private readonly string _filePath;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "ArmprodWeather"
        );
        
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.json");
    }

    public UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // In error statement returns default settings
        }

        return new UserSettings();
    }

    public void SaveSettings(UserSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignore data entry errors
        }
    }
}
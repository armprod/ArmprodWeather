using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeather.Services;
using ArmprodWeather.Models;

namespace ArmprodWeather.ViewModels;

public record HourlyItem(string Time, string Icon, string Temp);
public record DailyItem(string Day, string Icon, string TempRange);

public partial class MainViewModel : ViewModelBase
{
    private readonly WeatherService _weatherService = new();
    private readonly LocationService _locationService = new();
    private readonly SettingsService _settingsService = new();
    
    private CancellationTokenSource? _searchCts;
    private double _currentLat = 49.1951;
    private double _currentLon = 16.6077;

    [ObservableProperty] private string _cityName = "Loading...";
    [ObservableProperty] private string _currentTemperature = "--°";
    [ObservableProperty] private string _weatherCondition = "Loading...";
    [ObservableProperty] private string _tempRange = "H: --°  |  L: --°";
    [ObservableProperty] private string _windSpeed = "-- km/h";
    [ObservableProperty] private string _humidity = "-- %";
    [ObservableProperty] private string _weatherIcon = "❓";

    // Searching
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearchOpen = false;
    [ObservableProperty] private LocationItem? _selectedSearchResult;

    public ObservableCollection<LocationItem> SearchResults { get; } = new();
    public ObservableCollection<HourlyItem> HourlyForecast { get; } = new();
    public ObservableCollection<DailyItem> DailyForecast { get; } = new();

    public MainViewModel()
    {
        _ = InitializeLocationAsync();
    }

    private async Task InitializeLocationAsync()
    {
        var settings = _settingsService.LoadSettings();
        _currentLat = settings.Latitude;
        _currentLon = settings.Longitude;

        // Check if cached data is less than 15 minutes old
        bool isCacheValid = (DateTime.Now - settings.LastUpdated).TotalMinutes < 15;

        await LoadWeatherForLocationAsync(settings.Latitude, settings.Longitude, settings.CityName, forceRefresh: !isCacheValid);
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        SearchResults.Clear();
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Forced refresh bypasses the 15-minute cache check
        await LoadWeatherForLocationAsync(_currentLat, _currentLon, CityName, forceRefresh: true);
    }

    // Reaction to changes in search bar
    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);

                var results = await _locationService.SearchCityAsync(value);

                if (token.IsCancellationRequested) return;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SearchResults.Clear();
                    if (results != null)
                    {
                        foreach (var item in results)
                        {
                            SearchResults.Add(item);
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Ignore if user typed another character
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        }, token);
    }

    // Reaction to click on the item in the list
    partial void OnSelectedSearchResultChanged(LocationItem? value)
    {
        if (value != null)
        {
            _ = SelectLocationAsync(value);
        }
    }

    private async Task SelectLocationAsync(LocationItem item)
    {
        IsSearchOpen = false;
        SearchResults.Clear();
        SearchQuery = string.Empty;
        SelectedSearchResult = null;

        await LoadWeatherForLocationAsync(item.Latitude, item.Longitude, item.Name, forceRefresh: true);
    }

    private async Task LoadWeatherForLocationAsync(double lat, double lon, string name, bool forceRefresh = false)
    {
        try
        {
            _currentLat = lat;
            _currentLon = lon;
            CityName = name;

            var settings = _settingsService.LoadSettings();

            // Cache check: return stored data if within 15 minutes and same city
            if (!forceRefresh && settings.CityName == name && (DateTime.Now - settings.LastUpdated).TotalMinutes < 15)
            {
                if (!string.IsNullOrEmpty(settings.RawWeatherJson))
                {
                    var cachedWeather = JsonSerializer.Deserialize<WeatherResponse>(settings.RawWeatherJson);
                    if (cachedWeather != null)
                    {
                        UpdateUI(cachedWeather);
                        return;
                    }
                }
            }

            WeatherCondition = "Loading...";

            var weather = await _weatherService.GetWeatherAsync(lat, lon);
            if (weather?.Current == null) return;

            // Save updated location & JSON cache to disk
            settings.CityName = name;
            settings.Latitude = lat;
            settings.Longitude = lon;
            settings.LastUpdated = DateTime.Now;
            settings.RawWeatherJson = JsonSerializer.Serialize(weather);
            _settingsService.SaveSettings(settings);

            UpdateUI(weather);
        }
        catch (Exception ex)
        {
            WeatherCondition = $"Error: {ex.Message}";
        }
    }

    private void UpdateUI(WeatherResponse weather)
    {
        CurrentTemperature = $"{Math.Round(weather.Current.Temperature)}°";
        WeatherCondition = MapCodeToCondition(weather.Current.WeatherCode);
        WeatherIcon = MapCodeToIcon(weather.Current.WeatherCode);
        WindSpeed = $"{Math.Round(weather.Current.WindSpeed)} km/h";
        Humidity = $"{weather.Current.Humidity} %";

        if (weather.Daily != null && weather.Daily.TempMax.Count > 0)
        {
            TempRange = $"H: {Math.Round(weather.Daily.TempMax[0])}°  |  L: {Math.Round(weather.Daily.TempMin[0])}°";
        }

        HourlyForecast.Clear();
        if (weather.Hourly != null)
        {
            int currentHour = DateTime.Now.Hour;
            for (int i = currentHour; i < currentHour + 24 && i < weather.Hourly.Time.Count; i++)
            {
                var dt = DateTime.Parse(weather.Hourly.Time[i]);
                string timeLabel = (i == currentHour) ? "Now" : dt.ToString("HH:mm");
                HourlyForecast.Add(new HourlyItem(timeLabel, MapCodeToIcon(weather.Hourly.WeatherCode[i]), $"{Math.Round(weather.Hourly.Temperature[i])}°"));
            }
        }

        DailyForecast.Clear();
        if (weather.Daily != null)
        {
            for (int i = 0; i < weather.Daily.Time.Count; i++)
            {
                var dt = DateTime.Parse(weather.Daily.Time[i]);
                string dayName = (i == 0) ? "Today" : dt.ToString("ddd", new CultureInfo("en-US"));
                DailyForecast.Add(new DailyItem(dayName, MapCodeToIcon(weather.Daily.WeatherCode[i]), $"{Math.Round(weather.Daily.TempMin[i])}° / {Math.Round(weather.Daily.TempMax[i])}°"));
            }
        }
    }

    private static string MapCodeToIcon(int code) => code switch
    {
        0 => "☀️", 1 or 2 => "🌤️", 3 => "☁️", 45 or 48 => "🌫️",
        51 or 53 or 55 or 61 or 63 or 65 => "🌧️", 71 or 73 or 75 => "❄️",
        95 or 96 or 99 => "🌩️", _ => "🌡️"
    };

    private static string MapCodeToCondition(int code) => code switch
    {
        0 => "Clear", 1 or 2 => "Almost clear", 3 => "Cloudy", 45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle", 61 or 63 or 65 => "Rain", 71 or 73 or 75 => "Snowfall",
        95 or 96 or 99 => "Thunderstorm", _ => "Unpredictable weather"
    };
}
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
using Avalonia;
using Avalonia.Styling;

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
    private WeatherResponse? _lastWeather;
    private static readonly string SystemLanguage = 
    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("cs", StringComparison.OrdinalIgnoreCase) ? "Czech" : "English";

    [ObservableProperty] private string _cityName = "Loading...";
    [ObservableProperty] private string _currentTemperature = "--°";
    [ObservableProperty] private string _weatherCondition = "Loading...";
    [ObservableProperty] private string _tempRange = "H: --°  |  L: --°";
    [ObservableProperty] private string _windSpeed = "-- km/h";
    [ObservableProperty] private string _humidity = "-- %";
    [ObservableProperty] private string _weatherIcon = "❓";

    // Dynamic localization properties
    [ObservableProperty] private string _settingsTitle = "Settings";
    [ObservableProperty] private string _themeLabel = "Theme";
    [ObservableProperty] private string _languageLabel = "Language";
    [ObservableProperty] private string _searchPlaceholder = "Enter city name...";
    [ObservableProperty] private string _hourlyForecastHeader = "Hourly forecast";
    [ObservableProperty] private string _dailyForecastHeader = "7-day forecast";
    [ObservableProperty] private string _windHeader = "💨 Wind";
    [ObservableProperty] private string _humidityHeader = "💧 Humidity";

    // Searching
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearchOpen = false;
    [ObservableProperty] private LocationItem? _selectedSearchResult;

    public ObservableCollection<LocationItem> SearchResults { get; } = new();
    public ObservableCollection<HourlyItem> HourlyForecast { get; } = new();
    public ObservableCollection<DailyItem> DailyForecast { get; } = new();

    // Settings
    [ObservableProperty] private bool _isSettingsOpen = false;
    [ObservableProperty] private string _selectedTheme = "Dark";
    [ObservableProperty] private string _selectedLanguage = "English";

    public ObservableCollection<string> AvailableThemes { get; } = new() { "System", "Dark", "Light" };
    public ObservableCollection<string> AvailableLanguages { get; } = new() { "System", "English", "Czech" };

    public MainViewModel()
    {
        _ = InitializeLocationAsync();
    }

    private async Task InitializeLocationAsync()
    {
        var settings = _settingsService.LoadSettings();
        _currentLat = settings.Latitude;
        _currentLon = settings.Longitude;

        SelectedTheme = settings.Theme;
        SelectedLanguage = settings.Language;
        ApplyTheme(settings.Theme);
        ApplyLanguageCulture(settings.Language);
        UpdateLocalizedTexts();

        bool isCacheValid = (DateTime.Now - settings.LastUpdated).TotalMinutes < 15;
        await LoadWeatherForLocationAsync(settings.Latitude, settings.Longitude, settings.CityName, forceRefresh: !isCacheValid);
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        ApplyTheme(value);

        var settings = _settingsService.LoadSettings();
        settings.Theme = value;
        _settingsService.SaveSettings(settings);
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        var settings = _settingsService.LoadSettings();
        settings.Language = value;
        _settingsService.SaveSettings(settings);

        ApplyLanguageCulture(value);
        UpdateLocalizedTexts();

        if (_lastWeather != null)
        {
            UpdateUI(_lastWeather);
        }
    }

    private static string GetEffectiveLanguage(string selectedLanguage)
    {
        return selectedLanguage == "System" ? SystemLanguage : selectedLanguage;
    }

    private void UpdateLocalizedTexts()
    {
        bool isCzech = GetEffectiveLanguage(SelectedLanguage) == "Czech";

        SettingsTitle = isCzech ? "Nastavení" : "Settings";
        ThemeLabel = isCzech ? "Motiv aplikace" : "App Theme";
        LanguageLabel = isCzech ? "Jazyk" : "Language";
        SearchPlaceholder = isCzech ? "Zadejte název města..." : "Enter city name...";
        HourlyForecastHeader = isCzech ? "Hodinová předpověď" : "Hourly forecast";
        DailyForecastHeader = isCzech ? "7denní předpověď" : "7-day forecast";
        WindHeader = isCzech ? "💨 Vítr" : "💨 Wind";
        HumidityHeader = isCzech ? "💧 Vlhkost" : "💧 Humidity";
    }

    private static void ApplyLanguageCulture(string language)
    {
        string effective = GetEffectiveLanguage(language);
        var cultureCode = effective == "Czech" ? "cs-CZ" : "en-US";
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
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
        await LoadWeatherForLocationAsync(_currentLat, _currentLon, CityName, forceRefresh: true);
    }

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        }, token);
    }

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

            WeatherCondition = SelectedLanguage == "Czech" ? "Načítání..." : "Loading...";

            var weather = await _weatherService.GetWeatherAsync(lat, lon);
            if (weather?.Current == null) return;

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

    private void UpdateUI(WeatherResponse? weather)
    {
        if (weather?.Current == null) return;

        _lastWeather = weather;
        bool isCzech = GetEffectiveLanguage(SelectedLanguage) == "Czech";
        var culture = isCzech ? new CultureInfo("cs-CZ") : new CultureInfo("en-US");

        CurrentTemperature = $"{Math.Round(weather.Current.Temperature)}°";
        WeatherCondition = MapCodeToCondition(weather.Current.WeatherCode);
        WeatherIcon = MapCodeToIcon(weather.Current.WeatherCode);
        WindSpeed = $"{Math.Round(weather.Current.WindSpeed)} km/h";
        Humidity = $"{weather.Current.Humidity} %";

        if (weather.Daily?.TempMax is { Count: > 0 } && weather.Daily?.TempMin is { Count: > 0 })
        {
            string highLabel = isCzech ? "V" : "H";
            string lowLabel = isCzech ? "N" : "L";

            TempRange = $"{highLabel}: {Math.Round(weather.Daily.TempMax[0])}°  |  {lowLabel}: {Math.Round(weather.Daily.TempMin[0])}°";
        }

        HourlyForecast.Clear();
        if (weather.Hourly?.Time != null && weather.Hourly?.WeatherCode != null && weather.Hourly?.Temperature != null)
        {
            int currentHour = DateTime.Now.Hour;
            int maxItems = Math.Min(currentHour + 24, weather.Hourly.Time.Count);

            for (int i = currentHour; i < maxItems; i++)
            {
                var dt = DateTime.Parse(weather.Hourly.Time[i]);
                string timeLabel = (i == currentHour) ? (isCzech ? "Teď" : "Now") : dt.ToString("HH:mm");
                HourlyForecast.Add(new HourlyItem(
                    timeLabel, 
                    MapCodeToIcon(weather.Hourly.WeatherCode[i]), 
                    $"{Math.Round(weather.Hourly.Temperature[i])}°"));
            }
        }

        DailyForecast.Clear();
        if (weather.Daily?.Time != null && weather.Daily?.WeatherCode != null && weather.Daily?.TempMin != null && weather.Daily?.TempMax != null)
        {
            for (int i = 0; i < weather.Daily.Time.Count; i++)
            {
                var dt = DateTime.Parse(weather.Daily.Time[i]);
                string dayName = (i == 0) ? (isCzech ? "Dnes" : "Today") : dt.ToString("ddd", culture);

                if (isCzech && dayName is { Length: > 0 })
                {
                    dayName = char.ToUpper(dayName[0]) + dayName[1..];
                }

                DailyForecast.Add(new DailyItem(
                    dayName, 
                    MapCodeToIcon(weather.Daily.WeatherCode[i]), 
                    $"{Math.Round(weather.Daily.TempMin[i])}° / {Math.Round(weather.Daily.TempMax[i])}°"));
            }
        }
    }

    private static string MapCodeToIcon(int code) => code switch
    {
        0 => "☀️", 1 or 2 => "🌤️", 3 => "☁️", 45 or 48 => "🌫️",
        51 or 53 or 55 or 61 or 63 or 65 => "🌧️", 71 or 73 or 75 => "❄️",
        95 or 96 or 99 => "🌩️", _ => "🌡️"
    };

    private string MapCodeToCondition(int code)
    {
        bool isCzech = GetEffectiveLanguage(SelectedLanguage) == "Czech";

        return code switch
        {
            0 => isCzech ? "Jasno" : "Clear",
            1 or 2 => isCzech ? "Skoro jasno" : "Almost clear",
            3 => isCzech ? "Zataženo" : "Cloudy",
            45 or 48 => isCzech ? "Mlha" : "Fog",
            51 or 53 or 55 => isCzech ? "Mrholení" : "Drizzle",
            61 or 63 or 65 => isCzech ? "Déšť" : "Rain",
            71 or 73 or 75 => isCzech ? "Sněžení" : "Snowfall",
            95 or 96 or 99 => isCzech ? "Bouřky" : "Thunderstorm",
            _ => isCzech ? "Proměnlivo" : "Unpredictable weather"
        };
    }
}
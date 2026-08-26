using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeather.Services;
using ArmprodWeather.Models;
using ArmprodWeather.Helpers;

namespace ArmprodWeather.ViewModels;

public record HourlyItem(string Time, string Icon, string Temp);
public record DailyItem(string Day, string Icon, string TempRange);

public partial class MainViewModel : ViewModelBase
{
    private readonly WeatherService _weatherService = new();
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();
    
    private double _currentLat = 49.1951;
    private double _currentLon = 16.6077;
    private WeatherResponse? _lastWeather;

    // Sub-ViewModels
    public SearchViewModel Search { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public FavoritesViewModel Favorites { get; } = new();

    [ObservableProperty] private string _cityName = "Loading...";
    [ObservableProperty] private string _currentTemperature = "--°";
    [ObservableProperty] private string _weatherCondition = "Loading...";
    [ObservableProperty] private string _tempRange = "H: --°  |  L: --°";
    [ObservableProperty] private string _windSpeed = "-- km/h";
    [ObservableProperty] private string _humidity = "-- %";
    [ObservableProperty] private string _weatherIcon = "❓";

    // Dynamic localization properties
    [ObservableProperty] private string _hourlyForecastHeader = "Hourly forecast";
    [ObservableProperty] private string _dailyForecastHeader = "7-day forecast";
    [ObservableProperty] private string _windHeader = "💨 Wind";
    [ObservableProperty] private string _humidityHeader = "💧 Humidity";

    public ObservableCollection<HourlyItem> HourlyForecast { get; } = new();
    public ObservableCollection<DailyItem> DailyForecast { get; } = new();

    public MainViewModel()
    {
        Search.LocationSelected += async (location) =>
        {
            await LoadWeatherForLocationAsync(location.Latitude, location.Longitude, location.Name, forceRefresh: true);
        };

        Settings.LanguageChanged += (language) =>
        {
            UpdateLocalizedTexts();
            if (_lastWeather != null) UpdateUI(_lastWeather);
        };

        Settings.UnitsChanged += () =>
        {
            if (_lastWeather != null) UpdateUI(_lastWeather);
        };

        Favorites.FavoriteSelected += async (location) =>
        {
            await LoadWeatherForLocationAsync(location.Latitude, location.Longitude, location.Name, forceRefresh: false);
        };

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var settings = _settingsService.LoadSettings();
        _currentLat = settings.Latitude;
        _currentLon = settings.Longitude;

        Favorites.Initialize(settings.Favorites);
        Settings.Initialize(settings.Theme, settings.Language, settings.TemperatureUnit, settings.WindSpeedUnit);
        UpdateLocalizedTexts();

        bool isCacheValid = (DateTime.Now - settings.LastUpdated).TotalMinutes < 15;
        await LoadWeatherForLocationAsync(settings.Latitude, settings.Longitude, settings.CityName, forceRefresh: !isCacheValid);
    }

    private void UpdateLocalizedTexts()
    {
        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";

        HourlyForecastHeader = isCzech ? "Hodinová předpověď" : "Hourly forecast";
        DailyForecastHeader = isCzech ? "7denní předpověď" : "7-day forecast";
        WindHeader = isCzech ? "💨 Vítr" : "💨 Wind";
        HumidityHeader = isCzech ? "💧 Vlhkost" : "💧 Humidity";

        Search.SearchPlaceholder = isCzech ? "Zadejte název města..." : "Enter city name...";
        Settings.UpdateLocalizedTexts();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadWeatherForLocationAsync(_currentLat, _currentLon, CityName, forceRefresh: true);
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

            WeatherCondition = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech" ? "Načítání..." : "Loading...";

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

        Favorites.UpdateCurrentLocation(lat, lon, name);
    }

    private double FormatTemp(double celsius)
    {
        return Settings.SelectedTemperatureUnit == "°F" ? (celsius * 1.8 + 32) : celsius;
    }

    private double FormatWind(double kmh)
    {
        return Settings.SelectedWindSpeedUnit == "mph" ? (kmh * 0.621371) : kmh;
    }

    private void UpdateUI(WeatherResponse? weather)
    {
        if (weather?.Current == null) return;

        _lastWeather = weather;
        string effectiveLanguage = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage);
        bool isCzech = effectiveLanguage == "Czech";
        var culture = isCzech ? new CultureInfo("cs-CZ") : new CultureInfo("en-US");

        string tempUnit = Settings.GetEffectiveTemperatureUnit();
        string windUnit = Settings.GetEffectiveWindSpeedUnit();

        CurrentTemperature = $"{Math.Round(FormatTemp(weather.Current.Temperature))}{tempUnit}";
        WindSpeed = $"{Math.Round(FormatWind(weather.Current.WindSpeed))} {windUnit}";
        Humidity = $"{weather.Current.Humidity} %";

        WeatherCondition = WeatherMapper.MapCodeToCondition(weather.Current.WeatherCode, effectiveLanguage);
        WeatherIcon = WeatherMapper.MapCodeToIcon(weather.Current.WeatherCode);

        if (weather.Daily?.TempMax is { Count: > 0 } && weather.Daily?.TempMin is { Count: > 0 })
        {
            string highLabel = isCzech ? "Max" : "H";
            string lowLabel = isCzech ? "Min" : "L";
            double maxTemp = FormatTemp(weather.Daily.TempMax[0]);
            double minTemp = FormatTemp(weather.Daily.TempMin[0]);
            TempRange = $"{highLabel}: {Math.Round(maxTemp)}{tempUnit}  |  {lowLabel}: {Math.Round(minTemp)}{tempUnit}";
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
                double hourlyTemp = FormatTemp(weather.Hourly.Temperature[i]);

                HourlyForecast.Add(new HourlyItem(
                    timeLabel, 
                    WeatherMapper.MapCodeToIcon(weather.Hourly.WeatherCode[i]),
                    $"{Math.Round(hourlyTemp)}{tempUnit}"));
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

                double minDaily = FormatTemp(weather.Daily.TempMin[i]);
                double maxDaily = FormatTemp(weather.Daily.TempMax[i]);

                DailyForecast.Add(new DailyItem(
                    dayName, 
                    WeatherMapper.MapCodeToIcon(weather.Daily.WeatherCode[i]),
                    $"{Math.Round(minDaily)}{tempUnit} / {Math.Round(maxDaily)}{tempUnit}"));
            }
        }
    }
}
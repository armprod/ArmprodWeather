using System;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeather.Services;
using ArmprodWeather.Models;

namespace ArmprodWeather.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly WeatherService _weatherService = new();
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();
    
    private double _currentLat = 49.1951;
    private double _currentLon = 16.6077;
    private WeatherResponse? _lastWeather;
    private DateTime? _lastFailedRefreshAttempt;

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

        string initialCity = string.IsNullOrWhiteSpace(settings.CityName) ? "Praha" : settings.CityName;
        double initialLat = settings.Latitude == 0 ? 50.0755 : settings.Latitude;
        double initialLon = settings.Longitude == 0 ? 14.4378 : settings.Longitude;

        Favorites.Initialize(settings.Favorites, initialCity, initialLat, initialLon);
        Settings.Initialize(settings.Theme, settings.Language, settings.TemperatureUnit, settings.WindSpeedUnit);
        UpdateLocalizedTexts();

        bool isCacheValid = (DateTime.Now - settings.LastUpdated).TotalMinutes < 15;
        await LoadWeatherForLocationAsync(initialLat, initialLon, initialCity, forceRefresh: !isCacheValid);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadWeatherForLocationAsync(_currentLat, _currentLon, CityName, forceRefresh: true);
    }

    private async Task LoadWeatherForLocationAsync(double lat, double lon, string name, bool forceRefresh = false)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        var settings = _settingsService.LoadSettings();

        try
        {
            _currentLat = lat;
            _currentLon = lon;
            CityName = name;
            Favorites.UpdateCurrentLocation(lat, lon, name);

            // Kešování
            if (!forceRefresh && settings.CityName == name && (DateTime.Now - settings.LastUpdated).TotalMinutes < 15)
            {
                if (!string.IsNullOrEmpty(settings.RawWeatherJson))
                {
                    var cachedWeather = JsonSerializer.Deserialize<WeatherResponse>(settings.RawWeatherJson);
                    if (cachedWeather != null)
                    {
                        IsOffline = false;
                        UpdateLastUpdatedText(settings.LastUpdated);
                        UpdateUI(cachedWeather);
                        return;
                    }
                }
            }

            // Volání API
            var weather = await _weatherService.GetWeatherAsync(lat, lon);
            if (weather?.Current == null)
            {
                HandleLoadError("Nepodařilo se získat data z meteo služby.", "Failed to retrieve data from weather service.", settings);
                return;
            }

            settings.CityName = name;
            settings.Latitude = lat;
            settings.Longitude = lon;
            settings.LastUpdated = DateTime.Now;
            settings.RawWeatherJson = JsonSerializer.Serialize(weather);
            _settingsService.SaveSettings(settings);

            _lastFailedRefreshAttempt = null;
            IsOffline = false;
            UpdateLastUpdatedText(settings.LastUpdated);
            UpdateUI(weather);
        }
        catch (System.Net.Http.HttpRequestException)
        {
            HandleLoadError("Chybí připojení k internetu. Zkontrolujte síť a zkuste to znovu.", "No internet connection. Please check your network and try again.", settings);
        }
        catch (Exception ex)
        {
            HandleLoadError($"Došlo k chybě: {ex.Message}", $"An error occurred: {ex.Message}", settings);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void HandleLoadError(string czMessage, string enMessage, UserSettings settings)
    {
        _lastFailedRefreshAttempt = DateTime.Now;

        if (_lastWeather == null && !string.IsNullOrEmpty(settings.RawWeatherJson))
        {
            try
            {
                var cachedWeather = JsonSerializer.Deserialize<WeatherResponse>(settings.RawWeatherJson);
                if (cachedWeather != null) UpdateUI(cachedWeather);
            }
            catch { }
        }

        if (_lastWeather != null)
        {
            IsOffline = true;
            UpdateLastUpdatedText(settings.LastUpdated);
        }
        else
        {
            IsOffline = false;
            SetErrorState(czMessage, enMessage);
        }
    }

    private void UpdateLastUpdatedText(DateTime lastUpdated)
    {
        if (lastUpdated == default)
        {
            LastUpdatedText = string.Empty;
            OfflineMessage = string.Empty;
            return;
        }

        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";
        string timeStr = lastUpdated.ToString("HH:mm");
        LastUpdatedText = timeStr;

        if (_lastFailedRefreshAttempt.HasValue && IsOffline)
        {
            string failTime = _lastFailedRefreshAttempt.Value.ToString("HH:mm");
            OfflineMessage = isCzech ? $"Obnovení v {failTime} selhalo • Data z {timeStr}" : $"Refresh at {failTime} failed • Cached {timeStr}";
        }
        else
        {
            OfflineMessage = isCzech ? $"Zobrazena neaktuální data ({timeStr})" : $"Showing cached data ({timeStr})";
        }
    }

    private void SetErrorState(string czMessage, string enMessage)
    {
        HasError = true;
        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";
        ErrorMessage = isCzech ? czMessage : enMessage;
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeatherXplat.Helpers;
using ArmprodWeatherXplat.Models;
using ArmprodWeatherXplat.Services;
using Avalonia.Threading;

namespace ArmprodWeatherXplat.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly WeatherService _weatherService = new();
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();
    
    private double _currentLat = 49.1951;
    private double _currentLon = 16.6077;
    private WeatherResponse? _lastWeather;
    private DateTime? _lastFailedRefreshAttempt;

    private CancellationTokenSource? _weatherLoadCts;

    public MainViewModel()
    {
        Search.Initialize(this);
        Settings.Initialize(this);
    
        Search.LocationSelected += async (location) =>
        {
            await LoadWeatherForLocationAsync(location.Latitude, location.Longitude, location.Name, forceRefresh: true);
        };

        Settings.LanguageChanged += (language) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateLocalizedTexts();
                if (_lastWeather != null) UpdateUI(_lastWeather);
            }, DispatcherPriority.Background);
        };

        Settings.UnitsChanged += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_lastWeather != null) UpdateUI(_lastWeather);
            }, DispatcherPriority.Background);
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

        string initialCity = string.IsNullOrWhiteSpace(settings.CityName) ? "Brno" : settings.CityName;
        double initialLat = settings.Latitude == 0 ? 50.0755 : settings.Latitude;
        double initialLon = settings.Longitude == 0 ? 14.4378 : settings.Longitude;

        Favorites.Initialize(settings.Favorites, initialCity, initialLat, initialLon);
        
        Settings.Initialize(settings.Theme, settings.Language, settings.TemperatureUnit, settings.WindSpeedUnit, settings.TimeFormat);
        
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
        _weatherLoadCts?.Cancel();
        _weatherLoadCts = new CancellationTokenSource();
        var token = _weatherLoadCts.Token;

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

            // Caching
            if (!forceRefresh && settings.CityName == name && (DateTime.Now - settings.LastUpdated).TotalMinutes < 15)
            {
                if (!string.IsNullOrEmpty(settings.RawWeatherJson))
                {
                    var cachedWeather = System.Text.Json.JsonSerializer.Deserialize<WeatherResponse>(settings.RawWeatherJson);
                    if (cachedWeather != null && !token.IsCancellationRequested)
                    {
                        IsOffline = false;
                        UpdateLastUpdatedText(settings.LastUpdated);
                        UpdateUI(cachedWeather);
                        return;
                    }
                }
            }

            // API CALL
            var weather = await _weatherService.GetWeatherAsync(lat, lon);

            if (token.IsCancellationRequested) return;

            if (weather?.Current == null)
            {
                HandleLoadError("Nepodařilo se získat data z meteo služby.", "Failed to retrieve data from weather service.", settings);
                return;
            }

            settings.CityName = name;
            settings.Latitude = lat;
            settings.Longitude = lon;
            settings.LastUpdated = DateTime.Now;
            settings.RawWeatherJson = System.Text.Json.JsonSerializer.Serialize(weather);
            _settingsService.SaveSettings(settings);

            _lastFailedRefreshAttempt = null;
            IsOffline = false;
            UpdateLastUpdatedText(settings.LastUpdated);
            
            _lastWeather = weather;
            UpdateUI(weather);
        }
        catch (OperationCanceledException)
        {
            // The request was cancelled by a more recent click – we’ll ignore it
        }
        catch (System.Net.Http.HttpRequestException)
        {
            if (!token.IsCancellationRequested)
            {
                HandleLoadError("Chybí připojení k internetu. Zkontrolujte síť a zkuste to znovu.", "No internet connection. Please check your network and try again.", settings);
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                HandleLoadError($"Došlo k chybě: {ex.Message}", $"An error occurred: {ex.Message}", settings);
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsLoading = false;
            }
        }
    }

    private void HandleLoadError(string czMessage, string enMessage, UserSettings settings)
    {
        _lastFailedRefreshAttempt = DateTime.Now;

        if (_lastWeather == null && !string.IsNullOrEmpty(settings.RawWeatherJson))
        {
            try
            {
                var cachedWeather = System.Text.Json.JsonSerializer.Deserialize<WeatherResponse>(settings.RawWeatherJson);
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

    private void SetErrorState(string czMessage, string enMessage)
    {
        HasError = true;
        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";
        ErrorMessage = isCzech ? czMessage : enMessage;
    }
}
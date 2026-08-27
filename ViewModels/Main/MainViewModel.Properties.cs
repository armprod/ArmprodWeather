using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ArmprodWeather.Models;

namespace ArmprodWeather.ViewModels;

public record HourlyItem(
    string Time,
    string Icon,
    string Temp,
    string ApparentTemp,
    string PrecipProb,
    bool HasRain
);

public record DailyItem(string Day, string Icon, string TempRange);

public partial class MainViewModel
{
    // Sub-ViewModels
    public SearchViewModel Search { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public FavoritesViewModel Favorites { get; } = new();

    // Dynamické hodnoty počasí
    [ObservableProperty] private string _cityName = "Loading...";
    [ObservableProperty] private string _currentTemperature = "--°";
    [ObservableProperty] private string _weatherCondition = "Loading...";
    [ObservableProperty] private string _tempRange = "H: --°  |  L: --°";
    [ObservableProperty] private string _windSpeed = "-- km/h";
    [ObservableProperty] private string _windGustsText = string.Empty;
    [ObservableProperty] private string _windDirectionText = string.Empty;
    [ObservableProperty] private string _humidity = "-- %";
    [ObservableProperty] private string _weatherIcon = "❓";
    [ObservableProperty] private string _localTimeText = "--:--";
    [ObservableProperty] private string _apparentTempText = "--";
    [ObservableProperty] private string _pressureText = "--";
    [ObservableProperty] private string _sunriseText = "--:--";
    [ObservableProperty] private string _sunsetText = "--:--";
    [ObservableProperty] private string _uvIndexText = "--";
    [ObservableProperty] private string _precipProbText = "--";
    [ObservableProperty] private string _dewPointText = string.Empty;
    [ObservableProperty] private string _cloudCoverText = string.Empty;
    [ObservableProperty] private string _visibilityText = string.Empty;
    [ObservableProperty] private string _uvRiskText = string.Empty;
    [ObservableProperty] private string _precipAmountText = string.Empty;
    [ObservableProperty] private string _daylightDurationText = string.Empty;
    [ObservableProperty] private string _sunshineText = string.Empty;
    [ObservableProperty] private string _tempDeltaText = string.Empty;
    [ObservableProperty] private string _hourlyPeakPrecipText = string.Empty;
    [ObservableProperty] private string _peakUvTimeText = string.Empty;

    // Nadpisy
    [ObservableProperty] private string _localTimeHeader = "Local time";
    [ObservableProperty] private string _windHeader = "💨 Wind";
    [ObservableProperty] private string _humidityHeader = "💧 Humidity";
    [ObservableProperty] private string _apparentTempHeader = "🌡️ Feels like";
    [ObservableProperty] private string _pressureHeader = "⏲️ Pressure";
    [ObservableProperty] private string _uvIndexHeader = "☀️ UV Index";
    [ObservableProperty] private string _precipProbHeader = "🌧️ Rain chance";
    [ObservableProperty] private string _sunriseHeader = "🌅 Sunrise";
    [ObservableProperty] private string _sunsetHeader = "🌇 Sunset";
    [ObservableProperty] private string _hourlyForecastHeader = "Hourly forecast";
    [ObservableProperty] private string _dailyForecastHeader = "7-day forecast";

    // Stavové proměnné
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _lastUpdatedText = string.Empty;
    [ObservableProperty] private string _offlineHeader = string.Empty;
    [ObservableProperty] private string _offlineMessage = string.Empty;

    // Kolekce
    public ObservableCollection<HourlyItem> HourlyForecast { get; } = new();
    public ObservableCollection<DailyItem> DailyForecast { get; } = new();
}
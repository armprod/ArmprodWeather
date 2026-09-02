using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ArmprodWeatherXplat.Models;

namespace ArmprodWeatherXplat.ViewModels;

public partial class HourlyItem : ObservableObject
{
    [ObservableProperty] private string _time = string.Empty;
    [ObservableProperty] private string _icon = string.Empty;
    [ObservableProperty] private string _temp = string.Empty;
    [ObservableProperty] private string _apparentTemp = string.Empty;
    [ObservableProperty] private string _precipProb = string.Empty;
    [ObservableProperty] private bool _hasRain;
}

public partial class DailyItem : ObservableObject
{
    [ObservableProperty] private string _day = string.Empty;
    [ObservableProperty] private string _icon = string.Empty;
    [ObservableProperty] private string _tempRange = string.Empty;
}

public partial class MainViewModel
{
    // Sub-ViewModels
    public SearchViewModel Search { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public FavoritesViewModel Favorites { get; } = new();

    // Main/Basic Statuses
    [ObservableProperty] private string _cityName = "Loading...";
    [ObservableProperty] private string _currentTemperature = "--";
    [ObservableProperty] private string _weatherCondition = "--";
    [ObservableProperty] private string _weatherIcon = "❓";
    [ObservableProperty] private string _tempRange = "--";
    [ObservableProperty] private string _localTimeText = "--:--";

    // Application Card Values
    [ObservableProperty] private string _windSpeed = "--";
    [ObservableProperty] private string _windGustsText = string.Empty;
    [ObservableProperty] private string _windDirectionText = string.Empty;

    [ObservableProperty] private string _humidity = "-- %";
    [ObservableProperty] private string _dewPointText = string.Empty;

    [ObservableProperty] private string _apparentTempText = "--";
    [ObservableProperty] private string _tempDeltaText = string.Empty;

    [ObservableProperty] private string _pressureText = "--";
    [ObservableProperty] private string _pressureAdviceText = string.Empty;

    [ObservableProperty] private string _uvIndexText = "--";
    [ObservableProperty] private string _uvRiskText = string.Empty;
    [ObservableProperty] private string _peakUvTimeText = string.Empty;

    [ObservableProperty] private string _precipProbText = "-- %";
    [ObservableProperty] private string _precipAmountText = string.Empty;
    [ObservableProperty] private string _hourlyPeakPrecipText = string.Empty;

    [ObservableProperty] private string _visibilityText = "--";
    [ObservableProperty] private string _visibilityAdviceText = string.Empty;

    [ObservableProperty] private string _cloudCoverText = "-- %";
    [ObservableProperty] private string _cloudCoverAdviceText = string.Empty;

    [ObservableProperty] private string _sunriseText = "--:--";
    [ObservableProperty] private string _sunsetText = "--:--";
    [ObservableProperty] private string _daylightDurationText = string.Empty;
    [ObservableProperty] private string _sunshineText = string.Empty;

    [ObservableProperty] private string _aqiText = "--";
    [ObservableProperty] private string _aqiAdviceText = string.Empty;

    [ObservableProperty] private string _moonPhaseText = "--";
    [ObservableProperty] private string _moonRiseSetText = string.Empty;

    // Application Cards Headers
    [ObservableProperty] private string _localTimeHeader = string.Empty;
    [ObservableProperty] private string _windHeader = string.Empty;
    [ObservableProperty] private string _humidityHeader = string.Empty;
    [ObservableProperty] private string _apparentTempHeader = string.Empty;
    [ObservableProperty] private string _pressureHeader = string.Empty;
    [ObservableProperty] private string _uvIndexHeader = string.Empty;
    [ObservableProperty] private string _precipProbHeader = string.Empty;
    [ObservableProperty] private string _visibilityHeader = string.Empty;
    [ObservableProperty] private string _cloudCoverHeader = string.Empty;
    [ObservableProperty] private string _sunriseHeader = string.Empty;
    [ObservableProperty] private string _sunsetHeader = string.Empty;
    [ObservableProperty] private string _aqiHeader = string.Empty;
    [ObservableProperty] private string _moonHeader = string.Empty;
    [ObservableProperty] private string _hourlyForecastHeader = string.Empty;
    [ObservableProperty] private string _dailyForecastHeader = string.Empty;

    // Application Statuses Values
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _lastUpdatedText = string.Empty;
    [ObservableProperty] private string _offlineHeader = string.Empty;
    [ObservableProperty] private string _offlineMessage = string.Empty;

    // Collections
    public ObservableCollection<HourlyItem> HourlyForecast { get; } = new();
    public ObservableCollection<DailyItem> DailyForecast { get; } = new();
}
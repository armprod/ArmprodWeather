using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeatherXplat.Services;
using Avalonia;
using Avalonia.Styling;

namespace ArmprodWeatherXplat.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();

    [ObservableProperty] private bool _isSettingsOpen = false;
    [ObservableProperty] private string _selectedTheme = "System";
    [ObservableProperty] private string _selectedLanguage = "System";

    // Unit properties
    [ObservableProperty] private string _selectedTemperatureUnit = "System";
    [ObservableProperty] private string _selectedWindSpeedUnit = "System";
    [ObservableProperty] private TimeFormatSetting _selectedTimeFormat = TimeFormatSetting.System;

    // Localized labels for Settings panel
    [ObservableProperty] private string _settingsTitle = "Settings";
    [ObservableProperty] private string _themeLabel = "Theme";
    [ObservableProperty] private string _languageLabel = "Language";
    [ObservableProperty] private string _temperatureUnitLabel = "Temperature Units";
    [ObservableProperty] private string _windSpeedUnitLabel = "Wind Speed Units";
    [ObservableProperty] private string _timeFormatLabel = "Time Format";

    public ObservableCollection<string> AvailableThemes { get; } = new() { "System", "Dark", "Light" };
    public ObservableCollection<string> AvailableLanguages { get; } = new() { "System", "English", "Czech" };
    public ObservableCollection<string> AvailableTemperatureUnits { get; } = new() { "System", "°C", "°F" };
    public ObservableCollection<string> AvailableWindSpeedUnits { get; } = new() { "System", "km/h", "mph" };
    public ObservableCollection<TimeFormatSetting> AvailableTimeFormats { get; } = new()
    {
        TimeFormatSetting.System,
        TimeFormatSetting.TwentyFourHour,
        TimeFormatSetting.TwelveHour
    };

    // Event for informate MainViewModel about language change
    public event Action<string>? LanguageChanged;
    public event Action? UnitsChanged;

    public void Initialize(string theme, string language, string tempUnit, string windUnit, TimeFormatSetting timeFormat)
    {
        SelectedTheme = theme;
        SelectedLanguage = language;
        SelectedTemperatureUnit = tempUnit;
        SelectedWindSpeedUnit = windUnit;
        SelectedTimeFormat = timeFormat;

        ApplyTheme(theme);
        _localizationService.ApplyLanguageCulture(language);
        UpdateLocalizedTexts();
    }

    [RelayCommand]
    public void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        ApplyTheme(value);
        SaveCurrentSettings();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        SaveCurrentSettings();
        _localizationService.ApplyLanguageCulture(value);
        UpdateLocalizedTexts();
        LanguageChanged?.Invoke(value);
    }

    partial void OnSelectedTemperatureUnitChanged(string value)
    {
        SaveCurrentSettings();
        UnitsChanged?.Invoke();
    }

    partial void OnSelectedWindSpeedUnitChanged(string value)
    {
        SaveCurrentSettings();
        UnitsChanged?.Invoke();
    }

    partial void OnSelectedTimeFormatChanged(TimeFormatSetting value)
    {
        SaveCurrentSettings();
        UnitsChanged?.Invoke();
    }

    private void SaveCurrentSettings()
    {
        var settings = _settingsService.LoadSettings();
        settings.Theme = SelectedTheme;
        settings.Language = SelectedLanguage;
        settings.TemperatureUnit = SelectedTemperatureUnit;
        settings.WindSpeedUnit = SelectedWindSpeedUnit;
        settings.TimeFormat = SelectedTimeFormat;
        _settingsService.SaveSettings(settings);
    }

    public string GetEffectiveTemperatureUnit()
    {
        if (SelectedTemperatureUnit != "System")
            return SelectedTemperatureUnit;

        return IsSystemMetric() ? "°C" : "°F";
    }

    public string GetEffectiveWindSpeedUnit()
    {
        if (SelectedWindSpeedUnit != "System")
            return SelectedWindSpeedUnit;

        return IsSystemMetric() ? "km/h" : "mph";
    }

    private static bool IsSystemMetric()
    {
        try
        {
            return RegionInfo.CurrentRegion.IsMetric;
        }
        catch
        {
            return true;
        }
    }

    public void UpdateLocalizedTexts()
    {
        bool isCzech = _localizationService.GetEffectiveLanguage(SelectedLanguage) == "Czech";

        SettingsTitle = isCzech ? "Nastavení" : "Settings";
        ThemeLabel = isCzech ? "Motiv aplikace" : "App Theme";
        LanguageLabel = isCzech ? "Jazyk" : "Language";
        TemperatureUnitLabel = isCzech ? "Jednotky teploty" : "Temperature Units";
        WindSpeedUnitLabel = isCzech ? "Jednotky rychlosti větru" : "Wind Speed Units";
        TimeFormatLabel = isCzech ? "Formát času" : "Time Format";
    }

    public static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
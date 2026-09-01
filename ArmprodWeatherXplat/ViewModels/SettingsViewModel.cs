using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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

    private bool _isUpdatingLocalization;

    public SettingsViewModel()
    {
        UpdateLocalizedTexts();
    }

    [ObservableProperty] private bool _isSettingsOpen = false;

    // Interní/Kanovnické hodnoty pro ukládání a logiku
    [ObservableProperty] private string _selectedTheme = "System";
    [ObservableProperty] private string _selectedLanguage = "System";
    [ObservableProperty] private string _selectedTemperatureUnit = "System";
    [ObservableProperty] private string _selectedWindSpeedUnit = "System";
    [ObservableProperty] private TimeFormatSetting _selectedTimeFormat = TimeFormatSetting.System;

    // Zobrazované lokalizované texty pro UI vazby v ListBoxech
    [ObservableProperty] private string _selectedThemeDisplay = "System";
    [ObservableProperty] private string _selectedLanguageDisplay = "System";
    [ObservableProperty] private string _selectedTemperatureUnitDisplay = "System";
    [ObservableProperty] private string _selectedWindSpeedUnitDisplay = "System";
    [ObservableProperty] private string _selectedTimeFormatDisplay = "System";

    // Lokalizované popisky sekcí
    [ObservableProperty] private string _settingsTitle = "Settings";
    [ObservableProperty] private string _themeLabel = "Theme";
    [ObservableProperty] private string _languageLabel = "Language";
    [ObservableProperty] private string _temperatureUnitLabel = "Temperature Units";
    [ObservableProperty] private string _windSpeedUnitLabel = "Wind Speed Units";
    [ObservableProperty] private string _timeFormatLabel = "Time Format";

    // Kolekce pro UI
    public ObservableCollection<string> AvailableThemes { get; } = new();
    public ObservableCollection<string> AvailableLanguages { get; } = new();
    public ObservableCollection<string> AvailableTemperatureUnits { get; } = new();
    public ObservableCollection<string> AvailableWindSpeedUnits { get; } = new();
    public ObservableCollection<string> AvailableTimeFormats { get; } = new();

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

    partial void OnSelectedThemeDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrEmpty(value)) return;

        SelectedTheme = value switch
        {
            "Tmavý" or "Dark" => "Dark",
            "Světlý" or "Light" => "Light",
            _ => "System"
        };

        ApplyTheme(SelectedTheme);
        SaveCurrentSettings();
    }

    partial void OnSelectedLanguageDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrEmpty(value)) return;

        SelectedLanguage = value switch
        {
            "Čeština" or "Czech" => "Czech",
            "Angličtina" or "English" => "English",
            _ => "System"
        };

        SaveCurrentSettings();
        _localizationService.ApplyLanguageCulture(SelectedLanguage);
        UpdateLocalizedTexts();
        LanguageChanged?.Invoke(SelectedLanguage);
    }

    partial void OnSelectedTemperatureUnitDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrEmpty(value)) return;

        SelectedTemperatureUnit = value switch
        {
            "°C" => "°C",
            "°F" => "°F",
            _ => "System"
        };

        SaveCurrentSettings();
        UnitsChanged?.Invoke();
    }

    partial void OnSelectedWindSpeedUnitDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrEmpty(value)) return;

        SelectedWindSpeedUnit = value switch
        {
            "km/h" => "km/h",
            "mph" => "mph",
            _ => "System"
        };

        SaveCurrentSettings();
        UnitsChanged?.Invoke();
    }

    partial void OnSelectedTimeFormatDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrEmpty(value)) return;

        if (value.Contains("24"))
            SelectedTimeFormat = TimeFormatSetting.TwentyFourHour;
        else if (value.Contains("12"))
            SelectedTimeFormat = TimeFormatSetting.TwelveHour;
        else
            SelectedTimeFormat = TimeFormatSetting.System;

        SaveCurrentSettings();
        UnitsChanged?.Invoke();
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

    public TimeFormatSetting GetEffectiveTimeFormat()
    {
        if (SelectedTimeFormat != TimeFormatSetting.System)
            return SelectedTimeFormat;

        bool isCzech = _localizationService.GetEffectiveLanguage(SelectedLanguage) == "Czech";
        return isCzech ? TimeFormatSetting.TwentyFourHour : TimeFormatSetting.TwelveHour;
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
        _isUpdatingLocalization = true;

        try
        {
            bool isCzech = _localizationService.GetEffectiveLanguage(SelectedLanguage) == "Czech";

            // Texty nadpisů
            SettingsTitle = isCzech ? "Nastavení" : "Settings";
            ThemeLabel = isCzech ? "Motiv aplikace" : "App Theme";
            LanguageLabel = isCzech ? "Jazyk" : "Language";
            TemperatureUnitLabel = isCzech ? "Jednotky teploty" : "Temperature Units";
            WindSpeedUnitLabel = isCzech ? "Jednotky rychlosti větru" : "Wind Speed Units";
            TimeFormatLabel = isCzech ? "Formát času" : "Time Format";

            // Naplnění kolekcí
            AvailableThemes.Clear();
            AvailableThemes.Add(isCzech ? "Systém" : "System");
            AvailableThemes.Add(isCzech ? "Tmavý" : "Dark");
            AvailableThemes.Add(isCzech ? "Světlý" : "Light");

            AvailableLanguages.Clear();
            AvailableLanguages.Add(isCzech ? "Systém" : "System");
            AvailableLanguages.Add(isCzech ? "Angličtina" : "English");
            AvailableLanguages.Add(isCzech ? "Čeština" : "Czech");

            AvailableTemperatureUnits.Clear();
            AvailableTemperatureUnits.Add(isCzech ? "Systém" : "System");
            AvailableTemperatureUnits.Add("°C");
            AvailableTemperatureUnits.Add("°F");

            AvailableWindSpeedUnits.Clear();
            AvailableWindSpeedUnits.Add(isCzech ? "Systém" : "System");
            AvailableWindSpeedUnits.Add("km/h");
            AvailableWindSpeedUnits.Add("mph");

            AvailableTimeFormats.Clear();
            AvailableTimeFormats.Add(isCzech ? "Systém" : "System");
            AvailableTimeFormats.Add(isCzech ? "24-hod" : "24-hour");
            AvailableTimeFormats.Add(isCzech ? "12-hod" : "12-hour");

            // Obnova vybraných hodnot až PO naplnění kolekcí
            SelectedThemeDisplay = SelectedTheme switch
            {
                "Dark" => isCzech ? "Tmavý" : "Dark",
                "Light" => isCzech ? "Světlý" : "Light",
                _ => isCzech ? "Systém" : "System"
            };

            SelectedLanguageDisplay = SelectedLanguage switch
            {
                "Czech" => isCzech ? "Čeština" : "Czech",
                "English" => isCzech ? "Angličtina" : "English",
                _ => isCzech ? "Systém" : "System"
            };

            SelectedTemperatureUnitDisplay = SelectedTemperatureUnit switch
            {
                "°C" => "°C",
                "°F" => "°F",
                _ => isCzech ? "Systém" : "System"
            };

            SelectedWindSpeedUnitDisplay = SelectedWindSpeedUnit switch
            {
                "km/h" => "km/h",
                "mph" => "mph",
                _ => isCzech ? "Systém" : "System"
            };

            SelectedTimeFormatDisplay = SelectedTimeFormat switch
            {
                TimeFormatSetting.TwentyFourHour => isCzech ? "24-hod" : "24-hour",
                TimeFormatSetting.TwelveHour => isCzech ? "12-hod" : "12-hour",
                _ => isCzech ? "Systém" : "System"
            };
        }
        finally
        {
            _isUpdatingLocalization = false;
        }
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
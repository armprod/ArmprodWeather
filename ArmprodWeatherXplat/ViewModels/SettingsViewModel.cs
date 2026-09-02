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
using Avalonia.Threading;

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

    [ObservableProperty] private string _selectedTheme = "System";
    [ObservableProperty] private string _selectedLanguage = "System";
    [ObservableProperty] private string _selectedTemperatureUnit = "System";
    [ObservableProperty] private string _selectedWindSpeedUnit = "System";
    [ObservableProperty] private TimeFormatSetting _selectedTimeFormat = TimeFormatSetting.System;

    [ObservableProperty] private string _selectedThemeDisplay = "System";
    [ObservableProperty] private string _selectedLanguageDisplay = "System";
    [ObservableProperty] private string _selectedTemperatureUnitDisplay = "System";
    [ObservableProperty] private string _selectedWindSpeedUnitDisplay = "System";
    [ObservableProperty] private string _selectedTimeFormatDisplay = "System";

    // Sections Marks
    [ObservableProperty] private string _settingsTitle = "Settings";
    [ObservableProperty] private string _themeLabel = "Theme";
    [ObservableProperty] private string _languageLabel = "Language";
    [ObservableProperty] private string _temperatureUnitLabel = "Temperature Units";
    [ObservableProperty] private string _windSpeedUnitLabel = "Wind Speed Units";
    [ObservableProperty] private string _timeFormatLabel = "Time Format";

    // UI Collections
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
        if (_isUpdatingLocalization || string.IsNullOrWhiteSpace(value)) return;

        var newTheme = value switch
        {
            "Tmavý" or "Dark" => "Dark",
            "Světlý" or "Light" => "Light",
            _ => "System"
        };

        if (SelectedTheme == newTheme) return;

        SelectedTheme = newTheme;
        SaveCurrentSettings();
        ApplyTheme(SelectedTheme);
    }

    public static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;

        var targetVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        if (Application.Current.RequestedThemeVariant == targetVariant) return;

        Dispatcher.UIThread.Post(() =>
        {
            Application.Current.RequestedThemeVariant = targetVariant;
        }, DispatcherPriority.Background);
    }

    partial void OnSelectedTemperatureUnitDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrWhiteSpace(value)) return;

        SelectedTemperatureUnit = value switch
        {
            "°C" => "°C",
            "°F" => "°F",
            _ => "System"
        };

        SaveCurrentSettings();
        Dispatcher.UIThread.Post(() => UnitsChanged?.Invoke());
    }

    partial void OnSelectedWindSpeedUnitDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrWhiteSpace(value)) return;

        SelectedWindSpeedUnit = value switch
        {
            "km/h" => "km/h",
            "mph" => "mph",
            _ => "System"
        };

        SaveCurrentSettings();
        Dispatcher.UIThread.Post(() => UnitsChanged?.Invoke());
    }

    partial void OnSelectedTimeFormatDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrWhiteSpace(value)) return;

        if (value.Contains("24"))
            SelectedTimeFormat = TimeFormatSetting.TwentyFourHour;
        else if (value.Contains("12"))
            SelectedTimeFormat = TimeFormatSetting.TwelveHour;
        else
            SelectedTimeFormat = TimeFormatSetting.System;

        SaveCurrentSettings();
        Dispatcher.UIThread.Post(() => UnitsChanged?.Invoke());
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

    partial void OnSelectedLanguageDisplayChanged(string value)
    {
        if (_isUpdatingLocalization || string.IsNullOrWhiteSpace(value)) return;

        var newLanguage = value switch
        {
            "Čeština" or "Czech" => "Czech",
            "Angličtina" or "English" => "English",
            _ => "System"
        };

        if (SelectedLanguage == newLanguage) return;

        SelectedLanguage = newLanguage;
        SaveCurrentSettings();

        Dispatcher.UIThread.Post(() =>
        {
            _localizationService.ApplyLanguageCulture(SelectedLanguage);
            UpdateLocalizedTexts();
            LanguageChanged?.Invoke(SelectedLanguage);
        }, DispatcherPriority.Background);
    }

    public void UpdateLocalizedTexts()
    {
        _isUpdatingLocalization = true;
        try
        {
            bool isCzech = _localizationService.GetEffectiveLanguage(SelectedLanguage) == "Czech";

            // Labels
            SettingsTitle = isCzech ? "Nastavení" : "Settings";
            ThemeLabel = isCzech ? "Motiv aplikace" : "App Theme";
            LanguageLabel = isCzech ? "Jazyk" : "Language";
            TemperatureUnitLabel = isCzech ? "Jednotky teploty" : "Temperature Units";
            WindSpeedUnitLabel = isCzech ? "Jednotky rychlosti větru" : "Wind Speed Units";
            TimeFormatLabel = isCzech ? "Formát času" : "Time Format";

            // Collections
            UpdateCollection(AvailableThemes, new[] { isCzech ? "Systém" : "System", isCzech ? "Tmavý" : "Dark", isCzech ? "Světlý" : "Light" });
            UpdateCollection(AvailableLanguages, new[] { isCzech ? "Systém" : "System", isCzech ? "Angličtina" : "English", isCzech ? "Čeština" : "Czech" });
            UpdateCollection(AvailableTemperatureUnits, new[] { isCzech ? "Systém" : "System", "°C", "°F" });
            UpdateCollection(AvailableWindSpeedUnits, new[] { isCzech ? "Systém" : "System", "km/h", "mph" });
            UpdateCollection(AvailableTimeFormats, new[] { isCzech ? "Systém" : "System", isCzech ? "24-hod" : "24-hour", isCzech ? "12-hod" : "12-hour" });

            // Refresh Displayed Text
            SelectedThemeDisplay = SelectedTheme switch { "Dark" => isCzech ? "Tmavý" : "Dark", "Light" => isCzech ? "Světlý" : "Light", _ => isCzech ? "Systém" : "System" };
            SelectedLanguageDisplay = SelectedLanguage switch { "Czech" => isCzech ? "Čeština" : "Czech", "English" => isCzech ? "Angličtina" : "English", _ => isCzech ? "Systém" : "System" };
            SelectedTemperatureUnitDisplay = SelectedTemperatureUnit switch { "°C" => "°C", "°F" => "°F", _ => isCzech ? "Systém" : "System" };
            SelectedWindSpeedUnitDisplay = SelectedWindSpeedUnit switch { "km/h" => "km/h", "mph" => "mph", _ => isCzech ? "Systém" : "System" };
            SelectedTimeFormatDisplay = SelectedTimeFormat switch { TimeFormatSetting.TwentyFourHour => isCzech ? "24-hod" : "24-hour", TimeFormatSetting.TwelveHour => isCzech ? "12-hod" : "12-hour", _ => isCzech ? "Systém" : "System" };
        }
        finally
        {
            _isUpdatingLocalization = false;
        }
    }

    private static void UpdateCollection(ObservableCollection<string> collection, string[] newItems)
    {
        if (collection.SequenceEqual(newItems)) return;
        collection.Clear();
        foreach (var item in newItems)
        {
            collection.Add(item);
        }
    }

    private void SaveCurrentSettings()
    {
        var theme = SelectedTheme;
        var lang = SelectedLanguage;
        var temp = SelectedTemperatureUnit;
        var wind = SelectedWindSpeedUnit;
        var time = SelectedTimeFormat;

        System.Threading.Tasks.Task.Run(() =>
        {
            var settings = _settingsService.LoadSettings();
            settings.Theme = theme;
            settings.Language = lang;
            settings.TemperatureUnit = temp;
            settings.WindSpeedUnit = wind;
            settings.TimeFormat = time;
            _settingsService.SaveSettings(settings);
        });
    }
}
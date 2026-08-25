using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeather.Services;
using Avalonia;
using Avalonia.Styling;

namespace ArmprodWeather.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();

    [ObservableProperty] private bool _isSettingsOpen = false;
    [ObservableProperty] private string _selectedTheme = "Dark";
    [ObservableProperty] private string _selectedLanguage = "English";

    // Localized labels for Settings panel
    [ObservableProperty] private string _settingsTitle = "Settings";
    [ObservableProperty] private string _themeLabel = "Theme";
    [ObservableProperty] private string _languageLabel = "Language";

    public ObservableCollection<string> AvailableThemes { get; } = new() { "System", "Dark", "Light" };
    public ObservableCollection<string> AvailableLanguages { get; } = new() { "System", "English", "Czech" };

    // Událost pro informování MainViewModel o změně jazyka
    public event Action<string>? LanguageChanged;

    public void Initialize(string theme, string language)
    {
        SelectedTheme = theme;
        SelectedLanguage = language;
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

        var settings = _settingsService.LoadSettings();
        settings.Theme = value;
        _settingsService.SaveSettings(settings);
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        var settings = _settingsService.LoadSettings();
        settings.Language = value;
        _settingsService.SaveSettings(settings);

        _localizationService.ApplyLanguageCulture(value);
        UpdateLocalizedTexts();

        LanguageChanged?.Invoke(value);
    }

    public void UpdateLocalizedTexts()
    {
        bool isCzech = _localizationService.GetEffectiveLanguage(SelectedLanguage) == "Czech";

        SettingsTitle = isCzech ? "Nastavení" : "Settings";
        ThemeLabel = isCzech ? "Motiv aplikace" : "App Theme";
        LanguageLabel = isCzech ? "Jazyk" : "Language";
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
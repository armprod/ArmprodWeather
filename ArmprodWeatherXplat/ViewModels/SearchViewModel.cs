using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeatherXplat.Models;
using ArmprodWeatherXplat.Services;
using Avalonia.Threading;

namespace ArmprodWeatherXplat.ViewModels;

public partial class SearchViewModel : ViewModelBase
{
    private readonly LocationService _locationService = new();
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();
    private CancellationTokenSource? _searchCts;
    
    private MainViewModel? _mainViewModel;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearchOpen = false;
    [ObservableProperty] private LocationItem? _selectedSearchResult;
    [ObservableProperty] private string _searchPlaceholder = "Enter city name...";

    public ObservableCollection<LocationItem> SearchResults { get; } = new();

    public event Action<LocationItem>? LocationSelected;

    public void Initialize(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    public void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        
        SearchResults.Clear();
        SearchQuery = string.Empty;
        
        SelectedSearchResult = null;

        if (IsSearchOpen && _mainViewModel != null)
        {
            _mainViewModel.Settings.IsSettingsOpen = false;
        }
    }

    partial void OnSelectedSearchResultChanged(LocationItem? value)
    {
        if (value == null) return;

        var selected = value;

        IsSearchOpen = false;
        SearchResults.Clear();
        SearchQuery = string.Empty;

        SelectedSearchResult = null;

        LocationSelected?.Invoke(selected);
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

                var settings = _settingsService.LoadSettings();
                
                string effectiveLanguage = _localizationService.GetEffectiveLanguage(settings.Language);
                string langCode = _localizationService.GetApiLanguageCode(effectiveLanguage);

                var results = await _locationService.SearchLocationsAsync(value, langCode);

                if (token.IsCancellationRequested) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;

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
                // Ignore after fast writing
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        }, token);
    }
}
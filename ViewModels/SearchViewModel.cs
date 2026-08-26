using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeather.Models;
using ArmprodWeather.Services;

namespace ArmprodWeather.ViewModels;

public partial class SearchViewModel : ViewModelBase
{
    private readonly LocationService _locationService = new();
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearchOpen = false;
    [ObservableProperty] private LocationItem? _selectedSearchResult;
    [ObservableProperty] private string _searchPlaceholder = "Enter city name...";

    public ObservableCollection<LocationItem> SearchResults { get; } = new();

    // Event informate MainViewModel abour selected city
    public event Action<LocationItem>? LocationSelected;

    [RelayCommand]
    public void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        SearchResults.Clear();
        SearchQuery = string.Empty;
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
                string langCode = _localizationService.GetApiLanguageCode(settings.Language);

                var results = await _locationService.SearchLocationsAsync(value, langCode);

                if (token.IsCancellationRequested) return;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        }, token);
    }

    partial void OnSelectedSearchResultChanged(LocationItem? value)
    {
        if (value != null)
        {
            var selected = value;
            
            IsSearchOpen = false;
            SearchResults.Clear();
            SearchQuery = string.Empty;
            SelectedSearchResult = null;

            LocationSelected?.Invoke(selected);
        }
    }
}
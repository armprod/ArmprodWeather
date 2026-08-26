using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeather.Services;

namespace ArmprodWeather.ViewModels;

public partial class FavoritesViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();

    public event Action<FavoriteLocation>? FavoriteSelected;

    [ObservableProperty] private bool _isCurrentFavorite;
    public ObservableCollection<FavoriteLocation> Favorites { get; } = new();

    private double _currentLat;
    private double _currentLon;
    private string _currentCityName = string.Empty;

    public void Initialize(List<FavoriteLocation>? savedFavorites)
    {
        Favorites.Clear();
        if (savedFavorites != null)
        {
            foreach (var fav in savedFavorites)
            {
                Favorites.Add(fav);
            }
        }
    }

    public void UpdateCurrentLocation(double lat, double lon, string cityName)
    {
        _currentLat = lat;
        _currentLon = lon;
        _currentCityName = cityName;

        IsCurrentFavorite = Favorites.Any(f => 
            Math.Abs(f.Latitude - lat) < 0.01 && 
            Math.Abs(f.Longitude - lon) < 0.01);
    }

    [RelayCommand]
    private void ToggleCurrentFavorite()
    {
        if (string.IsNullOrEmpty(_currentCityName)) return;

        var existing = Favorites.FirstOrDefault(f => 
            Math.Abs(f.Latitude - _currentLat) < 0.01 && 
            Math.Abs(f.Longitude - _currentLon) < 0.01);

        if (existing != null)
        {
            Favorites.Remove(existing);
            IsCurrentFavorite = false;
        }
        else
        {
            Favorites.Add(new FavoriteLocation(_currentCityName, _currentLat, _currentLon));
            IsCurrentFavorite = true;
        }

        var settings = _settingsService.LoadSettings();
        settings.Favorites = Favorites.ToList();
        _settingsService.SaveSettings(settings);
    }

    [RelayCommand]
    private void SelectFavorite(FavoriteLocation location)
    {
        if (location != null)
        {
            FavoriteSelected?.Invoke(location);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmprodWeatherXplat.Models;
using ArmprodWeatherXplat.Services;

namespace ArmprodWeatherXplat.ViewModels;

public partial class FavoritesViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();

    public event Action<FavoriteLocation>? FavoriteSelected;

    [ObservableProperty] private bool _isCurrentFavorite;
    public ObservableCollection<FavoriteLocation> Favorites { get; } = new();

    private double _currentLat;
    private double _currentLon;
    private string _currentCityName = string.Empty;

    public void Initialize(List<FavoriteLocation>? savedFavorites, string currentCity, double lat, double lon)
    {
        Favorites.Clear();
        if (savedFavorites != null)
        {
            foreach (var fav in savedFavorites)
            {
                Favorites.Add(fav);
            }
        }

        UpdateCurrentLocation(lat, lon, currentCity);
    }

    public void UpdateCurrentLocation(double lat, double lon, string cityName)
    {
        _currentLat = lat;
        _currentLon = lon;
        _currentCityName = cityName ?? string.Empty;

        IsCurrentFavorite = Favorites.Any(IsMatch);
    }

    [RelayCommand]
    private void ToggleCurrentFavorite()
    {
        if (string.IsNullOrWhiteSpace(_currentCityName)) return;

        var existing = Favorites.FirstOrDefault(IsMatch);

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

        SaveFavorites();
    }

    [RelayCommand]
    private void SelectFavorite(FavoriteLocation location)
    {
        if (location != null)
        {
            FavoriteSelected?.Invoke(location);
        }
    }

    private bool IsMatch(FavoriteLocation f)
    {
        bool nameMatch = !string.IsNullOrWhiteSpace(_currentCityName) && 
                          f.Name.Equals(_currentCityName, StringComparison.OrdinalIgnoreCase);

        bool coordMatch = Math.Abs(f.Latitude - _currentLat) < 0.05 && 
                         Math.Abs(f.Longitude - _currentLon) < 0.05;

        return nameMatch || coordMatch;
    }

    private void SaveFavorites()
    {
        var settings = _settingsService.LoadSettings();
        settings.Favorites = Favorites.ToList();
        _settingsService.SaveSettings(settings);
    }
}
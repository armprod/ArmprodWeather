using Avalonia.Controls;
using ArmprodWeatherXplat.ViewModels;

namespace ArmprodWeatherXplat.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void OnRefreshRequested(object? sender, RefreshRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();

        try
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.RefreshCommand.ExecuteAsync(null);
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}
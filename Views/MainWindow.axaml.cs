using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ArmprodWeather.ViewModels;

namespace ArmprodWeather.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void SearchPanel_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty && e.GetNewValue<bool>())
        {
            Dispatcher.UIThread.Post(() =>
            {
                SearchInput.Focus();
            }, DispatcherPriority.Input);
        }
    }
}
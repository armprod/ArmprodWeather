using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ArmprodWeatherXplat.ViewModels;

namespace ArmprodWeatherXplat.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
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
using Avalonia.Controls;
using ArmprodWeather.ViewModels;

namespace ArmprodWeather.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
using Wavw.Services;
using Wavw.Model;
using System.Windows.Input;

namespace Wavw.Views;

public partial class WeatherPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;
    private readonly NavigationCommands _navigationCommands;

    public WeatherPage()
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel();
        _navigationCommands = new NavigationCommands();
        
        // Create a composite binding context
        var compositeContext = new CompositeBindingContext
        {
            ViewModel = _viewModel,
            NavigationCommands = _navigationCommands
        };
        BindingContext = compositeContext;
    }

    public WeatherPage(Beach selectedBeach)
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel(selectedBeach);
        _navigationCommands = new NavigationCommands();
        
        // Create a composite binding context
        var compositeContext = new CompositeBindingContext
        {
            ViewModel = _viewModel,
            NavigationCommands = _navigationCommands
        };
        BindingContext = compositeContext;
    }
}

// Composite binding context class
public class CompositeBindingContext
{
    public WeatherViewModel ViewModel { get; set; }
    public NavigationCommands NavigationCommands { get; set; }
}

// Navigation commands class
public class NavigationCommands
{
    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToWeatherCommand { get; }
    public ICommand NavigateToMapCommand { get; }

    public NavigationCommands()
    {
        NavigateToHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
        NavigateToWeatherCommand = new Command(async () => await Shell.Current.GoToAsync("//WeatherPage"));
        NavigateToMapCommand = new Command(async () => await Shell.Current.GoToAsync("//HomePage"));
    }
} 
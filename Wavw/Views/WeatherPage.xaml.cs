using Wavw.Services;
using Wavw.Model;
using System.Windows.Input;
using System.Diagnostics;

namespace Wavw.Views;

public partial class WeatherPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;
    private readonly NavigationCommands _navigationCommands;
    private readonly MarineWeatherService _marineWeatherService;
    private List<WeatherForecast> _forecasts;

    public WeatherPage()
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel();
        _navigationCommands = new NavigationCommands();
        _marineWeatherService = new MarineWeatherService();
        _forecasts = new List<WeatherForecast>();
        
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
        _marineWeatherService = new MarineWeatherService();
        _forecasts = new List<WeatherForecast>();
        
        var compositeContext = new CompositeBindingContext
        {
            ViewModel = _viewModel,
            NavigationCommands = _navigationCommands
        };
        BindingContext = compositeContext;

        if (selectedBeach != null)
        {
            LoadWeatherForecast(selectedBeach.Latitude, selectedBeach.Longitude);
        }
    }

    private async void LoadWeatherForecast(double latitude, double longitude)
    {
        try
        {
            _viewModel.IsLoadingForecast = true;
            var forecasts = await _marineWeatherService.GetMarineWeatherForecastAsync(latitude, longitude);
            
            if (forecasts != null)
            {
                _forecasts = forecasts;
                await MainThread.InvokeOnMainThreadAsync(() => UpdateForecastDisplay());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading weather forecast: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Forecast Error", 
                    "Unable to load weather forecast. Please try again later.", 
                    "OK");
            });
        }
        finally
        {
            _viewModel.IsLoadingForecast = false;
        }
    }

    private string GetWindDirection(string degrees)
    {
        if (string.IsNullOrEmpty(degrees)) return "N/A";
        if (int.TryParse(degrees, out int deg))
        {
            if (deg >= 337.5 || deg < 22.5) return "N";
            if (deg >= 22.5 && deg < 67.5) return "NE";
            if (deg >= 67.5 && deg < 112.5) return "E";
            if (deg >= 112.5 && deg < 157.5) return "SE";
            if (deg >= 157.5 && deg < 202.5) return "S";
            if (deg >= 202.5 && deg < 247.5) return "SW";
            if (deg >= 247.5 && deg < 292.5) return "W";
            if (deg >= 292.5 && deg < 337.5) return "NW";
        }
        return "N/A";
    }

    private void UpdateForecastDisplay()
    {
        try
        {
            var forecastContainer = ForecastContainer;
            if (forecastContainer != null)
            {
                forecastContainer.Children.Clear();
    
                if (_forecasts?.Any() == true)
                {
                    foreach (var forecast in _forecasts)
                    {
                        var weatherInfo = new VerticalStackLayout
                        {
                            Children =
                            {
                                new Label 
                                { 
                                    Text = DateTime.Parse(forecast.Date).ToString("dddd, MMM dd"),
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.Parse("#1B4965")
                                },
                                new Label { Text = $"High: {forecast.MaxTemp}°C", TextColor = Color.Parse("#1B4965") },
                                new Label { Text = $"Low: {forecast.MinTemp}°C", TextColor = Color.Parse("#1B4965") },
                                new Label { Text = $"Wind: {forecast.Hourly?[0].WindSpeed} km/h", TextColor = Color.Parse("#1B4965") },
                                new Label { Text = $"Wind Direction: {GetWindDirection(forecast.WindDirection)}", TextColor = Color.Parse("#1B4965") },
                                new Label { Text = $"Weather: {forecast.WeatherDesc}", TextColor = Color.Parse("#1B4965") }
                            }
                        };
    
                        // Add tide information only if available
                        if (forecast.TideHeight > 0 || !string.IsNullOrEmpty(forecast.TideType))
                        {
                            weatherInfo.Children.Add(new Label { Text = $"Tide Height: {forecast.TideHeight:F1}m", TextColor = Color.Parse("#1B4965") });
                            if (!string.IsNullOrEmpty(forecast.TideType))
                            {
                                weatherInfo.Children.Add(new Label { Text = $"Tide Type: {forecast.TideType}", TextColor = Color.Parse("#1B4965") });
                            }
                        }
    
                        var weatherIcon = new Image 
                        { 
                            Source = forecast.WeatherIconUrl,
                            HeightRequest = 50,
                            WidthRequest = 50,
                            HorizontalOptions = LayoutOptions.End
                        };
    
                        var grid = new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = GridLength.Star },
                                new ColumnDefinition { Width = GridLength.Auto }
                            }
                        };
    
                        grid.Add(weatherInfo, 0);
                        grid.Add(weatherIcon, 1);
    
                        var forecastFrame = new Frame
                        {
                            BorderColor = Colors.LightGray,
                            CornerRadius = 10,
                            Padding = new Thickness(10),
                            Content = grid
                        };
    
                        forecastContainer.Children.Add(forecastFrame);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating forecast display: {ex.Message}");
        }
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
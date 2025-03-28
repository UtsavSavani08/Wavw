using Wavw.Models;
using Wavw.Services;

namespace Wavw.Views;

public partial class WeatherPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public WeatherPage()
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel();
        BindingContext = _viewModel;
    }

    public WeatherPage(Beach selectedBeach)
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel(selectedBeach);
        BindingContext = _viewModel;
    }
} 
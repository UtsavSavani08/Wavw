using Wavw.Model;
using System.Windows.Input;

namespace Wavw.Views;

[QueryProperty(nameof(Beach), "Beach")]
public partial class BeachPage : ContentPage
{
	private Beach _beach;
	
	public Beach Beach
	{
		get => _beach;
		set
		{
			_beach = value;
			OnPropertyChanged();
		}
	}

	public ICommand NavigateToHomeCommand { get; }
	public ICommand NavigateToWeatherCommand { get; }
	public ICommand NavigateToMapCommand { get; }

	public BeachPage()
	{
		InitializeComponent();
		NavigateToHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
		NavigateToWeatherCommand = new Command(async () => await Shell.Current.GoToAsync("//WeatherPage"));
		NavigateToMapCommand = new Command(async () => await Shell.Current.GoToAsync("//HomePage"));
		BindingContext = this;
	}

	// In BeachPage.xaml.cs
	private async void OnBackButtonClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}

	private async void OnViewOnMapClicked(object sender, EventArgs e)
	{
		if (Beach != null)
		{
			var navigationParameter = new Dictionary<string, object>
			{
				{ "ShowBeachLocation", true },
				{ "BeachLatitude", Beach.Latitude },
				{ "BeachLongitude", Beach.Longitude },
				{ "BeachName", Beach.Name },
				{ "BeachCity", Beach.City },
				{ "BeachState", Beach.State },
				{ "BeachRating", Beach.Rating },
				{ "BeachCleanliness", Beach.Cleanliness },
				{ "BeachBestSeason", Beach.BestSeason },
				{ "BeachMainAttractions", Beach.MainAttractions },
				{ "BeachImageUrl", Beach.ImageUrl }
			};

			await Shell.Current.GoToAsync("//HomePage", navigationParameter);
		}
	}

    // Update the OnClickCheckWeather method
    private async void OnClickCheckWeather(object sender, TappedEventArgs e)
    {
        if (Beach != null)
        {
            try
            {
                var weatherPage = new WeatherPage(Beach);
                await Navigation.PushAsync(weatherPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing weather info: {ex.Message}");
                await DisplayAlert("Error", 
                    "Unable to show weather information. Please try again.", 
                    "OK");
            }
        }
    }
}
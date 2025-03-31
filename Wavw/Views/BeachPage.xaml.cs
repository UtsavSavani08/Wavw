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
				{ "BeachName", Beach.Name }
			};

			await Shell.Current.GoToAsync("//HomePage", navigationParameter);
		}
	}
}
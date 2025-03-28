using Wavw.Models;
using Microsoft.Maui.Controls;

namespace Wavw.Views;

public partial class BeachPage : ContentPage
{
	private readonly Beach _beach;

	public BeachPage(Beach beach)
	{
		InitializeComponent();
		_beach = beach;
		LoadBeachDetails();
	}

	private void LoadBeachDetails()
	{
		// Update UI elements with beach details
		BeachNameLabel.Text = _beach.Name;
		LocationLabel.Text = $"{_beach.City}, {_beach.State}";
		RatingLabel.Text = _beach.Rating;
		StateLabel.Text = _beach.State;
		CityLabel.Text = _beach.City;
		CleanlinessLabel.Text = _beach.Cleanliness;
		BestSeasonLabel.Text = _beach.BestSeason;

		// Set the beach image if available
		try
		{
			if (!string.IsNullOrEmpty(_beach.ImageUrl))
			{
				BeachImage.Source = new UriImageSource
				{
					Uri = new Uri(_beach.ImageUrl),
					CachingEnabled = true,
					CacheValidity = TimeSpan.FromDays(7)
				};
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
			// Keep the default image if there's an error
		}

		// Load attractions
		AttractionsLayout.Children.Clear();
		if (!string.IsNullOrEmpty(_beach.MainAttractions))
		{
			var attractions = _beach.MainAttractions.Split(',', StringSplitOptions.RemoveEmptyEntries);
			foreach (var attraction in attractions)
			{
				var attractionLayout = new HorizontalStackLayout
				{
					Spacing = 8
				};

				var bullet = new BoxView
				{
					WidthRequest = 4,
					HeightRequest = 4,
					CornerRadius = 2,
					BackgroundColor = Color.FromArgb("#1B4965"),
					VerticalOptions = LayoutOptions.Center
				};

				var attractionLabel = new Label
				{
					Text = attraction.Trim(),
					FontSize = 14,
					TextColor = Colors.Black
				};

				attractionLayout.Children.Add(bullet);
				attractionLayout.Children.Add(attractionLabel);
				AttractionsLayout.Children.Add(attractionLayout);
			}
		}
	}

	private async void OnBackButtonTapped(object sender, TappedEventArgs e)
	{
		await Navigation.PopAsync();
	}

	private async void OnViewMapClicked(object sender, EventArgs e)
	{
		// Handle map navigation using _beach.Latitude and _beach.Longitude
	}

	private async void OnCheckWeatherClicked(object sender, EventArgs e)
	{
		// Handle weather check using _beach.Latitude and _beach.Longitude
	}
}
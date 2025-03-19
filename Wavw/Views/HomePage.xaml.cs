using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Wavw.Model;
using Wavw.Services;

namespace Wavw.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly BeachService _beachService;

        public HomePage()
        {
            InitializeComponent();
            _beachService = new BeachService();
            GetCurrentLocation();
        }

        // Get user's current location and find nearby beaches
        private async void GetCurrentLocation()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(10)
                    });
                }

                if (location != null)
                {
                    BeachMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                        new Location(location.Latitude, location.Longitude),
                        Distance.FromKilometers(10)));

                    var beaches = await _beachService.GetBeachesNearbyAsync(location.Latitude, location.Longitude);
                    DisplayBeachesOnMap(beaches);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting location: {ex.Message}");
                await DisplayAlert("Error", "Could not retrieve location. Enable GPS and try again.", "OK");
            }
        }

        // Search a beach and update map
        private async void OnSearchBeach(object sender, EventArgs e)
        {
            try
            {
                var searchText = BeachSearchBar.Text;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var beaches = await _beachService.GetBeachByNameAsync(searchText);
                    DisplayBeachesOnMap(beaches);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching beach: {ex.Message}");
                await DisplayAlert("Search Error", "Could not find beach. Try again later.", "OK");
            }
        }

        // Display beaches as pins on the map
        private void DisplayBeachesOnMap(List<Beach> beaches)
        {
            try
            {
                BeachMap.Pins.Clear();
                foreach (var beach in beaches)
                {
                    BeachMap.Pins.Add(new Pin
                    {
                        Label = beach.Name,
                        Address = $"{beach.MainAttractions}, Cleanliness: {beach.Cleanliness}",
                        Location = new Location(beach.Latitude, beach.Longitude)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying beaches: {ex.Message}");
                DisplayAlert("Error", "Could not display beaches on the map.", "OK");
            }
        }
    }
}

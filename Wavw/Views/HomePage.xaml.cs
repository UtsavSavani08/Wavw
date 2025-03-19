using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Wavw.Model;
using Wavw.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace Wavw.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly IGeolocation _geolocation;
        private readonly BeachService _beachService;
        private Microsoft.Maui.Controls.Maps.Map _map;
        private Beach? _selectedBeach;
        private Location? _currentLocation;
        private Frame? _detailsPanel;

        public ICommand SearchCommand { get; }
        public ICommand GetCurrentLocationCommand { get; }
        public ICommand CloseDetailsCommand { get; }

        public Beach? SelectedBeach
        {
            get => _selectedBeach;
            set
            {
                _selectedBeach = value;
                OnPropertyChanged();
                if (value != null && _currentLocation != null)
                {
                    SelectedBeachDistance = value.DistanceFromUser(_currentLocation);
                }
            }
        }

        public double SelectedBeachDistance { get; private set; }

        public HomePage(IGeolocation geolocation, BeachService beachService)
        {
            InitializeComponent();
            _geolocation = geolocation;
            _beachService = beachService;
            _map = BeachMap;
            _detailsPanel = this.FindByName<Frame>("DetailsPanel");

            SearchCommand = new Command<string>(async (term) => await SearchBeach(term));
            GetCurrentLocationCommand = new Command(async () => await GetCurrentLocation());
            CloseDetailsCommand = new Command(async () => await OnCloseDetailsClicked(null, null));

            BindingContext = this;
        }

        private async Task SearchBeach(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return;

            try
            {
                // Show loading indicator
                IsBusy = true;

                var beach = await _beachService.SearchBeachByName(searchTerm);
                if (beach != null)
                {
                    SelectedBeach = beach;
                    ClearMapPins();
                    AddBeachPin(beach);
                    _map.MoveToRegion(MapSpan.FromCenterAndRadius(
                        new Location(beach.Latitude, beach.Longitude),
                        Distance.FromKilometers(5)));
                    
                    // Show the details panel
                    if (_detailsPanel != null)
                    {
                        await _detailsPanel.TranslateTo(0, 0, 250, Easing.SpringOut);
                    }
                }
                else
                {
                    await DisplayAlert("Not Found", 
                        $"No beach found matching '{searchTerm}'. Please check the spelling.", 
                        "OK");
                }
            }
            catch (InvalidOperationException ex)
            {
                await DisplayAlert("Invalid Search", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while searching for the beach. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GetCurrentLocation()
        {
            try
            {
                IsBusy = true;
                var location = await _geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(5)
                });

                if (location != null)
                {
                    _currentLocation = location;
                    ClearMapPins();
                    AddUserLocationPin(location);
                    await CenterMapOn(location.Latitude, location.Longitude);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Unable to get your location. Please check your location settings.", "OK");
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearMapPins()
        {
            _map.Pins.Clear();
        }

        private void AddUserLocationPin(Location location)
        {
            var pin = new Pin
            {
                Label = "You are here",
                Location = new Location(location.Latitude, location.Longitude),
                Type = PinType.SearchResult
            };
            _map.Pins.Add(pin);
        }

        private void AddBeachPin(Beach beach)
        {
            var pin = new Pin
            {
                Label = beach.Name,
                Location = new Location(beach.Latitude, beach.Longitude),
                Type = PinType.Place
            };

            pin.MarkerClicked += (s, e) =>
            {
                SelectedBeach = beach;
                if (_detailsPanel != null)
                {
                    _detailsPanel.TranslateTo(0, 0, 250, Easing.SpringOut);
                }
            };

            _map.Pins.Add(pin);
        }

        private async Task CenterMapOn(double latitude, double longitude)
        {
            var location = new Location(latitude, longitude);
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(5)));
        }

        private async Task OnCloseDetailsClicked(object sender, EventArgs e)
        {
            if (_detailsPanel != null)
            {
                await _detailsPanel.TranslateTo(0, 300, 250, Easing.SpringIn);
            }
        }
    }
}

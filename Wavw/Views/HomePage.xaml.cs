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
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System.Linq;

namespace Wavw.Views
{
    [QueryProperty(nameof(ShowBeachLocation), "ShowBeachLocation")]
    [QueryProperty(nameof(BeachLatitude), "BeachLatitude")]
    [QueryProperty(nameof(BeachLongitude), "BeachLongitude")]
    [QueryProperty(nameof(BeachName), "BeachName")]
    [QueryProperty(nameof(BeachCity), "BeachCity")]
    [QueryProperty(nameof(BeachState), "BeachState")]
    [QueryProperty(nameof(BeachRating), "BeachRating")]
    [QueryProperty(nameof(BeachCleanliness), "BeachCleanliness")]
    [QueryProperty(nameof(BeachBestSeason), "BeachBestSeason")]
    [QueryProperty(nameof(BeachMainAttractions), "BeachMainAttractions")]
    [QueryProperty(nameof(BeachImageUrl), "BeachImageUrl")]
    public partial class HomePage : ContentPage
    {
        private readonly IGeolocation _geolocation;
        private readonly BeachService _beachService;
        private Microsoft.Maui.Controls.Maps.Map _map;
        private Beach? _selectedBeach;
        private Location? _currentLocation;
        private Frame? _detailsPanel;
        private bool _isLocationPermissionGranted;
        private const uint AnimationDuration = 250;
        private const string LocationPermissionRequestedKey = "LocationPermissionRequested";

        public bool ShowBeachLocation { get; set; }
        public double BeachLatitude { get; set; }
        public double BeachLongitude { get; set; }
        public string BeachName { get; set; }
        
        // Add new properties for beach details
        public string BeachCity { get; set; }
        public string BeachState { get; set; }
        public string BeachRating { get; set; }
        public string BeachCleanliness { get; set; }
        public string BeachBestSeason { get; set; }
        public string BeachMainAttractions { get; set; }
        public string BeachImageUrl { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand GetCurrentLocationCommand { get; }
        public ICommand CloseDetailsCommand { get; }
        public ICommand GetDirectionsCommand { get; }
        public ICommand GetWeatherCommand { get; }
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToWeatherCommand { get; }
        public ICommand NavigateToMapCommand { get; }

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
                    ShowDetailsPanel();
                }
            }
        }

        public double SelectedBeachDistance { get; private set; }

        public string DisplayBeachName => _selectedBeach?.Name.ToUpper() ?? string.Empty;

        // Add the command property
        public ICommand ShowBeachInfoCommand { get; }

        // Update the constructor to initialize the command
        public HomePage(IGeolocation geolocation, BeachService beachService)
        {
            InitializeComponent();
            _geolocation = geolocation;
            _beachService = beachService;
            _map = BeachMap;
            _detailsPanel = this.FindByName<Frame>("DetailsPanel");

            SearchCommand = new Command<string>(async (term) => await SearchBeach(term));
            GetCurrentLocationCommand = new Command(async () => await GetCurrentLocation());
            CloseDetailsCommand = new Command(async () => await HideDetailsPanel());
            GetDirectionsCommand = new Command(async () => await OpenDirections());
            GetWeatherCommand = new Command(async () => await ShowWeatherInfo());
            ShowBeachInfoCommand = new Command(async () => await ShowBeachInfo());
            NavigateToHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
            NavigateToWeatherCommand = new Command(async () => await Shell.Current.GoToAsync("//WeatherPage"));
            NavigateToMapCommand = new Command(async () => await Shell.Current.GoToAsync("//HomePage"));

            BindingContext = this;

            // Initialize the details panel position
            if (_detailsPanel != null)
            {
                _detailsPanel.TranslationY = 1000;
            }

            // Handle navigation parameters when the page appears
            this.Loaded += async (s, e) =>
            {
                if (ShowBeachLocation)
                {
                    // Create a complete beach object from navigation parameters
                    var beach = new Beach
                    {
                        Name = BeachName,
                        Latitude = BeachLatitude,
                        Longitude = BeachLongitude,
                        City = BeachCity,
                        State = BeachState,
                        Rating = BeachRating,
                        Cleanliness = BeachCleanliness,
                        BestSeason = BeachBestSeason,
                        MainAttractions = BeachMainAttractions,
                        ImageUrl = BeachImageUrl
                    };

                    // Clear existing pins and add the beach pin
                    ClearMapPins();
                    AddBeachPin(beach);
                    await CenterMapOn(BeachLatitude, BeachLongitude);
                    
                    // Set as selected beach to show details
                    SelectedBeach = beach;
                }
                else
                {
                    // Request location access for normal flow
                    await CheckLocationPermission();
                }
            };
        }

        private async Task CheckLocationPermission()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Required", 
                            "Location permission is required to show nearby beaches. Please enable it in your device settings.", 
                            "OK");
                        return;
                    }
                }

                _isLocationPermissionGranted = status == PermissionStatus.Granted;

                if (_isLocationPermissionGranted)
                {
                    // Check if GPS is enabled
                    var location = await _geolocation.GetLastKnownLocationAsync();
                    if (location == null)
                    {
                        // Try getting current location with a timeout
                        try
                        {
                            location = await _geolocation.GetLocationAsync(new GeolocationRequest
                            {
                                DesiredAccuracy = GeolocationAccuracy.Medium,
                                Timeout = TimeSpan.FromSeconds(5)
                            });
                        }
                        catch (FeatureNotEnabledException)
                        {
                            await DisplayAlert("GPS Required", 
                                "Please enable GPS/Location services to use this feature.", 
                                "OK");
                            // Open location settings
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                await Launcher.OpenAsync("android.settings.LOCATION_SOURCE_SETTINGS");
                            }
                            else if (DeviceInfo.Platform == DevicePlatform.iOS)
                            {
                                await Launcher.OpenAsync(new Uri("app-settings:"));
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
                            await DisplayAlert("Location Error", 
                                "Unable to get your location. Please check your device settings.", 
                                "OK");
                            return;
                        }
                    }

                    if (location != null)
                    {
                        _currentLocation = location;
                        await CenterMapOn(location.Latitude, location.Longitude);
                        await ShowNearbyBeaches();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission check error: {ex.Message}");
                await DisplayAlert("Error", 
                    "Unable to access location services. Please check your device settings.", 
                    "OK");
            }
        }

        private async Task GetCurrentLocation()
        {
            if (!_isLocationPermissionGranted)
            {
                await CheckLocationPermission();
                return;
            }

            try
            {
                IsBusy = true;

                var request = new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(5)
                };

                var location = await _geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    _currentLocation = location;
                    await CenterMapOn(location.Latitude, location.Longitude);
                    await ShowNearbyBeaches();
                }
                else
                {
                    await DisplayAlert("Location Error", 
                        "Unable to get your location. Please check your GPS settings.", 
                        "OK");
                }
            }
            catch (FeatureNotEnabledException)
            {
                await DisplayAlert("GPS Required", 
                    "Please enable GPS/Location services to use this feature.", 
                    "OK");
                // Open location settings
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    await Launcher.OpenAsync("android.settings.LOCATION_SOURCE_SETTINGS");
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    await Launcher.OpenAsync(new Uri("app-settings:"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
                await DisplayAlert("Error", 
                    "Unable to get your location. Please try again.", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShowNearbyBeaches()
        {
            if (_currentLocation == null) return;

            try
            {
                ClearMapPins();
                
                // Add user's current location pin
                AddUserLocationPin(_currentLocation);

                var nearestBeaches = await _beachService.GetNearestBeaches(_currentLocation, 3);
                if (nearestBeaches != null && nearestBeaches.Any())
                {
                    // Add all beach pins
                    foreach (var beach in nearestBeaches)
                    {
                        AddBeachPin(beach);
                    }

                    // Calculate the bounds to show all pins
                    var locations = new List<Location> { _currentLocation };
                    locations.AddRange(nearestBeaches.Select(b => new Location(b.Latitude, b.Longitude)));
                    await CenterMapOnPins(locations);

                    // Show details for the nearest beach
                    SelectedBeach = nearestBeaches.First();
                    await ShowDetailsPanel();
                }
                else
                {
                    await DisplayAlert("No Beaches Found", 
                        "No beaches found. Please try again.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing nearby beaches: {ex.Message}");
                await DisplayAlert("Error", 
                    "Unable to find nearby beaches. Please try again.", 
                    "OK");
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
                Type = PinType.Place
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

            pin.MarkerClicked += async (s, e) =>
            {
                SelectedBeach = beach;
                await ShowDetailsPanel();
                e.HideInfoWindow = true; // Hide the default info window since we're showing our custom panel
            };

            _map.Pins.Add(pin);
        }

        private async Task CenterMapOn(double latitude, double longitude)
        {
            var location = new Location(latitude, longitude);
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(5)));
        }

        private async Task CenterMapOnPins(List<Location> locations)
        {
            if (!locations.Any()) return;

            // Calculate the bounding box for all locations
            var minLat = locations.Min(l => l.Latitude);
            var maxLat = locations.Max(l => l.Latitude);
            var minLon = locations.Min(l => l.Longitude);
            var maxLon = locations.Max(l => l.Longitude);

            // Calculate center point
            var centerLat = (minLat + maxLat) / 2;
            var centerLon = (minLon + maxLon) / 2;

            // Calculate the distance to determine radius
            var maxDistance = locations.Max(l => 
                Location.CalculateDistance(
                    l, 
                    new Location(centerLat, centerLon), 
                    DistanceUnits.Kilometers
                )
            );

            // Add some padding to the radius
            var radius = maxDistance * 1.5;

            // Ensure minimum zoom level
            radius = Math.Max(radius, 1);

            _map.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    new Location(centerLat, centerLon),
                    Distance.FromKilometers(radius)
                )
            );
        }

        private async Task ShowDetailsPanel()
        {
            if (_detailsPanel != null)
            {
                _detailsPanel.IsVisible = true;
                var nameLabel = _detailsPanel.FindByName<Label>("BeachNameLabel");
                if (nameLabel != null && _selectedBeach != null)
                {
                    nameLabel.Text = _selectedBeach.Name.ToUpper();
                }
                await _detailsPanel.TranslateTo(0, 0, AnimationDuration, Easing.SpringOut);
            }
        }

        private async Task HideDetailsPanel()
        {
            if (_detailsPanel != null)
            {
                await _detailsPanel.TranslateTo(0, 1000, AnimationDuration, Easing.SpringIn);
                _detailsPanel.IsVisible = false;
                SelectedBeach = null;
            }
        }

        private async Task OpenDirections()
        {
            if (SelectedBeach == null) return;

            try
            {
                var location = $"{SelectedBeach.Latitude},{SelectedBeach.Longitude}";
                var name = Uri.EscapeDataString(SelectedBeach.Name);
                
                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    await Launcher.OpenAsync($"http://maps.apple.com/?q={name}&ll={location}");
                }
                else
                {
                    await Launcher.OpenAsync($"geo:{location}?q={location}({name})");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Unable to open directions. Please try again.", "OK");
                System.Diagnostics.Debug.WriteLine($"Error opening directions: {ex.Message}");
            }
        }

        // Update the ShowBeachInfo method
        private async Task ShowBeachInfo()
        {
            if (SelectedBeach != null)
            {
                try
                {
                    var navigationParameter = new Dictionary<string, object>
                    {
                        { "Beach", SelectedBeach }
                    };
                    await Shell.Current.GoToAsync($"BeachPage", navigationParameter);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error navigating to beach info: {ex.Message}");
                    await DisplayAlert("Error", "Unable to show beach information. Please try again.", "OK");
                }
            }
        }

        private async Task SearchBeach(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting search for: {searchTerm}");
                // Show loading indicator
                IsBusy = true;

                var beach = await _beachService.SearchBeachByName(searchTerm);
                System.Diagnostics.Debug.WriteLine($"Search result: {(beach != null ? $"Found {beach.Name}" : "Not found")}");
                
                if (beach != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Selected beach: {beach.Name} at {beach.Latitude}, {beach.Longitude}");
                    SelectedBeach = beach;
                    ClearMapPins();
                    AddBeachPin(beach);
                    await CenterMapOn(beach.Latitude, beach.Longitude);
                    
                    // Show the details panel
                    await ShowDetailsPanel();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No beach found, showing alert");
                    await DisplayAlert("Not Found", 
                        $"No beach found matching '{searchTerm}'. Please check the spelling.", 
                        "OK");
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid search: {ex.Message}");
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

        private async Task ShowWeatherInfo()
        {
            if (SelectedBeach == null) return;

            try
            {
                var weatherPage = new WeatherPage(SelectedBeach);
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

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

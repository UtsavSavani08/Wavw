using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Wavw.Model;
using System.Text.Json.Serialization;

namespace Wavw.Services
{
    public class WeatherViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private const string API_KEY = "d61e2c4c-05a5-11f0-a906-0242ac130003-d61e2cc4-05a5-11f0-a906-0242ac130003"; // Replace with your actual API key
        private readonly BeachService _beachService;
        private const string BaseUrl = "https://api.stormglass.io/v2/weather/";
        
        private string _beachName;
        private double _waveHeight;
        private double _seaTemperature;
        private string _windConditions;
        private DateTime _lastUpdated;
        private Beach _beach;
        private bool _hasBeachSelected;

        public ICommand SearchCommand { get; }

        public bool HasBeachSelected
        {
            get => _hasBeachSelected;
            set
            {
                _hasBeachSelected = value;
                OnPropertyChanged();
            }
        }

        public string BeachName
        {
            get => _beachName;
            set
            {
                _beachName = value;
                OnPropertyChanged();
            }
        }

        public double WaveHeight
        {
            get => _waveHeight;
            set
            {
                _waveHeight = value;
                OnPropertyChanged();
            }
        }

        public double SeaTemperature
        {
            get => _seaTemperature;
            set
            {
                _seaTemperature = value;
                OnPropertyChanged();
            }
        }

        public string WindConditions
        {
            get => _windConditions;
            set
            {
                _windConditions = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                _lastUpdated = value;
                OnPropertyChanged();
            }
        }

        public WeatherViewModel()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _beachService = new BeachService();
            HasBeachSelected = false;
            SearchCommand = new Command<string>(async (term) => await SearchBeach(term));
        }

        public WeatherViewModel(Beach beach) : this()
        {
            _beach = beach;
            BeachName = beach.Name;
            HasBeachSelected = true;
            LoadWeatherDataAsync(beach.Latitude, beach.Longitude).ConfigureAwait(false);
        }

        private async Task SearchBeach(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return;

            try
            {
                var beach = await _beachService.SearchBeachByName(searchTerm);
                
                if (beach != null)
                {
                    _beach = beach;
                    BeachName = beach.Name;
                    HasBeachSelected = true;
                    await LoadWeatherDataAsync(beach.Latitude, beach.Longitude);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Not Found", 
                        $"No beach found matching '{searchTerm}'. Please check the spelling.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", 
                    "An error occurred while searching for the beach. Please try again.", 
                    "OK");
            }
        }

        private async Task LoadWeatherDataAsync(double latitude, double longitude)
        {
            try
            {
                // Validate coordinates
                if (latitude == 0 || longitude == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Error: Invalid coordinates - Latitude or Longitude is 0");
                    throw new ArgumentException("Invalid coordinates provided");
                }

                System.Diagnostics.Debug.WriteLine($"Starting API request for coordinates: Lat={latitude}, Long={longitude}");
                
                var parameters = "waveHeight,waterTemperature,windSpeed,windDirection";
                var url = $"{BaseUrl}point?lat={latitude}&lng={longitude}&params={parameters}";
                
                System.Diagnostics.Debug.WriteLine($"Full API URL: {url}");
                System.Diagnostics.Debug.WriteLine($"API Key being used: {API_KEY}");

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", API_KEY);

                // Log all request headers
                foreach (var header in request.Headers)
                {
                    System.Diagnostics.Debug.WriteLine($"Request Header: {header.Key} = {string.Join(", ", header.Value)}");
                }

                System.Diagnostics.Debug.WriteLine("Sending API request...");
                var response = await _httpClient.SendAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"Response Status Code: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response Headers:");
                foreach (var header in response.Headers)
                {
                    System.Diagnostics.Debug.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response: {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API Error Status Code: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"API Error Response: {jsonResponse}");
                    
                    // Check for specific error cases
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("API Key is invalid or expired");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        throw new Exception("API rate limit exceeded");
                    }
                    
                    throw new Exception($"API Error: {response.StatusCode} - {jsonResponse}");
                }

                System.Diagnostics.Debug.WriteLine("Deserializing API response...");
                var apiResponse = JsonSerializer.Deserialize<StormglassResponse>(jsonResponse);
                
                if (apiResponse?.Hours == null || !apiResponse.Hours.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Error: No hours data in API response");
                    throw new Exception("No weather data available in the API response");
                }

                System.Diagnostics.Debug.WriteLine($"Number of hours in response: {apiResponse.Hours.Count}");
                var currentData = apiResponse.Hours[0];
                
                // Log the raw values before processing
                System.Diagnostics.Debug.WriteLine($"Raw Wave Height - NOAA: {currentData.WaveHeight?.Noaa}, SG: {currentData.WaveHeight?.Sg}");
                System.Diagnostics.Debug.WriteLine($"Raw Water Temp - NOAA: {currentData.WaterTemperature?.Noaa}, SG: {currentData.WaterTemperature?.Sg}");
                System.Diagnostics.Debug.WriteLine($"Raw Wind Speed - NOAA: {currentData.WindSpeed?.Noaa}, SG: {currentData.WindSpeed?.Sg}");
                System.Diagnostics.Debug.WriteLine($"Raw Wind Direction - NOAA: {currentData.WindDirection?.Noaa}, SG: {currentData.WindDirection?.Sg}");

                // Update the UI values
                WaveHeight = currentData.WaveHeight?.Noaa ?? currentData.WaveHeight?.Sg ?? 0;
                SeaTemperature = currentData.WaterTemperature?.Noaa ?? currentData.WaterTemperature?.Sg ?? 0;
                var windSpeed = currentData.WindSpeed?.Noaa ?? currentData.WindSpeed?.Sg ?? 0;
                var windDir = currentData.WindDirection?.Noaa ?? currentData.WindDirection?.Sg ?? 0;
                WindConditions = $"{windSpeed:F1} m/s from {GetWindDirection(windDir)}";
                LastUpdated = DateTime.Parse(currentData.Time);
                HasBeachSelected = true;

                System.Diagnostics.Debug.WriteLine($"Successfully updated weather data for {BeachName}");
                System.Diagnostics.Debug.WriteLine($"Final values - Wave Height: {WaveHeight}, Sea Temp: {SeaTemperature}, Wind: {WindConditions}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading weather data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                var errorMessage = ex.Message.Contains("API Key") 
                    ? "Invalid API key. Please check your API configuration."
                    : "Unable to load weather data. Please try again later.";
                    
                await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
            }
        }

        private string GetWindDirection(double degrees)
        {
            string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = (int)((degrees + 22.5) % 360 / 45);
            return directions[index];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StormglassResponse
    {
        [JsonPropertyName("hours")]
        public List<StormglassHourData> Hours { get; set; }

        [JsonPropertyName("meta")]
        public StormglassMeta Meta { get; set; }
    }

    public class StormglassHourData
    {
        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("waveHeight")]
        public StormglassValues WaveHeight { get; set; }

        [JsonPropertyName("waterTemperature")]
        public StormglassValues WaterTemperature { get; set; }

        [JsonPropertyName("windSpeed")]
        public StormglassValues WindSpeed { get; set; }

        [JsonPropertyName("windDirection")]
        public StormglassValues WindDirection { get; set; }
    }

    public class StormglassValues
    {
        [JsonPropertyName("noaa")]
        public double? Noaa { get; set; }

        [JsonPropertyName("sg")]
        public double? Sg { get; set; }
    }

    public class StormglassMeta
    {
        [JsonPropertyName("dailyQuota")]
        public int DailyQuota { get; set; }

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }

        [JsonPropertyName("requestCount")]
        public int RequestCount { get; set; }
    }
} 
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Wavw.Model;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Wavw.Services
{
    public class WeatherViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly BeachService _beachService;
        private const string BaseUrl = "https://api.stormglass.io/v2/weather/point";
        
        private string _beachName;
        private double _waveHeight;
        private double _seaTemperature;
        private string _windConditions;
        private DateTime _lastUpdated;
        private Beach _beach;
        private bool _hasBeachSelected;
        private bool _isLoading;
        private bool _isBusy;

        public ICommand SearchCommand { get; }
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public Beach Beach => _beach;

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

        // Add these properties
        private List<WeatherForecast> _forecasts;
        private bool _isLoadingForecast;
        private readonly MarineWeatherService _marineWeatherService;

        // In WeatherViewModel, modify the Forecasts property
        public List<WeatherForecast> Forecasts
        {
            get => _forecasts;
            set
            {
                _forecasts = value;
                OnPropertyChanged();
                // Use the correct MainThread method
                MainThread.InvokeOnMainThreadAsync(() => 
                    WeatherForecastUpdated?.Invoke(this, _forecasts));
            }
        }
        
        // Add this event at class level
        public event EventHandler<List<WeatherForecast>> WeatherForecastUpdated;

        public bool IsLoadingForecast
        {
            get => _isLoadingForecast;
            set
            {
                _isLoadingForecast = value;
                OnPropertyChanged();
            }
        }

        // Update constructor
        public WeatherViewModel()
        {
            try
            {
                string configPath = Path.Combine(FileSystem.Current.AppDataDirectory, "appsettings.json");

                // Ensure config file exists in app data directory
                if (!File.Exists(configPath))
                {
                    var assembly = typeof(WeatherViewModel).Assembly;
                    using var stream = assembly.GetManifestResourceStream("Wavw.appsettings.json");
                    using var reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    File.WriteAllText(configPath, content);
                }
                    
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(FileSystem.Current.AppDataDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                _apiKey = configuration["ApiKeys:StormGlass"];
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
                _marineWeatherService = new MarineWeatherService();
                _beachService = new BeachService();
                HasBeachSelected = false;
                SearchCommand = new Command<string>(async (term) => await SearchBeach(term));
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration error: {ex.Message}");
                throw new Exception("Failed to initialize weather service. Please check your configuration.", ex);
            }
        }

        private async Task SearchBeach(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return;
        
            try
            {
                IsLoading = true;
                IsLoadingForecast = true;
                var beach = await _beachService.SearchBeachByName(searchTerm);
                
                if (beach != null)
                {
                    _beach = beach;
                    BeachName = beach.Name;
                    HasBeachSelected = true;
                    
                    // Load both weather and forecast data in one place
                    await Task.WhenAll(
                        LoadWeatherDataAsync(beach.Latitude, beach.Longitude),
                        LoadForecastDataAsync(beach.Latitude, beach.Longitude)
                    );
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
                Debug.WriteLine($"Search error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", 
                    "An error occurred while searching for the beach. Please try again.", 
                    "OK");
            }
            finally
            {
                IsLoading = false;
                IsLoadingForecast = false;
            }
        }

        // Update the constructor with beach parameter
        public WeatherViewModel(Beach beach) : this()
        {
            _beach = beach;
            BeachName = beach.Name;
            HasBeachSelected = true;
            // Load both weather and forecast data
            Task.WhenAll(
                LoadWeatherDataAsync(beach.Latitude, beach.Longitude),
                LoadForecastDataAsync(beach.Latitude, beach.Longitude)
            ).ConfigureAwait(false);
        }

        private async Task LoadWeatherDataAsync(double latitude, double longitude)
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"Attempting to load weather data for coordinates: Lat={latitude}, Long={longitude}");

                if (!IsValidCoordinate(latitude, longitude))
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid coordinates: Lat={latitude}, Long={longitude}");
                    throw new ArgumentException($"Invalid coordinates provided: Lat={latitude}, Long={longitude}");
                }

                var parameters = "waveHeight,waterTemperature,windSpeed,windDirection";
                var url = $"{BaseUrl}?lat={latitude}&lng={longitude}&params={parameters}";
                
                System.Diagnostics.Debug.WriteLine($"Making API request to: {url}");

                var response = await _httpClient.GetAsync(url);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response Content: {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("API Key is invalid or expired. Please check your API key configuration.");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        throw new Exception("API rate limit exceeded. Please try again later.");
                    }
                    
                    throw new Exception($"API Error: {response.StatusCode} - {jsonResponse}");
                }

                var apiResponse = JsonSerializer.Deserialize<StormglassResponse>(jsonResponse);
                
                if (apiResponse?.Hours == null || !apiResponse.Hours.Any())
                {
                    throw new Exception("No weather data available for these coordinates");
                }

                var currentData = apiResponse.Hours[0];
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Update UI with the weather data
                    WaveHeight = currentData.WaveHeight?.Noaa ?? currentData.WaveHeight?.Sg ?? 0;
                    SeaTemperature = currentData.WaterTemperature?.Noaa ?? currentData.WaterTemperature?.Sg ?? 0;
                    var windSpeed = currentData.WindSpeed?.Noaa ?? currentData.WindSpeed?.Sg ?? 0;
                    var windDir = currentData.WindDirection?.Noaa ?? currentData.WindDirection?.Sg ?? 0;
                    WindConditions = $"{windSpeed:F1} m/s from {GetWindDirection(windDir)}";
                    LastUpdated = DateTime.Parse(currentData.Time);
                    HasBeachSelected = true;
                });

                System.Diagnostics.Debug.WriteLine($"Successfully loaded weather data for {BeachName}");
                System.Diagnostics.Debug.WriteLine($"Wave Height: {WaveHeight}m");
                System.Diagnostics.Debug.WriteLine($"Sea Temperature: {SeaTemperature}°C");
                System.Diagnostics.Debug.WriteLine($"Wind Conditions: {WindConditions}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading weather data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                var errorMessage = ex.Message.Contains("API Key") 
                    ? "Invalid API key. Please check your API configuration."
                    : ex.Message.Contains("internet connection")
                        ? "No internet connection available. Please check your network settings."
                        : "Unable to load weather data. Please try again later.";
                    
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadForecastDataAsync(double latitude, double longitude)
        {
            try
            {
                IsLoadingForecast = true;
                var forecasts = await _marineWeatherService.GetMarineWeatherForecastAsync(latitude, longitude);
                Forecasts = forecasts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading forecast: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Forecast Error", 
                        "Unable to load weather forecast. Please try again later.", 
                        "OK");
                });
            }
            finally
            {
                IsLoadingForecast = false;
            }
        }

        private bool IsValidCoordinate(double latitude, double longitude)
        {
            // Latitude must be between -90 and 90
            // Longitude must be between -180 and 180
            return latitude >= -90 && latitude <= 90 && 
                   longitude >= -180 && longitude <= 180 &&
                   latitude != 0 && longitude != 0; // Exclude 0,0 as it's likely invalid data
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
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }

        [JsonPropertyName("requestCount")]
        public int RequestCount { get; set; }
    }
}

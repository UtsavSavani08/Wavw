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
        private const string API_KEY = "YOUR_API_KEY"; // TODO: Replace with your actual Stormglass API key
        private readonly BeachService _beachService;
        private const string BaseUrl = "https://api.stormglass.io/v2/weather/";
        private DateTime _lastApiCall = DateTime.MinValue;
        private const int MinSecondsBetweenCalls = 5; // Rate limiting
        
        private string _beachName;
        private double _waveHeight;
        private double _seaTemperature;
        private string _windConditions;
        private DateTime _lastUpdated;
        private Beach _beach;
        private bool _hasBeachSelected;
        private bool _isLoading;
        private string _errorMessage;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }

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

        public WeatherViewModel()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _beachService = new BeachService();
            HasBeachSelected = false;
            SearchCommand = new Command<string>(async (term) => await SearchBeach(term));
            RefreshCommand = new Command(async () => await RefreshWeatherData());
        }

        public WeatherViewModel(Beach beach) : this()
        {
            _beach = beach;
            BeachName = beach.Name;
            HasBeachSelected = true;
            LoadWeatherDataAsync(beach.Latitude, beach.Longitude).ConfigureAwait(false);
        }

        private async Task RefreshWeatherData()
        {
            if (_beach == null) return;
            await LoadWeatherDataAsync(_beach.Latitude, _beach.Longitude);
        }

        private async Task SearchBeach(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return;

            IsLoading = true;
            ErrorMessage = null;

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
                    ErrorMessage = $"No beach found matching '{searchTerm}'. Please check the spelling.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                ErrorMessage = "An error occurred while searching for the beach. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadWeatherDataAsync(double latitude, double longitude)
        {
            if (IsLoading) return;
            
            // Check rate limiting
            var timeSinceLastCall = DateTime.Now - _lastApiCall;
            if (timeSinceLastCall.TotalSeconds < MinSecondsBetweenCalls)
            {
                await Task.Delay((int)(MinSecondsBetweenCalls - timeSinceLastCall.TotalSeconds) * 1000);
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                // Validate coordinates
                if (latitude == 0 || longitude == 0)
                {
                    throw new ArgumentException("Invalid coordinates provided");
                }

                // Check if API key is configured
                if (string.IsNullOrWhiteSpace(API_KEY) || API_KEY == "YOUR_API_KEY")
                {
                    throw new Exception("Stormglass API key not configured");
                }

                var parameters = "waveHeight,waterTemperature,windSpeed,windDirection";
                var url = $"{BaseUrl}point?lat={latitude}&lng={longitude}&params={parameters}";
                
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", API_KEY);

                _lastApiCall = DateTime.Now;
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.Unauthorized:
                            throw new Exception("Invalid API key. Please check your API configuration.");
                        case System.Net.HttpStatusCode.TooManyRequests:
                            throw new Exception("API rate limit exceeded. Please try again later.");
                        default:
                            throw new Exception($"API Error ({response.StatusCode}): {errorContent}");
                    }
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<StormglassResponse>(jsonResponse);
                
                if (apiResponse?.Hours == null || !apiResponse.Hours.Any())
                {
                    throw new Exception("No weather data available for this location");
                }

                var currentData = apiResponse.Hours[0];
                
                // Update the UI values with fallback to SG data if NOAA is not available
                WaveHeight = currentData.WaveHeight?.Noaa ?? currentData.WaveHeight?.Sg ?? 0;
                SeaTemperature = currentData.WaterTemperature?.Noaa ?? currentData.WaterTemperature?.Sg ?? 0;
                var windSpeed = currentData.WindSpeed?.Noaa ?? currentData.WindSpeed?.Sg ?? 0;
                var windDir = currentData.WindDirection?.Noaa ?? currentData.WindDirection?.Sg ?? 0;
                
                WindConditions = $"{windSpeed:F1} m/s from {GetWindDirection(windDir)}";
                LastUpdated = DateTime.Parse(currentData.Time);
                HasBeachSelected = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading weather data: {ex.Message}");
                ErrorMessage = ex.Message;
                HasBeachSelected = false;
            }
            finally
            {
                IsLoading = false;
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
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Wavw.Model;
using System.Diagnostics;
namespace Wavw.Services
{
    public class MarineWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.worldweatheronline.com/premium/v1/marine.ashx";

        public MarineWeatherService()
        {
            try
            {
                // Read directly from the project's appsettings.json
                string projectRoot = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(projectRoot, "appsettings.json");
                
                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine(FileSystem.Current.AppDataDirectory, "appsettings.json");
                }

                string jsonContent = File.ReadAllText(configPath);
                using var jsonDoc = JsonDocument.Parse(jsonContent);
                _apiKey = jsonDoc.RootElement
                    .GetProperty("ApiKeys")
                    .GetProperty("WorldWeatherOnline")
                    .GetString();

                Debug.WriteLine($"API Key loaded: {!string.IsNullOrEmpty(_apiKey)}");
                Debug.WriteLine($"API Key value: {_apiKey}"); // Temporary debug line
                // Update HttpClient configuration
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Configuration error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<WeatherForecast>> GetMarineWeatherForecastAsync(double latitude, double longitude)
        {
            try
            {
                var url = $"https://api.worldweatheronline.com/premium/v1/marine.ashx?key={_apiKey}&format=json&q={latitude},{longitude}&days=7&tide=yes&tp=24&includeLocation=yes";
                Debug.WriteLine($"Making forecast request to: {url}");

                if (string.IsNullOrEmpty(_apiKey))
                {
                    throw new InvalidOperationException("WorldWeatherOnline API key is missing. Please check your configuration.");
                }

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Forecast Response: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = content.Contains("Parameter key is missing") 
                        ? "API key is missing or invalid" 
                        : $"API Error: {response.StatusCode}";
                    throw new HttpRequestException(errorMessage);
                }

                var forecastResponse = JsonSerializer.Deserialize<MarineWeatherResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (forecastResponse?.Data?.Error != null)
                {
                    throw new Exception(forecastResponse.Data.Error[0].Message);
                }

                return ProcessWeatherData(forecastResponse?.Data?.Weather);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in marine weather service: {ex.Message}");
                throw;
            }
        }

        private List<WeatherForecast> ProcessWeatherData(List<WeatherForecast> forecasts)
        {
            if (forecasts == null) return new List<WeatherForecast>();

            foreach (var forecast in forecasts)
            {
                if (forecast.Hourly?.FirstOrDefault() is var hourlyData && hourlyData != null)
                {
                    // Get current tide data
                    var currentTide = forecast.Tides?
                        .FirstOrDefault()?
                        .TideDetails?
                        .FirstOrDefault();

                    forecast.TideHeight = currentTide?.TideHeight ?? 0;
                    forecast.TideType = currentTide?.TideType ?? string.Empty;
                    forecast.WindDirection = hourlyData.WindDirection;
                    forecast.WeatherCode = int.TryParse(hourlyData.WeatherCode, out int code) ? code : 0;
                    forecast.WeatherIconUrl = hourlyData.WeatherIconUrl?.FirstOrDefault()?.Value ?? string.Empty;
                    forecast.WeatherDesc = hourlyData.WeatherDesc?.FirstOrDefault()?.Value;
                }
            }

            return forecasts;
        }
    }
}
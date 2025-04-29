using System.Text.Json.Serialization;

namespace Wavw.Model
{
    public class WeatherForecast
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("maxtempC")]
        public string MaxTemp { get; set; }

        [JsonPropertyName("mintempC")]
        public string MinTemp { get; set; }

        [JsonPropertyName("hourly")]
        public List<HourlyForecast> Hourly { get; set; }

        [JsonPropertyName("tides")]
        public List<TideData> Tides { get; set; }
    
        // Properties for processed data
        public double TideHeight { get; set; }
        public string WindDirection { get; set; }
        public string TideType { get; set; }
        public int WeatherCode { get; set; }
        public string WeatherIconUrl { get; set; }
        public string WeatherDesc { get; set; }
    }

    public class HourlyForecast
    {
        [JsonPropertyName("weatherDesc")]
        public List<WeatherDescription> WeatherDesc { get; set; }

        [JsonPropertyName("windspeedKmph")]
        public string WindSpeed { get; set; }

        [JsonPropertyName("winddirDegree")]
        public string WindDirection { get; set; }

        [JsonPropertyName("tideHeight_mt")]
        public double TideHeight { get; set; }

        [JsonPropertyName("tideDateTime")]
        public string TideDateTime { get; set; }

        [JsonPropertyName("tide_type")]
        public string TideType { get; set; }

        [JsonPropertyName("weatherCode")]
        public string WeatherCode { get; set; }

        [JsonPropertyName("weatherIconUrl")]
        public List<WeatherIconUrl> WeatherIconUrl { get; set; }
    }

    // Add this new class
    public class WeatherIconUrl
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class WeatherDescription
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class MarineWeatherResponse
    {
        [JsonPropertyName("data")]
        public MarineWeatherData Data { get; set; }
    }

    public class MarineWeatherData
    {
        [JsonPropertyName("weather")]
        public List<WeatherForecast> Weather { get; set; }

        [JsonPropertyName("error")]
        public List<WeatherApiError> Error { get; set; }
    }

    public class WeatherApiError
    {
        [JsonPropertyName("msg")]
        public string Message { get; set; }
    }



    public class TideData
    {
        [JsonPropertyName("tide_data")]
        public List<TideDetail> TideDetails { get; set; }
    }

    public class TideDetail
    {
        private string _tideHeightStr;
        private double _tideHeight;
    
        [JsonPropertyName("tideHeight_mt")]
        public string TideHeightStr
        {
            get => _tideHeightStr;
            set
            {
                _tideHeightStr = value;
                _ = double.TryParse(value, out _tideHeight);
            }
        }
    
        public double TideHeight => _tideHeight;
    
        [JsonPropertyName("tide_type")]
        public string TideType { get; set; }
    
        [JsonPropertyName("tideTime")]
        public string TideTime { get; set; }
    }
}
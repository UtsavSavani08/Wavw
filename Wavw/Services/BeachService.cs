using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Wavw.Model;

namespace Wavw.Services
{
    public class BeachService
    {
        private readonly HttpClient _httpClient;
        private const string OverpassApiUrl = "https://overpass-api.de/api/interpreter";

        public BeachService()
        {
            _httpClient = new HttpClient();
        }

        // Fetch beach by name
        public async Task<List<Beach>> GetBeachByNameAsync(string beachName)
        {
            string query = $@"
        [out:json];
        node[name=""{beachName}""][natural=beach];
        out;";

            string url = $"{OverpassApiUrl}?data={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetStringAsync(url);
            return ParseBeachData(response);
        }

        // Fetch nearest beaches based on latitude and longitude
        public async Task<List<Beach>> GetBeachesNearbyAsync(double lat, double lon)
        {
            string query = $@"
        [out:json];
        node(around:20000,{lat},{lon})[natural=beach];
        out;";

            string url = $"{OverpassApiUrl}?data={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetStringAsync(url);
            return ParseBeachData(response);
        }

        // Parse API response
        private List<Beach> ParseBeachData(string jsonResponse)
        {
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;
            var beaches = new List<Beach>();

            if (root.TryGetProperty("elements", out JsonElement elements))
            {
                foreach (var element in elements.EnumerateArray())
                {
                    if (element.TryGetProperty("lat", out JsonElement latEl) &&
                        element.TryGetProperty("lon", out JsonElement lonEl))
                    {
                        string name = element.TryGetProperty("tags", out JsonElement tags) &&
                                      tags.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : "Unknown Beach";

                        beaches.Add(new Beach
                        {
                            Name = name,
                            Latitude = latEl.GetDouble(),
                            Longitude = lonEl.GetDouble(),
                            Rating = "Not Available",  // Placeholder, no rating data from OSM
                            Cleanliness = "Unknown",
                            MainAttractions = "Unknown"
                        });
                    }
                }
            }
            return beaches;
        }
    }
}

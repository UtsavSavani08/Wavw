using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Wavw.Model;
using System.Collections.ObjectModel;
using Microsoft.Maui.Devices.Sensors;
using System.Linq;
using System.Threading;

namespace Wavw.Services
{
    public class BeachService
    {
        private readonly HttpClient _httpClient;
        private const string OverpassApiUrl = "https://overpass-api.de/api/interpreter";
        private readonly List<Beach> _beaches;
        private readonly Dictionary<string, Beach> _beachNameCache;

        public BeachService()
        {
            _httpClient = new HttpClient();
            _beaches = LoadBeachesFromJson();
            // Create a case-insensitive cache for faster lookups
            _beachNameCache = new Dictionary<string, Beach>(StringComparer.OrdinalIgnoreCase);
            foreach (var beach in _beaches)
            {
                // Store the original name but cache without "beach"
                var nameWithoutBeach = beach.Name.Replace(" Beach", "", StringComparison.OrdinalIgnoreCase)
                                                .Replace(" beach", "", StringComparison.OrdinalIgnoreCase)
                                                .Trim();
                if (!_beachNameCache.ContainsKey(nameWithoutBeach))
                {
                    _beachNameCache[nameWithoutBeach] = beach;
                }
            }
        }

        private string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Remove the word "beach" and clean up the name
            return name.Replace(" Beach", "", StringComparison.OrdinalIgnoreCase)
                      .Replace("Beach ", "", StringComparison.OrdinalIgnoreCase)
                      .Replace(" beach", "", StringComparison.OrdinalIgnoreCase)
                      .Replace("beach ", "", StringComparison.OrdinalIgnoreCase)
                      .Replace("  ", " ")
                      .Trim()
                      .ToLowerInvariant();
        }

        private List<Beach> LoadBeachesFromJson()
        {
            try
            {
                var jsonFileName = "Resources/beaches.json";
                var jsonString = File.ReadAllText(jsonFileName);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var beachData = JsonSerializer.Deserialize<BeachData>(jsonString, options);
                return beachData?.Beaches ?? new List<Beach>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading beaches from JSON: {ex.Message}");
                return new List<Beach>();
            }
        }

        public async Task<Beach?> SearchBeachByName(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return null;

            try
            {
                // Check if user included "beach" in the search
                if (searchTerm.Contains("beach", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Please enter only the beach name without the word 'beach'.");
                }

                // Normalize the search term
                searchTerm = NormalizeName(searchTerm);

                // Try local search with exact and partial matches
                var localMatch = SearchLocalBeaches(searchTerm);
                if (localMatch != null)
                {
                    return localMatch;
                }

                // If no local match found, try API search
                var apiMatch = await SearchBeachInApi(searchTerm);
                if (apiMatch != null)
                {
                    return apiMatch;
                }

                return null;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw the specific error about "beach" in search
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching beach: {ex.Message}");
                throw;
            }
        }

        private Beach? SearchLocalBeaches(string searchTerm)
        {
            // Try exact match first (case-insensitive)
            if (_beachNameCache.TryGetValue(searchTerm, out Beach? exactMatch))
            {
                return exactMatch;
            }

            // Try partial matches with normalized names
            var normalizedSearchTerm = NormalizeName(searchTerm);
            var partialMatch = _beaches.FirstOrDefault(b => 
                NormalizeName(b.Name).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                normalizedSearchTerm.Contains(NormalizeName(b.Name), StringComparison.OrdinalIgnoreCase));

            if (partialMatch != null)
            {
                return partialMatch;
            }

            return null;
        }

        private async Task<Beach?> SearchBeachInApi(string searchTerm)
        {
            try
            {
                // Create a more flexible search query
                string query = $@"
                    [out:json];
                    area[name=""India""]->.india;
                    (
                        node(area.india)[""natural""=""beach""][name~""{searchTerm}"",i];
                        node(area.india)[""natural""=""beach""][name~"".*{searchTerm}.*"",i];
                        node(area.india)[""natural""=""beach""][name~"".*{searchTerm.Replace(" ", ".*")}.*"",i];
                        node(area.india)[""natural""=""beach""][name~""{searchTerm} beach"",i];
                        node(area.india)[""natural""=""beach""][name~""beach {searchTerm}"",i];
                    );
                    out;";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                string url = $"{OverpassApiUrl}?data={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetStringAsync(url, cts.Token);
                var apiBeaches = ParseBeachData(response);

                var bestMatch = apiBeaches.FirstOrDefault();
                if (bestMatch != null)
                {
                    // Add to local cache if not exists
                    var normalizedName = NormalizeName(bestMatch.Name);
                    if (!_beachNameCache.ContainsKey(normalizedName))
                    {
                        _beaches.Add(bestMatch);
                        _beachNameCache[normalizedName] = bestMatch;
                    }
                    return bestMatch;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API search error: {ex.Message}");
            }
            return null;
        }

        public async Task<List<Beach>> GetNearestBeaches(Location userLocation, int count = 3)
        {
            try
            {
                // First get beaches from local cache within reasonable distance (50km)
                var localBeaches = _beaches
                    .Where(b => b.DistanceFromUser(userLocation) <= 50)
                    .OrderBy(b => b.DistanceFromUser(userLocation))
                    .Take(count)
                    .ToList();

                if (localBeaches.Count >= count)
                {
                    return localBeaches;
                }

                // If not enough local beaches found, try API
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                string query = $@"
                    [out:json];
                    area[name=""India""]->.india;
                    node(area.india)(around:50000,{userLocation.Latitude},{userLocation.Longitude})[natural=beach];
                    out;";

                string url = $"{OverpassApiUrl}?data={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetStringAsync(url, cts.Token);
                var apiBeaches = ParseBeachData(response);

                // Combine and deduplicate beaches
                var allBeaches = localBeaches.Union(apiBeaches)
                    .OrderBy(b => b.DistanceFromUser(userLocation))
                    .Take(count)
                    .ToList();

                // Cache new beaches
                foreach (var beach in apiBeaches)
                {
                    if (!_beaches.Any(b => b.Name.Equals(beach.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _beaches.Add(beach);
                        var normalizedName = NormalizeName(beach.Name);
                        if (!_beachNameCache.ContainsKey(normalizedName))
                        {
                            _beachNameCache[normalizedName] = beach;
                        }
                    }
                }

                return allBeaches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting nearest beaches: {ex.Message}");
                // On error, just use local beaches
                return _beaches
                    .OrderBy(b => b.DistanceFromUser(userLocation))
                    .Take(count)
                    .ToList();
            }
        }

        private List<Beach> ParseBeachData(string jsonResponse)
        {
            try
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
                                Rating = "3.5/5",  // Default rating
                                Cleanliness = "Good",  // Default cleanliness
                                BestSeason = "October to March",  // Default season
                                MainAttractions = "Beach Activities, Swimming"  // Default attractions
                            });
                        }
                    }
                }
                return beaches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing beach data: {ex.Message}");
                return new List<Beach>();
            }
        }

        public List<Beach> GetAllBeaches()
        {
            return _beaches.ToList();
        }
    }

    // Class to help with JSON deserialization
    public class BeachData
    {
        public List<Beach> Beaches { get; set; } = new List<Beach>();
    }
}

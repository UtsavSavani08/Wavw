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
using System.IO;
using Microsoft.Maui.Storage;

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
            _beachNameCache = new Dictionary<string, Beach>(StringComparer.OrdinalIgnoreCase);
            
            // Debug the loaded beaches
            System.Diagnostics.Debug.WriteLine($"Loaded {_beaches.Count} beaches initially");
            foreach (var beach in _beaches)
            {
                System.Diagnostics.Debug.WriteLine($"Beach in list: {beach.Name}");
                _beachNameCache[beach.Name.ToLowerInvariant()] = beach;
            }
            System.Diagnostics.Debug.WriteLine($"Cache contains {_beachNameCache.Count} beaches");
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
                string jsonString = null;
                
                // First try to read from the app package
                try
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync("beaches.json").Result;
                    using var reader = new StreamReader(stream);
                    jsonString = reader.ReadToEnd();
                    System.Diagnostics.Debug.WriteLine("Successfully read beaches.json from app package");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not read from app package: {ex.Message}");
                    
                    // Fall back to file system if app package fails
                    var possiblePaths = new[]
                    {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "beaches.json"),
                        Path.Combine(FileSystem.AppDataDirectory, "Resources", "beaches.json"),
                        Path.Combine(FileSystem.AppDataDirectory, "beaches.json"),
                        "Resources/beaches.json",
                        "beaches.json"
                    };

                    foreach (var path in possiblePaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"Checking path: {path}");
                        if (File.Exists(path))
                        {
                            jsonString = File.ReadAllText(path);
                            System.Diagnostics.Debug.WriteLine($"Found and read beaches.json at: {path}");
                            break;
                        }
                    }
                }

                // If we still don't have the JSON content, use default beaches
                if (string.IsNullOrEmpty(jsonString))
                {
                    System.Diagnostics.Debug.WriteLine("No beaches.json found, using default list");
                    return new List<Beach>
                    {
                        new Beach { 
                            Name = "juhu", 
                            State = "Maharashtra",
                            City = "Mumbai",
                            Latitude = 19.0883, 
                            Longitude = 72.8263, 
                            Rating = "4.2/5", 
                            Cleanliness = "Good", 
                            BestSeason = "October to March", 
                            MainAttractions = "Celebrity Spotting, Famous Street Food" 
                        },
                        new Beach { 
                            Name = "marina", 
                            State = "Tamil Nadu",
                            City = "Chennai",
                            Latitude = 13.0500, 
                            Longitude = 80.2824, 
                            Rating = "4.3/5", 
                            Cleanliness = "Good", 
                            BestSeason = "December to February", 
                            MainAttractions = "World's Second Longest Urban Beach" 
                        },
                        new Beach { 
                            Name = "puri", 
                            State = "Odisha",
                            City = "Puri",
                            Latitude = 19.7987, 
                            Longitude = 85.8249, 
                            Rating = "4.4/5", 
                            Cleanliness = "Good", 
                            BestSeason = "October to February", 
                            MainAttractions = "Sacred Beach, Famous Sand Art" 
                        }
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Read JSON content, length: {jsonString.Length}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var beachData = JsonSerializer.Deserialize<BeachData>(jsonString, options);
                if (beachData?.Beaches == null || !beachData.Beaches.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No beaches found in JSON file, using default list");
                    return new List<Beach>
                    {
                        new Beach { 
                            Name = "juhu", 
                            State = "Maharashtra",
                            City = "Mumbai",
                            Latitude = 19.0883, 
                            Longitude = 72.8263, 
                            Rating = "4.2/5", 
                            Cleanliness = "Good", 
                            BestSeason = "October to March", 
                            MainAttractions = "Celebrity Spotting, Famous Street Food" 
                        },
                        new Beach { 
                            Name = "marina", 
                            State = "Tamil Nadu",
                            City = "Chennai",
                            Latitude = 13.0500, 
                            Longitude = 80.2824, 
                            Rating = "4.3/5", 
                            Cleanliness = "Good", 
                            BestSeason = "December to February", 
                            MainAttractions = "World's Second Longest Urban Beach" 
                        },
                        new Beach { 
                            Name = "puri", 
                            State = "Odisha",
                            City = "Puri",
                            Latitude = 19.7987, 
                            Longitude = 85.8249, 
                            Rating = "4.4/5", 
                            Cleanliness = "Good", 
                            BestSeason = "October to February", 
                            MainAttractions = "Sacred Beach, Famous Sand Art" 
                        }
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {beachData.Beaches.Count} beaches from JSON");
                foreach (var beach in beachData.Beaches)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded beach: {beach.Name}");
                }
                return beachData.Beaches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading beaches from JSON: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<Beach>
                {
                    new Beach { 
                        Name = "juhu", 
                        State = "Maharashtra",
                        City = "Mumbai",
                        Latitude = 19.0883, 
                        Longitude = 72.8263, 
                        Rating = "4.2/5", 
                        Cleanliness = "Good", 
                        BestSeason = "October to March", 
                        MainAttractions = "Celebrity Spotting, Famous Street Food" 
                    },
                    new Beach { 
                        Name = "marina", 
                        State = "Tamil Nadu",
                        City = "Chennai",
                        Latitude = 13.0500, 
                        Longitude = 80.2824, 
                        Rating = "4.3/5", 
                        Cleanliness = "Good", 
                        BestSeason = "December to February", 
                        MainAttractions = "World's Second Longest Urban Beach" 
                    },
                    new Beach { 
                        Name = "puri", 
                        State = "Odisha",
                        City = "Puri",
                        Latitude = 19.7987, 
                        Longitude = 85.8249, 
                        Rating = "4.4/5", 
                        Cleanliness = "Good", 
                        BestSeason = "October to February", 
                        MainAttractions = "Sacred Beach, Famous Sand Art" 
                    }
                };
            }
        }

        public async Task<Beach?> SearchBeachByName(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return null;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting search for beach: {searchTerm}");
                System.Diagnostics.Debug.WriteLine($"Total beaches available: {_beaches.Count}");
                
                // Normalize the search term
                var normalizedSearchTerm = NormalizeName(searchTerm);
                
                System.Diagnostics.Debug.WriteLine($"Normalized search term: {normalizedSearchTerm}");
                System.Diagnostics.Debug.WriteLine("Available beaches:");
                foreach (var beach in _beaches)
                {
                    var normalizedBeachName = NormalizeName(beach.Name);
                    System.Diagnostics.Debug.WriteLine($"- {beach.Name} (normalized: {normalizedBeachName})");
                }

                // First try exact match with normalized names
                var exactMatch = _beaches.FirstOrDefault(b => 
                    NormalizeName(b.Name).Equals(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase));
                
                if (exactMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found exact match: {exactMatch.Name}");
                    return exactMatch;
                }

                // Then try contains match with normalized names
                var containsMatch = _beaches.FirstOrDefault(b => 
                    NormalizeName(b.Name).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    normalizedSearchTerm.Contains(NormalizeName(b.Name), StringComparison.OrdinalIgnoreCase));
                
                if (containsMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found contains match: {containsMatch.Name}");
                    return containsMatch;
                }

                // Try fuzzy matching by splitting words
                var searchWords = normalizedSearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var fuzzyMatch = _beaches.FirstOrDefault(b => {
                    var beachWords = NormalizeName(b.Name).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return searchWords.Any(sw => beachWords.Any(bw => 
                        bw.Contains(sw, StringComparison.OrdinalIgnoreCase) || 
                        sw.Contains(bw, StringComparison.OrdinalIgnoreCase)));
                });

                if (fuzzyMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found fuzzy match: {fuzzyMatch.Name}");
                    return fuzzyMatch;
                }

                // If still no match found locally, try API search
                System.Diagnostics.Debug.WriteLine("No local matches found, trying API search");
                var apiMatch = await SearchBeachInApi(normalizedSearchTerm);
                
                if (apiMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found API match: {apiMatch.Name}");
                    return apiMatch;
                }

                System.Diagnostics.Debug.WriteLine("No matches found in any search method");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in beach search: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
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
                // Get all beaches ordered by distance, without any distance restriction
                var localBeaches = _beaches
                    .OrderBy(b => b.DistanceFromUser(userLocation))
                    .Take(count)
                    .ToList();

                if (localBeaches.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found {localBeaches.Count} nearest beaches:");
                    foreach (var beach in localBeaches)
                    {
                        System.Diagnostics.Debug.WriteLine($"- {beach.Name} at distance: {beach.DistanceFromUser(userLocation):F2} km");
                    }
                    return localBeaches;
                }

                // If no beaches in local cache, try API
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            string query = $@"
        [out:json];
            area[name=""India""]->.india;
            node(area.india)[natural=beach];
        out;";

            string url = $"{OverpassApiUrl}?data={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetStringAsync(url, cts.Token);
                var apiBeaches = ParseBeachData(response);

                // Order API beaches by distance and take the nearest ones
                var allBeaches = apiBeaches
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
                // On error, just use local beaches without distance restriction
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

    public class BeachData
    {
        public List<Beach> Beaches { get; set; } = new List<Beach>();
    }
}

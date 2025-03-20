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
                System.Diagnostics.Debug.WriteLine("Starting to load beaches from JSON...");
                
                // List all embedded resources
                var assembly = GetType().Assembly;
                System.Diagnostics.Debug.WriteLine("Available embedded resources:");
                foreach (var resourceName in assembly.GetManifestResourceNames())
                {
                    System.Diagnostics.Debug.WriteLine($"Resource: {resourceName}");
                }

                // First try to load from embedded resource
                var embeddedResourceName = "Wavw.Resources.Raw.beaches.json";
                System.Diagnostics.Debug.WriteLine($"Trying to load embedded resource: {embeddedResourceName}");
                using (var stream = assembly.GetManifestResourceStream(embeddedResourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var jsonString = reader.ReadToEnd();
                            System.Diagnostics.Debug.WriteLine($"Found beaches.json in embedded resources, content length: {jsonString.Length}");
                            var beaches = ParseBeachJson(jsonString);
                            if (beaches.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"Successfully loaded {beaches.Count} beaches from embedded resource");
                                return beaches;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Embedded resource stream was null");
                    }
                }

                // If not found in embedded resources, try file system
                var possiblePaths = new[]
                {
                    "Resources/Raw/beaches.json",
                    Path.Combine(FileSystem.AppDataDirectory, "Resources", "Raw", "beaches.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "beaches.json"),
                    Path.Combine(FileSystem.CacheDirectory, "Resources", "Raw", "beaches.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "beaches.json"),
                    Path.Combine(Environment.CurrentDirectory, "Resources", "Raw", "beaches.json"),
                    Path.Combine(Environment.CurrentDirectory, "beaches.json")
                };

                foreach (var path in possiblePaths)
                {
                    System.Diagnostics.Debug.WriteLine($"Checking path: {path}");
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found file at: {path}");
                        var jsonString = File.ReadAllText(path);
                        System.Diagnostics.Debug.WriteLine($"File content length: {jsonString.Length}");
                        System.Diagnostics.Debug.WriteLine($"First 100 characters of content: {(jsonString.Length > 100 ? jsonString.Substring(0, 100) : jsonString)}...");
                        
                        var beaches = ParseBeachJson(jsonString);
                        if (beaches.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"Successfully loaded {beaches.Count} beaches from file");
                            return beaches;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("File was found but no beaches were parsed from it");
                        }
                    }
                }

                // If we get here, we couldn't find the file
                System.Diagnostics.Debug.WriteLine("\nListing all files in current directory and subdirectories:");
                var currentDir = Directory.GetCurrentDirectory();
                System.Diagnostics.Debug.WriteLine($"Current directory: {currentDir}");
                var allFiles = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    System.Diagnostics.Debug.WriteLine($"Found file: {file}");
                }

                // Return default beaches for testing
                System.Diagnostics.Debug.WriteLine("\nReturning default test beaches since no file was found");
                return new List<Beach>
                {
                    new Beach 
                    { 
                        Name = "Mandvi Beach", 
                        Latitude = 22.8373, 
                        Longitude = 69.3564,
                        Rating = "4/5",
                        Cleanliness = "Good",
                        BestSeason = "October to March",
                        MainAttractions = "Windmills, camping, water sports"
                    },
                    new Beach 
                    { 
                        Name = "Dwarka Beach", 
                        Latitude = 22.2442, 
                        Longitude = 68.9685,
                        Rating = "4.5/5",
                        Cleanliness = "Very Good",
                        BestSeason = "October to March",
                        MainAttractions = "Temple view, sunset point"
                    },
                    new Beach 
                    { 
                        Name = "Somnath Beach", 
                        Latitude = 20.8880, 
                        Longitude = 70.4006,
                        Rating = "4/5",
                        Cleanliness = "Good",
                        BestSeason = "October to February",
                        MainAttractions = "Temple vicinity, evening aarti view"
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading beaches from JSON: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<Beach>();
            }
        }

        private List<Beach> ParseBeachJson(string jsonString)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                // Try parsing as BeachData first
                try
                {
                    var beachData = JsonSerializer.Deserialize<BeachData>(jsonString, options);
                    if (beachData?.Beaches != null && beachData.Beaches.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully parsed {beachData.Beaches.Count} beaches from BeachData format");
                        return beachData.Beaches;
                    }
                }
                catch (JsonException)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to parse as BeachData, trying direct Beach array");
                }

                // Try parsing as direct array of beaches
                try
                {
                    var beaches = JsonSerializer.Deserialize<List<Beach>>(jsonString, options);
                    if (beaches != null && beaches.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully parsed {beaches.Count} beaches from direct array format");
                        return beaches;
                    }
                }
                catch (JsonException)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to parse as direct Beach array");
                }

                System.Diagnostics.Debug.WriteLine("No valid beach data found in JSON");
                return new List<Beach>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing beach JSON: {ex.Message}");
                return new List<Beach>();
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
                
                // Convert search term to lowercase and trim
                var searchTermLower = searchTerm.ToLowerInvariant().Trim();
                
                System.Diagnostics.Debug.WriteLine("Available beaches:");
                foreach (var beach in _beaches)
                {
                    System.Diagnostics.Debug.WriteLine($"- {beach.Name} (lowercase: {beach.Name.ToLowerInvariant()})");
                }

                // First try exact match
                var exactMatch = _beaches.FirstOrDefault(b => 
                    b.Name.Equals(searchTermLower, StringComparison.OrdinalIgnoreCase));
                
                if (exactMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found exact match: {exactMatch.Name}");
                    return exactMatch;
                }

                // Then try contains match
                var containsMatch = _beaches.FirstOrDefault(b => 
                    b.Name.Contains(searchTermLower, StringComparison.OrdinalIgnoreCase));
                
                if (containsMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found contains match: {containsMatch.Name}");
                    return containsMatch;
                }

                // Try more flexible matching
                var flexibleMatch = _beaches.FirstOrDefault(b => 
                    searchTermLower.Contains(b.Name.ToLowerInvariant()) || 
                    b.Name.ToLowerInvariant().Contains(searchTermLower));
                
                if (flexibleMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found flexible match: {flexibleMatch.Name}");
                    return flexibleMatch;
                }

                // If still no match found locally, try API search
                System.Diagnostics.Debug.WriteLine("No local matches found, trying API search");
                var apiMatch = await SearchBeachInApi(searchTermLower);
                
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
                System.Diagnostics.Debug.WriteLine($"Getting nearest {count} beaches to location: {userLocation.Latitude}, {userLocation.Longitude}");
                System.Diagnostics.Debug.WriteLine($"Total beaches in database: {_beaches.Count}");
                
                if (_beaches.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No beaches loaded in the database!");
                    return new List<Beach>();
                }

                // Calculate distances for all beaches without any filtering
                var orderedBeaches = _beaches
                    .Select(beach => {
                        var distance = beach.DistanceFromUser(userLocation);
                        System.Diagnostics.Debug.WriteLine($"Distance from {beach.Name}: {distance:F2} km");
                        return (Beach: beach, Distance: distance);
                    })
                    .OrderBy(x => x.Distance)
                    .ToList();

                System.Diagnostics.Debug.WriteLine("All beaches sorted by distance:");
                foreach (var (beach, distance) in orderedBeaches)
                {
                    System.Diagnostics.Debug.WriteLine($"- {beach.Name}: {distance:F2} km");
                }

                // Take the nearest 3 beaches regardless of distance
                var nearestBeaches = orderedBeaches
                    .Take(count)
                    .Select(x => x.Beach)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Selected {nearestBeaches.Count} nearest beaches:");
                foreach (var beach in nearestBeaches)
                {
                    var distance = beach.DistanceFromUser(userLocation);
                    System.Diagnostics.Debug.WriteLine($"- {beach.Name} at distance: {distance:F2} km");
                }

                return nearestBeaches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting nearest beaches: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<Beach>();
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

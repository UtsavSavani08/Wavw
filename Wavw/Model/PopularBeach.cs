using System;
using System.Text.Json.Serialization;

namespace Wavw.Model;

public class PopularBeach
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The state where the beach is located
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The city where the beach is located
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// The latitude coordinate of the beach
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude coordinate of the beach
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    private string _rating;
    /// <summary>
    /// The rating of the beach (e.g., "4.5/5")
    /// </summary>
    [JsonPropertyName("rating")]
    public string Rating
    {
        get => _rating;
        set
        {
            // Handle rating format like "4.7/5"
            _rating = value?.Split('/')[0] ?? value;
        }
    }

    /// <summary>
    /// The cleanliness level of the beach
    /// </summary>
    [JsonPropertyName("cleanliness")]
    public string Cleanliness { get; set; } = "Good";

    /// <summary>
    /// The best season to visit the beach
    /// </summary>
    [JsonPropertyName("bestSeason")]
    public string BestSeason { get; set; } = "October to March";

    /// <summary>
    /// The main attractions and highlights of the beach
    /// </summary>
    [JsonPropertyName("mainAttractions")]
    public string MainAttractions { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the beach's image
    /// </summary>
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    public override string ToString()
    {
        return Name;
    }

}
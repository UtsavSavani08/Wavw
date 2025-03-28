using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using System.Text.Json.Serialization;

namespace Wavw.Model
{
    /// <summary>
    /// Represents a beach in India with its details and location
    /// </summary>
    public class Beach
    {
        /// <summary>
        /// The name of the beach
        /// </summary>
        [JsonPropertyName("name")]
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

        /// <summary>
        /// The rating of the beach (e.g., "4.5/5")
        /// </summary>
        [JsonPropertyName("rating")]
        public string Rating { get; set; } = "3.5/5";

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

        /// <summary>
        /// Calculates the distance between the beach and a user's location
        /// </summary>
        /// <param name="userLocation">The user's current location</param>
        /// <returns>Distance in kilometers</returns>
        public double DistanceFromUser(Location userLocation)
        {
            var beachLocation = new Location(Latitude, Longitude);
            return Location.CalculateDistance(userLocation, beachLocation, DistanceUnits.Kilometers);
        }

        /// <summary>
        /// Returns a string representation of the beach
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}

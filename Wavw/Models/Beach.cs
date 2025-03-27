using System.Text.Json.Serialization;

public class Beach
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Rating { get; set; } = "3.5/5";
    public string Cleanliness { get; set; } = "Good";
    public string BestSeason { get; set; } = "October to March";
    public string MainAttractions { get; set; } = "Beach Activities, Swimming";

    public double DistanceFromUser(Location userLocation)
    {
        return CalculateDistance(
            userLocation.Latitude, userLocation.Longitude,
            Latitude, Longitude
        );
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var d1 = lat1 * (Math.PI / 180);
        var num1 = lon1 * (Math.PI / 180);
        var d2 = lat2 * (Math.PI / 180);
        var num2 = lon2 * (Math.PI / 180) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2), 2) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2), 2);
        return 6371 * (2 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1 - d3)));
    }
} 
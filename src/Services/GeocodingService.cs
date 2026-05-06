using System.Text.Json;

namespace Dashboard.Services
{
    public class GeocodingService
    {
        private readonly HttpClient _http;

        public GeocodingService(HttpClient http)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent", "PickPrepPlate-App");
        }

        public async Task<(double Lat, double Lon)?> GetCoordinatesAsync(string address, string city, string state, string zip)
        {
            try
            {
                string fullAddress = $"{address}, {city}, {state} {zip}";
                string url = $"https://geocoding.geo.census.gov/geocoder/locations/onelineaddress?address={Uri.EscapeDataString(fullAddress)}&benchmark=Public_AR_Current&format=json";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var matches = doc.RootElement.GetProperty("result").GetProperty("addressMatches");

                if (matches.GetArrayLength() > 0)
                {
                    var coords = matches[0].GetProperty("coordinates");
                    return (coords.GetProperty("y").GetDouble(), coords.GetProperty("x").GetDouble());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Geocoding Error: {ex.Message}");
            }
            return null;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double r = 3958.8; // Earth radius in miles
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c;
        }

        private double ToRadians(double deg) => deg * (Math.PI / 180.0);
    }
}
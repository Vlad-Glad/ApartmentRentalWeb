using ApartmentRental.Models.DTO;
using System.Net.Http;
using System.Text.Json;

namespace ApartmentRental.Services
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;

        public NominatimGeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ApartmentRentalApp/1.0 (test@user.com)");
        }

        public async Task<List<AddressSuggestionDto>> SearchAsync(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<AddressSuggestionDto>();

            var url = $"https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&limit={limit}&q={Uri.EscapeDataString(query)}&accept-language=uk";

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<AddressSuggestionDto>();

            await using var stream = await response.Content.ReadAsStreamAsync();

            var json = await JsonDocument.ParseAsync(stream);
            var result = new List<AddressSuggestionDto>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var displayName = item.GetProperty("display_name").GetString() ?? "";
                var latStr = item.GetProperty("lat").GetString() ?? "0";
                var lonStr = item.GetProperty("lon").GetString() ?? "0";

                double.TryParse(latStr.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat);
                double.TryParse(lonStr.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon);

                var address = item.TryGetProperty("address", out var addrEl) ? addrEl : default;
                string city = "";

                if (address.ValueKind != JsonValueKind.Undefined)
                {
                    if (address.TryGetProperty("city", out var cityEl)) city = cityEl.GetString() ?? "";
                    else if (address.TryGetProperty("town", out var townEl)) city = townEl.GetString() ?? "";
                    else if (address.TryGetProperty("village", out var villageEl)) city = villageEl.GetString() ?? "";
                    else if (address.TryGetProperty("state", out var stateEl)) city = stateEl.GetString() ?? "";
                }

                result.Add(new AddressSuggestionDto
                {
                    Label = displayName,
                    City = city,
                    Latitude = lat,
                    Longitude = lon
                });
            }

            return result;
        }
    }
}

using System.Text.Json.Serialization;

namespace ApartmentRental.Models.PrivatBank
{
    public sealed class PrivatRateDto
    {
        [JsonPropertyName("ccy")]
        public string? Currency { get; set; }

        [JsonPropertyName("base_ccy")]
        public string? BaseCurrency { get; set; }

        [JsonPropertyName("buy")]
        public string? Buy { get; set; }

        [JsonPropertyName("sale")]
        public string? Sale { get; set; }
    }
}

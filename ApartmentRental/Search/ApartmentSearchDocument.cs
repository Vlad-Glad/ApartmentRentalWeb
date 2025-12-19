using System.Text.Json.Serialization;

namespace ApartmentRental.Search;

public sealed class ApartmentSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("apartmentId")]
    public int ApartmentId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("lessorEmail")]
    public string LessorEmail { get; set; } = "";
}

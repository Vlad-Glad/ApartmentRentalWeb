namespace ApartmentRental.Models.DTO;

public sealed class ApartmentStatsDto
{
    public int TotalApartments { get; set; }
    public List<CityStatDto> ByCity { get; set; } = new();
}

public sealed class CityStatDto
{
    public string City { get; set; } = "";
    public int Count { get; set; }
    public decimal AvgPrice { get; set; }
}

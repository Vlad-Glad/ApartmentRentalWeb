namespace ApartmentRental.Models.DTO
{
    public class AddressSuggestionDto
    {
        public string Label { get; set; } = default!; 
        public string City { get; set; } = default!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

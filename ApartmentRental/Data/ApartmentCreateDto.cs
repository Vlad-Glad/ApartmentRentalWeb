using System.ComponentModel.DataAnnotations;

namespace ApartmentRental.Data
{
    public class ApartmentCreateDto
    {
        [Required]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string FullAddress { get; set; } = default!;

        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

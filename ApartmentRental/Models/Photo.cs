using System.ComponentModel.DataAnnotations;

namespace ApartmentRental.Models
{
    public class Photo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Url(ErrorMessage = "Invalid image URL format.")]
        public required string ImageUrl { get; set; }

        public bool IsCover { get; set; }

        public int ApartmentId { get; set; }

        public virtual Apartment? Apartment { get; set; }
    }
}

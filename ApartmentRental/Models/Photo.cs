using System.ComponentModel.DataAnnotations;

namespace ApartmentRental.Models
{
    public class Photo
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public bool IsCover { get; set; }

        public int ApartmentId { get; set; }

        public virtual Apartment Apartment { get; set; }
    }
}

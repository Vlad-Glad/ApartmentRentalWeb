using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace ApartmentRental.Models
{
    public class Apartment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage ="Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City {  get; set; }
        public string? FullAddress {  get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string LessorId { get; set; }

        public virtual ApplicationUser Lessor { get; set; }

        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    }
}

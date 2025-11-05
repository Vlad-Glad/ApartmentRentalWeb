using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

        [Required]
        public string FullAddress {  get; set; }
        public double? Latitude { get; set; } = null;
        public double? Longitude { get; set; } = null;

        [Required]
        public string LessorId { get; set; }

        [ValidateNever]
        public virtual ApplicationUser Lessor { get; set; }

        [ValidateNever]
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    }
}

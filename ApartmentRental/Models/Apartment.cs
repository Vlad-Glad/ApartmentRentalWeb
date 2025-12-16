using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentRental.Models
{
    public class Apartment
    {
        [Key]
        public int Id { get; set; }

        [StringLength(256, MinimumLength = 3)]
        [Required(ErrorMessage ="Title is required")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Price must be greater than zero.")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "City is required")]
        public required string City {  get; set; }

        [Required]
        public required string FullAddress {  get; set; }
        public double? Latitude { get; set; } = null;
        public double? Longitude { get; set; } = null;

        [Required]
        public required string LessorId { get; set; }

        [ValidateNever]
        public virtual ApplicationUser Lessor { get; set; }

        [ValidateNever]
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    }
}

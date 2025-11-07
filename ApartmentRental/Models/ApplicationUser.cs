using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ApartmentRental.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Url(ErrorMessage = "Invalid image URL.")]
        public string? ProfilePictureUrl { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email")]
        public override string? Email
        {
            get => base.Email;
            set => base.Email = value;
        }


        [Phone(ErrorMessage = "Invalid phone number.")]
        [Display(Name = "Phone Number")]
        public override string? PhoneNumber
        {
            get => base.PhoneNumber;
            set => base.PhoneNumber = value;
        }

        public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    }
}

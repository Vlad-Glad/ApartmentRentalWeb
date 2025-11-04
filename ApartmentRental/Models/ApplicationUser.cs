using Microsoft.AspNetCore.Identity;

namespace ApartmentRental.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    }
}

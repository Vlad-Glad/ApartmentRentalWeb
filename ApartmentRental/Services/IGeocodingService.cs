using ApartmentRental.Models.DTO;

namespace ApartmentRental.Services
{
    public interface IGeocodingService
    {
        Task<List<AddressSuggestionDto>> SearchAsync(string query, int limit = 5);
    }
}
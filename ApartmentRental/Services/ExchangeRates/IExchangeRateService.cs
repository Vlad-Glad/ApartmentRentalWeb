using ApartmentRental.Models.PrivatBank;

namespace ApartmentRental.Services.ExchangeRates
{
    public interface IExchangeRateService
    {
        Task<ExchangeRate?> GetRatesAsync(CancellationToken ct = default);
    }
}

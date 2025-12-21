using System.Globalization;
using System.Text.Json;
using System.Linq;
using ApartmentRental.Models.PrivatBank;
using Microsoft.Extensions.Caching.Memory;

namespace ApartmentRental.Services.ExchangeRates
{
    public sealed class PrivatBankExchangeRateService : IExchangeRateService
    {
        private const string Endpoint = "https://api.privatbank.ua/p24api/pubinfo?json&exchange&coursid=5";
        private const string CacheKey = "PrivatBank.ExchangeRates.Coursid5";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PrivatBankExchangeRateService> _logger;

        public PrivatBankExchangeRateService(
            HttpClient http,
            IMemoryCache cache,
            ILogger<PrivatBankExchangeRateService> logger)
        {
            _http = http;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ExchangeRate?> GetRatesAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey, out ExchangeRate cached))
                return cached;

            try
            {
                using var response = await _http.GetAsync(Endpoint, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var items = JsonSerializer.Deserialize<List<PrivatRateDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (items is null || items.Count == 0)
                    return null;

                decimal usdToUah = ReadRate(items, "USD", useSale: true);
                decimal eurToUah = ReadRate(items, "EUR", useSale: true);

                var result = new ExchangeRate
                {
                    UsdToUah = usdToUah,
                    EurToUah = eurToUah,
                    UpdatedAtUtc = DateTimeOffset.Now
                };

                _cache.Set(CacheKey, result, CacheTtl);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch PrivatBank exchange rates.");
                return _cache.TryGetValue(CacheKey, out ExchangeRate fallback) ? fallback : null;
            }
        }

        private static decimal ReadRate(List<PrivatRateDto> items, string currency, bool useSale)
        {
            var entry = items.FirstOrDefault(x =>
                string.Equals(x.Currency, currency, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.BaseCurrency, "UAH", StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                throw new InvalidOperationException($"Rate for {currency}/UAH not found.");

            var raw = useSale ? entry.Sale : entry.Buy;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException($"Rate value for {currency}/UAH is empty.");

            // Privat returns decimals with '.' — parse invariant.
            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"Cannot parse rate '{raw}' for {currency}/UAH.");

            return value;
        }
    }
}

using ApartmentRental.Services.ExchangeRates;
using ApartmentRental.Models.PrivatBank;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentRental.Services
{
    public class CurrencyWidgetViewComponent : ViewComponent
    {
        private readonly IExchangeRateService _rates;

        public CurrencyWidgetViewComponent(IExchangeRateService rates)
        {
            _rates = rates;
        }

        public async Task<IViewComponentResult> InvokeAsync(decimal priceUsd, CancellationToken ct = default)
        {
            var rates = await _rates.GetRatesAsync(ct);

            if (rates is null)
            {
                return View(new CurrencyWidgetViewModel
                {
                    PriceUsd = priceUsd,
                    Warning = "Rates are temporarily unavailable."
                });
            }

            var priceUah = Math.Round(priceUsd * rates.UsdToUah, 0, MidpointRounding.AwayFromZero);
            var priceEur = Math.Round(priceUsd * rates.UsdToUah / rates.EurToUah, 2, MidpointRounding.AwayFromZero);

            return View(new CurrencyWidgetViewModel
            {
                PriceUsd = priceUsd,
                PriceUah = priceUah,
                PriceEur = priceEur,
                UsdToUah = rates.UsdToUah,
                EurToUah = rates.EurToUah,
                UpdatedAtUtc = rates.UpdatedAtUtc
            });
        }
    }
}

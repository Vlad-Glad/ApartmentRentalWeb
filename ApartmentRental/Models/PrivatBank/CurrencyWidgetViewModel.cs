namespace ApartmentRental.Models.PrivatBank
{
    public sealed class CurrencyWidgetViewModel
    {
        public decimal PriceUsd { get; init; }

        public decimal? PriceUah { get; init; }
        public decimal? PriceEur { get; init; }

        public decimal? UsdToUah { get; init; }
        public decimal? EurToUah { get; init; }

        public DateTimeOffset? UpdatedAtUtc { get; init; }
        public string? Warning { get; init; }
    }
}

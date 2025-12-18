namespace ApartmentRental.Models.PrivatBank
{
    public sealed class ExchangeRate
    {
        public decimal UsdToUah { get; init; }
        public decimal EurToUah { get; init; }
        public DateTimeOffset UpdatedAtUtc { get; init; }
    }
}

namespace ApartmentRental.Models.DTO
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }

        public int Skip { get; set; }
        public int Limit { get; set; }

        public string? NextLink { get; set; }
    }
}

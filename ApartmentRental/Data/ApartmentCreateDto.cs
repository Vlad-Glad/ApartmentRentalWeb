namespace ApartmentRental.Data
{
    public class ApartmentCreateDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string City { get; set; }
        public string? FullAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Lessor id is allowed to be in json for now
        public string LessorId { get; set; }
    }
}

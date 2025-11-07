namespace ApartmentRental.Data
{
    public class ApartmentDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? City { get; set; }
        public string? FullAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string LessorId { get; set; }
        public string? LessorEmail { get; set; }
    }
}

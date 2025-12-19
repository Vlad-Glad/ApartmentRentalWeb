namespace ApartmentRental.Search;

public sealed class ApartmentSearchDocument
{
    public string Id { get; set; } = "";
    public int ApartmentId { get; set; }
    public string Title { get; set; } = "";
    public string LessorEmail { get; set; } = "";
}

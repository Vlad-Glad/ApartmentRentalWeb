namespace ApartmentRental.Services
{
    public interface IBlobService
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteAsync(string blobUrl);
    }
}

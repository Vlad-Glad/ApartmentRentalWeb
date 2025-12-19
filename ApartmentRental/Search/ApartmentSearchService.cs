using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;

namespace ApartmentRental.Search;

public sealed class AzureSearchOptions
{
    public string Endpoint { get; set; } = "";
    public string IndexName { get; set; } = "";
    public string AdminKey { get; set; } = "";
    public string QueryKey { get; set; } = "";
}

public interface IApartmentSearchService
{
    Task IndexAsync(ApartmentSearchDocument doc, CancellationToken ct = default);
    Task DeleteAsync(string docId, CancellationToken ct = default);
    Task<IReadOnlyList<ApartmentSearchDocument>> SearchAsync(string query, CancellationToken ct = default);
}

public sealed class ApartmentSearchService : IApartmentSearchService
{
    private readonly SearchClient _queryClient;
    private readonly SearchClient _adminClient;

    public ApartmentSearchService(IOptions<AzureSearchOptions> options)
    {
        var opt = options.Value;
        var endpoint = new Uri(opt.Endpoint);

        _queryClient = new SearchClient(endpoint, opt.IndexName, new AzureKeyCredential(opt.QueryKey));
        _adminClient = new SearchClient(endpoint, opt.IndexName, new AzureKeyCredential(opt.AdminKey));
    }

    public async Task IndexAsync(ApartmentSearchDocument doc, CancellationToken ct = default)
    {
        var batch = IndexDocumentsBatch.MergeOrUpload(new[] { doc });
        await _adminClient.IndexDocumentsAsync(batch, cancellationToken: ct);
    }

    public async Task DeleteAsync(string docId, CancellationToken ct = default)
    {
        var batch = IndexDocumentsBatch.Delete("id", new[] { docId });
        await _adminClient.IndexDocumentsAsync(batch, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<ApartmentSearchDocument>> SearchAsync(string query, CancellationToken ct = default)
    {
        var options = new SearchOptions
        {
            Size = 20
        };
        options.SearchFields.Add("title");
        options.SearchFields.Add("lessorEmail");

        // ensure fields are returned
        options.Select.Add("id");
        options.Select.Add("apartmentId");
        options.Select.Add("title");
        options.Select.Add("lessorEmail");

        var results = await _queryClient.SearchAsync<ApartmentSearchDocument>(query, options, ct);

        var list = new List<ApartmentSearchDocument>();
        await foreach (var r in results.Value.GetResultsAsync())
            list.Add(r.Document);

        return list;
    }
}

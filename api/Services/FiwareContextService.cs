using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DriveTraceCore.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public interface IFiwareContextService
{
    Task<IReadOnlyList<object>> BuildCurrentContextAsync();
    Task<object> PublishCurrentContextAsync();
}

public sealed class FiwareContextService : IFiwareContextService
{
    private readonly DriveTraceDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public FiwareContextService(DriveTraceDbContext db, HttpClient httpClient, IConfiguration configuration)
    {
        _db = db;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<object>> BuildCurrentContextAsync()
    {
        var contextUrl = _configuration["Fiware:ContextUrl"] ?? "https://uri.drivolution.local/context.jsonld";
        var supports = await _db.Supports.AsNoTracking().ToListAsync();
        var units = await _db.ProductUnits.AsNoTracking().ToListAsync();
        var sections = await _db.ProductionLineSections.AsNoTracking().ToListAsync();
        var racks = await _db.Racks.AsNoTracking().ToListAsync();

        var supportEntities = supports.Select(s => new
        {
            id = $"urn:ngsi-ld:Support:{s.SupportCode}",
            type = "Support",
            supportCode = new { type = "Property", value = s.SupportCode },
            status = new { type = "Property", value = s.Status },
            currentSection = new { type = "Relationship", @object = SectionUrn(sections.FirstOrDefault(x => x.Id == s.CurrentSectionId)?.SectionCode) },
            atContext = contextUrl
        });

        var unitEntities = units.Select(u => new
        {
            id = $"urn:ngsi-ld:ProductUnit:{u.UnitCode}",
            type = "ProductUnit",
            unitCode = new { type = "Property", value = u.UnitCode },
            unitType = new { type = "Property", value = u.UnitType },
            status = new { type = "Property", value = u.Status },
            qualityStatus = new { type = "Property", value = u.QualityStatus },
            currentSupport = new { type = "Relationship", @object = SupportUrn(supports.FirstOrDefault(x => x.Id == u.CurrentSupportId)?.SupportCode) },
            atContext = contextUrl
        });

        var rackEntities = racks.Select(r => new
        {
            id = $"urn:ngsi-ld:Rack:{r.RackCode}",
            type = "Rack",
            rackCode = new { type = "Property", value = r.RackCode },
            status = new { type = "Property", value = r.Status },
            atContext = contextUrl
        });

        return supportEntities.Cast<object>().Concat(unitEntities).Concat(rackEntities).ToList();
    }

    public async Task<object> PublishCurrentContextAsync()
    {
        var baseUrl = _configuration["Fiware:OrionLdBaseUrl"] ?? "http://localhost:1026/ngsi-ld/v1";
        var entities = await BuildCurrentContextAsync();
        var results = new List<object>();

        foreach (var entity in entities)
        {
            var json = JsonSerializer.Serialize(entity).Replace("\"atContext\"", "\"@context\"");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/entities");
            request.Content = new StringContent(json, Encoding.UTF8, "application/ld+json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/ld+json"));

            try
            {
                var response = await _httpClient.SendAsync(request);
                results.Add(new { entity = TryExtractId(json), statusCode = (int)response.StatusCode, response.IsSuccessStatusCode });
            }
            catch (Exception ex)
            {
                results.Add(new { entity = TryExtractId(json), statusCode = 0, isSuccessStatusCode = false, error = ex.Message });
            }
        }

        return new { publishedAt = DateTime.UtcNow, count = entities.Count, results };
    }

    private static string? TryExtractId(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetString();
        }
        catch
        {
            return null;
        }
    }

    private static string? SupportUrn(string? supportCode) => string.IsNullOrWhiteSpace(supportCode) ? null : $"urn:ngsi-ld:Support:{supportCode}";
    private static string? SectionUrn(string? sectionCode) => string.IsNullOrWhiteSpace(sectionCode) ? null : $"urn:ngsi-ld:ProductionLineSection:{sectionCode}";
}

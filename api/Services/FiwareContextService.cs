using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DriveTraceCore.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public interface IFiwareContextService
{
    Task<FiwareContextResponse> GetContextAsync(CancellationToken cancellationToken = default);
    Task<FiwarePublishResponse> PublishCurrentContextAsync(CancellationToken cancellationToken = default);
}

public sealed class FiwareEntityItem
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object?> Attributes { get; init; } = new Dictionary<string, object?>();
}

public sealed class FiwareContextResponse
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool BrokerReachable { get; init; }
    public string Source { get; init; } = "relational-fallback";
    public string Message { get; init; } = string.Empty;
    public int EntityCount { get; init; }
    public int RelationalSnapshotCount { get; init; }
    public string OrionLdBaseUrl { get; init; } = string.Empty;
    public IReadOnlyList<FiwareEntityItem> Entities { get; init; } = Array.Empty<FiwareEntityItem>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class FiwarePublishResponse
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool BrokerReachable { get; init; }
    public string Message { get; init; } = string.Empty;
    public int AttemptedCount { get; init; }
    public int PublishedCount { get; init; }
    public int FailedCount { get; init; }
    public int StaleDeletedCount { get; init; }
    public string OrionLdBaseUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> EntityIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> StaleEntityIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class FiwareContextService : IFiwareContextService
{
    private const string DefaultOrionLdBaseUrl = "http://localhost:1026/ngsi-ld/v1";
    private const string NgsiLdCoreContext = "https://uri.etsi.org/ngsi-ld/v1/ngsi-ld-core-context-v1.8.jsonld";
    private static readonly string[] ManagedEntityIdPatterns =
    [
        "urn:ngsi-ld:Support:.*",
        "urn:ngsi-ld:ProductUnit:.*",
        "urn:ngsi-ld:Rack:.*",
        "urn:ngsi-ld:ProductionLine:.*",
        "urn:ngsi-ld:ProductionLineSection:.*",
        "urn:ngsi-ld:Checkpoint:.*"
    ];

    private static readonly IReadOnlyDictionary<string, string> EmbeddedDomainContext = new Dictionary<string, string>
    {
        ["Support"] = "https://uri.drivolution.local/ns/Support",
        ["ProductUnit"] = "https://uri.drivolution.local/ns/ProductUnit",
        ["Rack"] = "https://uri.drivolution.local/ns/Rack",
        ["ProductionLine"] = "https://uri.drivolution.local/ns/ProductionLine",
        ["ProductionLineSection"] = "https://uri.drivolution.local/ns/ProductionLineSection",
        ["Checkpoint"] = "https://uri.drivolution.local/ns/Checkpoint",
        ["supportCode"] = "https://uri.drivolution.local/ns/supportCode",
        ["unitCode"] = "https://uri.drivolution.local/ns/unitCode",
        ["lineCode"] = "https://uri.drivolution.local/ns/lineCode",
        ["rackCode"] = "https://uri.drivolution.local/ns/rackCode",
        ["checkpointCode"] = "https://uri.drivolution.local/ns/checkpointCode",
        ["unitType"] = "https://uri.drivolution.local/ns/unitType",
        ["status"] = "https://uri.drivolution.local/ns/status",
        ["qualityStatus"] = "https://uri.drivolution.local/ns/qualityStatus",
        ["currentSupport"] = "https://uri.drivolution.local/ns/currentSupport",
        ["currentSection"] = "https://uri.drivolution.local/ns/currentSection",
        ["currentProductionLine"] = "https://uri.drivolution.local/ns/currentProductionLine",
        ["lastMovementAt"] = "https://uri.drivolution.local/ns/lastMovementAt",
        ["routeState"] = "https://uri.drivolution.local/ns/routeState",
        ["sectionCode"] = "https://uri.drivolution.local/ns/sectionCode",
        ["sectionType"] = "https://uri.drivolution.local/ns/sectionType",
        ["lineId"] = "https://uri.drivolution.local/ns/lineId",
        ["name"] = "https://uri.drivolution.local/ns/name"
    };

    private readonly DriveTraceDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FiwareContextService> _logger;

    public FiwareContextService(
        DriveTraceDbContext db,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FiwareContextService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FiwareContextResponse> GetContextAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = OrionLdBaseUrl();
        var localEntities = await BuildCurrentContextEntitiesAsync(cancellationToken);
        var localSummaries = localEntities.Select(ToEntityItem).ToList();

        var brokerErrors = new List<string>();
        var brokerEntities = await ReadBrokerEntitiesAsync(baseUrl, brokerErrors, cancellationToken);

        if (brokerEntities is not null)
        {
            var message = brokerEntities.Count == 0
                ? "Orion-LD acessível, mas ainda sem entidades FIWARE publicadas."
                : $"Foram obtidas {brokerEntities.Count} entidades FIWARE do Orion-LD.";

            return new FiwareContextResponse
            {
                Timestamp = DateTime.UtcNow,
                BrokerReachable = true,
                Source = "orion-ld",
                Message = message,
                EntityCount = brokerEntities.Count,
                RelationalSnapshotCount = localSummaries.Count,
                OrionLdBaseUrl = baseUrl,
                Entities = brokerEntities,
                Errors = brokerErrors
            };
        }

        _logger.LogWarning("Falling back to relational FIWARE snapshot because Orion-LD is unavailable. Errors: {Errors}", string.Join(" | ", brokerErrors));

        return new FiwareContextResponse
        {
            Timestamp = DateTime.UtcNow,
            BrokerReachable = false,
            Source = "relational-fallback",
            Message = "Orion-LD indisponível. A devolver snapshot relacional para manter a dashboard utilizável.",
            EntityCount = localSummaries.Count,
            RelationalSnapshotCount = localSummaries.Count,
            OrionLdBaseUrl = baseUrl,
            Entities = localSummaries,
            Errors = brokerErrors
        };
    }

    public async Task<FiwarePublishResponse> PublishCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = OrionLdBaseUrl();
        var entities = await BuildCurrentContextEntitiesAsync(cancellationToken);
        var entityIds = entities.Select(ExtractEntityId).Where(id => !string.IsNullOrWhiteSpace(id)).Cast<string>().ToList();
        var entityIdSet = entityIds.ToHashSet(StringComparer.Ordinal);
        var errors = new List<string>();
        var staleEntityIds = new List<string>();
        var staleDeletedCount = 0;

        var brokerEntitiesBeforePublish = await ReadBrokerEntitiesAsync(baseUrl, errors, cancellationToken);
        var staleCandidates = brokerEntitiesBeforePublish?
            .Select(item => item.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Where(id => !entityIdSet.Contains(id))
            .ToList() ?? new List<string>();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/entityOperations/upsert");
        request.Content = new StringContent(JsonSerializer.Serialize(entities), Encoding.UTF8, "application/ld+json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                staleDeletedCount = await DeleteStaleEntitiesAsync(baseUrl, staleCandidates, staleEntityIds, errors, cancellationToken);
                _logger.LogInformation(
                    "FIWARE publish succeeded. Status={StatusCode}, Count={Count}, StaleDeleted={StaleDeleted}",
                    (int)response.StatusCode,
                    entityIds.Count,
                    staleDeletedCount);

                var summaryMessage = staleDeletedCount > 0
                    ? $"Publicadas {entityIds.Count} entidades no Orion-LD e removidas {staleDeletedCount} entidades órfãs."
                    : $"Publicadas {entityIds.Count} entidades no Orion-LD.";

                return new FiwarePublishResponse
                {
                    Timestamp = DateTime.UtcNow,
                    BrokerReachable = true,
                    Message = summaryMessage,
                    AttemptedCount = entityIds.Count,
                    PublishedCount = entityIds.Count,
                    FailedCount = 0,
                    StaleDeletedCount = staleDeletedCount,
                    OrionLdBaseUrl = baseUrl,
                    EntityIds = entityIds,
                    StaleEntityIds = staleEntityIds,
                    Errors = errors
                };
            }

            var body = await SafeReadBodyAsync(response);
            var error = $"Orion-LD returned HTTP {(int)response.StatusCode} during publish." + (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Body: {body}");
            errors.Add(error);
            _logger.LogWarning("FIWARE publish failed. {Error}", error);

            return new FiwarePublishResponse
            {
                Timestamp = DateTime.UtcNow,
                BrokerReachable = response.StatusCode != HttpStatusCode.ServiceUnavailable,
                Message = "Não foi possível publicar contexto no Orion-LD. O modo relacional mantém-se disponível.",
                AttemptedCount = entityIds.Count,
                PublishedCount = 0,
                FailedCount = entityIds.Count,
                StaleDeletedCount = staleDeletedCount,
                OrionLdBaseUrl = baseUrl,
                EntityIds = Array.Empty<string>(),
                StaleEntityIds = staleEntityIds,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            var error = $"Não foi possível contactar o endpoint de publicação Orion-LD: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "FIWARE publish endpoint is unreachable.");

            return new FiwarePublishResponse
            {
                Timestamp = DateTime.UtcNow,
                BrokerReachable = false,
                Message = "Não foi possível contactar o endpoint de publicação Orion-LD. O modo relacional mantém-se disponível.",
                AttemptedCount = entityIds.Count,
                PublishedCount = 0,
                FailedCount = entityIds.Count,
                StaleDeletedCount = staleDeletedCount,
                OrionLdBaseUrl = baseUrl,
                EntityIds = Array.Empty<string>(),
                StaleEntityIds = staleEntityIds,
                Errors = errors
            };
        }
    }

    private async Task<List<FiwareEntityItem>?> ReadBrokerEntitiesAsync(
        string baseUrl,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var output = new List<FiwareEntityItem>();

        foreach (var pattern in ManagedEntityIdPatterns)
        {
            var requestUri = $"{baseUrl}/entities?idPattern={Uri.EscapeDataString(pattern)}&options=keyValues&limit=500";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to read Orion-LD entities for pattern '{pattern}': {ex.Message}");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response);
                errors.Add($"Orion-LD HTTP {(int)response.StatusCode} for pattern '{pattern}'." + (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Body: {body}"));
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json)) continue;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
                foreach (var entry in doc.RootElement.EnumerateArray())
                {
                    output.Add(ToEntityItem(entry));
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Invalid JSON from Orion-LD for pattern '{pattern}': {ex.Message}");
                return null;
            }
        }

        return output;
    }

    private async Task<int> DeleteStaleEntitiesAsync(
        string baseUrl,
        IReadOnlyList<string> staleCandidates,
        List<string> deletedStaleEntityIds,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        if (staleCandidates.Count == 0) return 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/entityOperations/delete");
        request.Content = new StringContent(JsonSerializer.Serialize(staleCandidates), Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add($"Falha ao remover entidades órfãs no Orion-LD: {ex.Message}");
            return 0;
        }

        if (response.IsSuccessStatusCode)
        {
            deletedStaleEntityIds.AddRange(staleCandidates);
            return staleCandidates.Count;
        }

        var body = await SafeReadBodyAsync(response);
        errors.Add(
            $"Orion-LD HTTP {(int)response.StatusCode} ao remover entidades órfãs."
            + (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Body: {body}"));
        return 0;
    }

    private async Task<List<Dictionary<string, object?>>> BuildCurrentContextEntitiesAsync(CancellationToken cancellationToken)
    {
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var units = await _db.ProductUnits.AsNoTracking().ToListAsync(cancellationToken);
        var lines = await _db.ProductionLines.AsNoTracking().ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().ToListAsync(cancellationToken);
        var checkpoints = await _db.Checkpoints.AsNoTracking().ToListAsync(cancellationToken);
        var racks = await _db.Racks.AsNoTracking().ToListAsync(cancellationToken);
        var movements = await _db.ProductUnitLocationHistory.AsNoTracking().ToListAsync(cancellationToken);

        var linesById = lines.ToDictionary(item => item.Id);
        var sectionsById = sections.ToDictionary(item => item.Id);
        var supportsById = supports.ToDictionary(item => item.Id);
        var latestMovementByUnit = movements
            .GroupBy(item => item.ProductUnitId)
            .ToDictionary(group => group.Key, group => group.Max(item => item.OccurredAt));
        var entities = new List<Dictionary<string, object?>>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.LineCode)) continue;
            var entity = CreateEntity($"urn:ngsi-ld:ProductionLine:{line.LineCode}", "ProductionLine");
            AddProperty(entity, "lineCode", line.LineCode);
            AddProperty(entity, "name", line.Name);
            AddProperty(entity, "status", DisplayStatus("Active"));
            entities.Add(entity);
        }

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.SectionCode)) continue;
            var entity = CreateEntity($"urn:ngsi-ld:ProductionLineSection:{section.SectionCode}", "ProductionLineSection");
            AddProperty(entity, "sectionCode", section.SectionCode);
            AddProperty(entity, "name", section.Name);
            AddProperty(entity, "sectionType", section.SectionType);
            if (section.LineId.HasValue) AddProperty(entity, "lineId", section.LineId.Value);
            entities.Add(entity);
        }

        foreach (var support in supports)
        {
            if (string.IsNullOrWhiteSpace(support.SupportCode)) continue;
            var entity = CreateEntity($"urn:ngsi-ld:Support:{support.SupportCode}", "Support");
            AddProperty(entity, "supportCode", support.SupportCode);
            AddProperty(entity, "status", DisplayStatus(support.Status));
            AddRelationship(entity, "currentSection", SectionUrn(sectionsById, support.CurrentSectionId));
            entities.Add(entity);
        }

        foreach (var unit in units)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitCode)) continue;
            var entity = CreateEntity($"urn:ngsi-ld:ProductUnit:{unit.UnitCode}", "ProductUnit");
            AddProperty(entity, "unitCode", unit.UnitCode);
            AddProperty(entity, "unitType", DisplayUnitType(unit.UnitType));
            AddProperty(entity, "status", DisplayStatus(unit.Status));
            AddProperty(entity, "qualityStatus", DisplayQualityStatus(unit.QualityStatus));
            var currentSection = unit.CurrentSectionId.HasValue && sectionsById.TryGetValue(unit.CurrentSectionId.Value, out var sectionValue)
                ? sectionValue
                : null;
            var currentLineId = currentSection?.LineId;
            if (latestMovementByUnit.TryGetValue(unit.Id, out var lastMovementAt))
            {
                AddProperty(entity, "lastMovementAt", lastMovementAt);
            }

            AddProperty(entity, "routeState", DisplayRouteState(RouteState(unit.Status, unit.QualityStatus, currentSection)));
            AddRelationship(entity, "currentSupport", SupportUrn(supportsById, unit.CurrentSupportId));
            AddRelationship(entity, "currentSection", SectionUrn(sectionsById, unit.CurrentSectionId));
            AddRelationship(entity, "currentProductionLine", LineUrn(linesById, currentLineId));
            entities.Add(entity);
        }

        foreach (var rack in racks)
        {
            if (string.IsNullOrWhiteSpace(rack.RackCode)) continue;
            var entity = CreateEntity($"urn:ngsi-ld:Rack:{rack.RackCode}", "Rack");
            AddProperty(entity, "rackCode", rack.RackCode);
            AddProperty(entity, "status", DisplayStatus(rack.Status));
            AddRelationship(entity, "currentSection", SectionUrn(sectionsById, rack.SectionId));
            entities.Add(entity);
        }

        foreach (var checkpoint in checkpoints)
        {
            if (string.IsNullOrWhiteSpace(checkpoint.CheckpointCode)) continue;
            var entity = CreateEntity($"urn:ngsi-ld:Checkpoint:{checkpoint.CheckpointCode}", "Checkpoint");
            AddProperty(entity, "checkpointCode", checkpoint.CheckpointCode);
            AddProperty(entity, "name", checkpoint.Name);
            AddProperty(entity, "status", DisplayStatus(checkpoint.Status));
            AddRelationship(entity, "currentSection", SectionUrn(sectionsById, checkpoint.SectionId));
            entities.Add(entity);
        }

        return entities;
    }

    private static FiwareEntityItem ToEntityItem(Dictionary<string, object?> entity)
    {
        var attributes = new Dictionary<string, object?>();
        foreach (var pair in entity)
        {
            if (pair.Key is "id" or "type" or "@context") continue;
            attributes[pair.Key] = NormalizeAttributeValue(pair.Value);
        }

        return new FiwareEntityItem
        {
            Id = Convert.ToString(entity.GetValueOrDefault("id")) ?? string.Empty,
            Type = NormalizeToken(Convert.ToString(entity.GetValueOrDefault("type")) ?? string.Empty),
            Attributes = attributes
        };
    }

    private static FiwareEntityItem ToEntityItem(JsonElement entry)
    {
        var id = entry.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
        var typeRaw = entry.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;
        var attributes = new Dictionary<string, object?>();

        foreach (var prop in entry.EnumerateObject())
        {
            if (prop.Name is "id" or "type" or "@context") continue;
            attributes[NormalizeToken(prop.Name)] = NormalizeJsonValue(prop.Value);
        }

        return new FiwareEntityItem
        {
            Id = id,
            Type = NormalizeToken(typeRaw),
            Attributes = attributes
        };
    }

    private Dictionary<string, object?> CreateEntity(string id, string type)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["type"] = type,
            ["@context"] = EmbeddedContext()
        };
    }

    private static void AddProperty(Dictionary<string, object?> entity, string key, object? value)
    {
        if (value is null) return;
        if (value is string text && string.IsNullOrWhiteSpace(text)) return;
        entity[key] = new Dictionary<string, object?>
        {
            ["type"] = "Property",
            ["value"] = value
        };
    }

    private static void AddRelationship(Dictionary<string, object?> entity, string key, string? referenceUrn)
    {
        if (string.IsNullOrWhiteSpace(referenceUrn)) return;
        entity[key] = new Dictionary<string, object?>
        {
            ["type"] = "Relationship",
            ["object"] = referenceUrn
        };
    }

    private static string? SupportUrn(IReadOnlyDictionary<int, DriveTraceCore.Api.Models.Support> supportsById, int? supportId)
    {
        if (!supportId.HasValue) return null;
        return supportsById.TryGetValue(supportId.Value, out var support) && !string.IsNullOrWhiteSpace(support.SupportCode)
            ? $"urn:ngsi-ld:Support:{support.SupportCode}"
            : null;
    }

    private static string? SectionUrn(IReadOnlyDictionary<int, DriveTraceCore.Api.Models.ProductionLineSection> sectionsById, int? sectionId)
    {
        if (!sectionId.HasValue) return null;
        return sectionsById.TryGetValue(sectionId.Value, out var section) && !string.IsNullOrWhiteSpace(section.SectionCode)
            ? $"urn:ngsi-ld:ProductionLineSection:{section.SectionCode}"
            : null;
    }

    private static string? LineUrn(IReadOnlyDictionary<int, DriveTraceCore.Api.Models.ProductionLine> linesById, int? lineId)
    {
        if (!lineId.HasValue) return null;
        return linesById.TryGetValue(lineId.Value, out var line) && !string.IsNullOrWhiteSpace(line.LineCode)
            ? $"urn:ngsi-ld:ProductionLine:{line.LineCode}"
            : null;
    }

    private static string RouteState(string status, string qualityStatus, DriveTraceCore.Api.Models.ProductionLineSection? section)
    {
        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Completed";
        if (status.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) return "Scrap";
        if (status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Rework", StringComparison.OrdinalIgnoreCase)
            || qualityStatus.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            return "Attention";
        }

        if (section is null) return "Unassigned";
        var sectionText = $"{section.SectionType} {section.Name}".ToLowerInvariant();
        if (sectionText.Contains("post-line") || sectionText.Contains("log") || sectionText.Contains("rack") || sectionText.Contains("expedition"))
        {
            return "PostLine";
        }

        return section.IsTransferPoint ? "TransferPoint" : "InLine";
    }

    private static string DisplayStatus(string status)
    {
        return status switch
        {
            "Active" => "Em produção",
            "In Progress" => "Em progresso",
            "Completed" => "Concluída",
            "Blocked" => "Bloqueada",
            "Rework" => "Em retrabalho",
            "Scrap" => "Sucata",
            "Stored" => "Armazenado",
            "Loaded" => "Carregado",
            "Available" => "Disponível",
            "Pending" => "Pendente",
            "Planned" => "Planeada",
            "Cancelled" => "Cancelada",
            "Open" => "Aberto",
            "Closed" => "Fechado",
            _ => status
        };
    }

    private static string DisplayQualityStatus(string qualityStatus)
    {
        return qualityStatus switch
        {
            "PASS" => "Aprovado",
            "FAIL" => "Reprovado",
            "Pending" => "Pendente",
            _ => DisplayStatus(qualityStatus)
        };
    }

    private static string DisplayUnitType(string unitType)
    {
        return unitType switch
        {
            "Subproduct" => "Subproduto",
            "Final" => "Final",
            _ => unitType
        };
    }

    private static string DisplayRouteState(string routeState)
    {
        return routeState switch
        {
            "Completed" => "Concluída",
            "Scrap" => "Sucata",
            "Attention" => "Atenção",
            "Unassigned" => "Sem atribuição",
            "PostLine" => "Pós-linha",
            "TransferPoint" => "Ponto de transferência",
            "InLine" => "Em linha",
            _ => routeState
        };
    }

    private static object[] EmbeddedContext()
    {
        return
        [
            NgsiLdCoreContext,
            EmbeddedDomainContext
        ];
    }

    private string OrionLdBaseUrl()
    {
        return (_configuration["Fiware:OrionLdBaseUrl"] ?? DefaultOrionLdBaseUrl).TrimEnd('/');
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var hashIndex = value.LastIndexOf('#');
        if (hashIndex >= 0 && hashIndex + 1 < value.Length) return value[(hashIndex + 1)..];
        var slashIndex = value.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex + 1 < value.Length) return value[(slashIndex + 1)..];
        return value;
    }

    private static object? NormalizeAttributeValue(object? value)
    {
        if (value is not Dictionary<string, object?> map) return value;
        if (map.TryGetValue("value", out var propertyValue)) return propertyValue;
        if (map.TryGetValue("object", out var relationshipValue)) return relationshipValue;
        return map;
    }

    private static object? NormalizeJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var longValue) ? longValue : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => NormalizeJsonObject(value),
            JsonValueKind.Array => JsonSerializer.Deserialize<List<object?>>(value.GetRawText()),
            _ => value.GetRawText()
        };
    }

    private static object? NormalizeJsonObject(JsonElement value)
    {
        if (value.TryGetProperty("value", out var propertyValue)) return NormalizeJsonValue(propertyValue);
        if (value.TryGetProperty("object", out var relationshipValue)) return NormalizeJsonValue(relationshipValue);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(value.GetRawText());
    }

    private static string? ExtractEntityId(Dictionary<string, object?> entity)
    {
        return entity.TryGetValue("id", out var value) ? Convert.ToString(value) : null;
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return string.Empty;
        }
    }
}

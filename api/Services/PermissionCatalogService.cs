using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Services;

public static class PermissionNames
{
    public const string UsersManage = "Users.Manage";
    public const string MasterDataManage = "MasterData.Manage";
    public const string OrdersView = "Orders.View";
    public const string OrdersManage = "Orders.Manage";
    public const string ProductUnitsView = "ProductUnits.View";
    public const string ProductUnitsTransfer = "ProductUnits.Transfer";
    public const string ProductUnitsTrace = "ProductUnits.Trace";
    public const string SupportsManage = "Supports.Manage";
    public const string QualityView = "Quality.View";
    public const string QualityRecord = "Quality.Record";
    public const string QualityDecide = "Quality.Decide";
    public const string RacksView = "Racks.View";
    public const string RacksManage = "Racks.Manage";
    public const string MaterialsView = "Materials.View";
    public const string MaterialsManage = "Materials.Manage";
    public const string GrafanaView = "Grafana.View";
    public const string FiwareView = "Fiware.View";
    public const string FiwareManage = "Fiware.Manage";
    public const string SimulationManage = "Simulation.Manage";
    public const string CustomerPortalView = "CustomerPortal.View";
    public const string OperationalEventsView = "OperationalEvents.View";

    public static readonly IReadOnlyList<string> All =
    [
        UsersManage,
        MasterDataManage,
        OrdersView,
        OrdersManage,
        ProductUnitsView,
        ProductUnitsTransfer,
        ProductUnitsTrace,
        SupportsManage,
        QualityView,
        QualityRecord,
        QualityDecide,
        RacksView,
        RacksManage,
        MaterialsView,
        MaterialsManage,
        GrafanaView,
        FiwareView,
        FiwareManage,
        SimulationManage,
        CustomerPortalView,
        OperationalEventsView
    ];
}

public static class RoleNames
{
    public const string Administrator = nameof(Administrator);
    public const string Supervisor = nameof(Supervisor);
    public const string Operator = nameof(Operator);
    public const string QualityTechnician = nameof(QualityTechnician);
    public const string Logistics = nameof(Logistics);
    public const string Customer = nameof(Customer);
    public const string DemoViewer = nameof(DemoViewer);

    public static readonly IReadOnlyList<string> All =
    [
        Administrator,
        Supervisor,
        Operator,
        QualityTechnician,
        Logistics,
        Customer,
        DemoViewer
    ];
}

public sealed class PermissionCatalogService
{
    public static readonly IReadOnlyList<DemoUserProfile> DemoUsers =
    [
        new("admin", "Administrador", "admin", RoleNames.Administrator, "admin@drivetrace.local", "Administração", null, null, null),
        new("supervisor", "Supervisor de Produção", "supervisor", RoleNames.Supervisor, "supervisor@drivetrace.local", "Produção", null, null, null),
        new("operador", "Operador Linha 1", "operador", RoleNames.Operator, "operador@drivetrace.local", "Linha 1", "LINHA-01", "SEC-SOLD", null),
        new("qualidade", "Técnico de Qualidade", "qualidade", RoleNames.QualityTechnician, "qualidade@drivetrace.local", "Qualidade", "LINHA-04", "SEC-CQ", null),
        new("logistica", "Responsável de Logística", "logistica", RoleNames.Logistics, "logistica@drivetrace.local", "Logística", "LINHA-04", "SEC-RACK", null),
        new("cliente", "Cliente Demo", "cliente", RoleNames.Customer, "cliente@drivetrace.local", "Cliente", null, null, "CLI-AUTO-001"),
        new("demo", "Visualizador Demo", "demo", RoleNames.DemoViewer, "demo@drivetrace.local", "Demonstração", null, null, null)
    ];

    private static readonly IReadOnlyDictionary<string, string> RoleAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = RoleNames.Administrator,
        ["administrator"] = RoleNames.Administrator,
        ["supervisor"] = RoleNames.Supervisor,
        ["operator"] = RoleNames.Operator,
        ["operador"] = RoleNames.Operator,
        ["quality"] = RoleNames.QualityTechnician,
        ["qualitytechnician"] = RoleNames.QualityTechnician,
        ["tecnicoqualidade"] = RoleNames.QualityTechnician,
        ["logistics"] = RoleNames.Logistics,
        ["logistica"] = RoleNames.Logistics,
        ["client"] = RoleNames.Customer,
        ["cliente"] = RoleNames.Customer,
        ["customer"] = RoleNames.Customer,
        ["demo"] = RoleNames.DemoViewer,
        ["demoviewer"] = RoleNames.DemoViewer
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> RolePermissions = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        [RoleNames.Administrator] = PermissionNames.All.ToHashSet(StringComparer.OrdinalIgnoreCase),
        [RoleNames.Supervisor] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionNames.OrdersView,
            PermissionNames.OrdersManage,
            PermissionNames.ProductUnitsView,
            PermissionNames.ProductUnitsTransfer,
            PermissionNames.ProductUnitsTrace,
            PermissionNames.SupportsManage,
            PermissionNames.QualityView,
            PermissionNames.RacksView,
            PermissionNames.RacksManage,
            PermissionNames.MaterialsView,
            PermissionNames.GrafanaView,
            PermissionNames.OperationalEventsView,
            PermissionNames.SimulationManage
        },
        [RoleNames.Operator] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionNames.OrdersView,
            PermissionNames.ProductUnitsView,
            PermissionNames.ProductUnitsTransfer,
            PermissionNames.ProductUnitsTrace,
            PermissionNames.OperationalEventsView
        },
        [RoleNames.QualityTechnician] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionNames.OrdersView,
            PermissionNames.ProductUnitsView,
            PermissionNames.ProductUnitsTrace,
            PermissionNames.QualityView,
            PermissionNames.QualityRecord,
            PermissionNames.QualityDecide,
            PermissionNames.OperationalEventsView
        },
        [RoleNames.Logistics] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionNames.ProductUnitsView,
            PermissionNames.ProductUnitsTrace,
            PermissionNames.RacksView,
            PermissionNames.RacksManage,
            PermissionNames.MaterialsView,
            PermissionNames.MaterialsManage,
            PermissionNames.SupportsManage,
            PermissionNames.OperationalEventsView
        },
        [RoleNames.Customer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionNames.CustomerPortalView
        },
        [RoleNames.DemoViewer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionNames.OrdersView,
            PermissionNames.ProductUnitsView,
            PermissionNames.ProductUnitsTrace,
            PermissionNames.QualityView,
            PermissionNames.RacksView,
            PermissionNames.MaterialsView,
            PermissionNames.FiwareView,
            PermissionNames.GrafanaView,
            PermissionNames.CustomerPortalView,
            PermissionNames.OperationalEventsView
        }
    };

    public object GetCatalog(string? requestedRole = null)
    {
        var activeRole = ResolveRole(requestedRole);
        return new
        {
            roles = RoleNames.All.Select(role => new
            {
                key = role,
                label = RoleLabel(role),
                permissions = RolePermissions.TryGetValue(role, out var permissions) ? permissions.OrderBy(x => x) : Enumerable.Empty<string>()
            }),
            permissions = PermissionNames.All.Select(permission => new
            {
                key = permission,
                label = PermissionLabel(permission)
            }),
            activeRole,
            activePermissions = GetPermissionsForRole(activeRole).OrderBy(x => x)
        };
    }

    public static DemoUserProfile ResolveDemoUser(HttpContext context)
    {
        var username = context.Request.Headers["X-DriveTrace-User"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = DemoUsers.FirstOrDefault(item => item.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user is not null) return user;
        }

        var role = ResolveRole(context);
        return DemoUsers.FirstOrDefault(item => item.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
            ?? DemoUsers[0];
    }

    public static string ToRoleKey(string role)
    {
        return role switch
        {
            RoleNames.Administrator => "admin",
            RoleNames.Supervisor => "supervisor",
            RoleNames.Operator => "operator",
            RoleNames.QualityTechnician => "quality",
            RoleNames.Logistics => "logistics",
            RoleNames.Customer => "client",
            RoleNames.DemoViewer => "demoViewer",
            _ => "demoViewer"
        };
    }

    public static bool HasPermission(HttpContext context, string permission)
    {
        var activePermissions = ResolvePermissions(context);
        return activePermissions.Contains(permission);
    }

    public static string ResolveRole(HttpContext context)
    {
        var headerRole = context.Request.Headers["X-DriveTrace-Role"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerRole))
        {
            return ResolveRole(headerRole);
        }

        var username = context.Request.Headers["X-DriveTrace-User"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = DemoUsers.FirstOrDefault(item => item.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user is not null) return user.Role;
        }

        return ResolveRole((string?)null);
    }

    public static string ResolveRole(string? requestedRole)
    {
        if (string.IsNullOrWhiteSpace(requestedRole))
        {
            return RoleNames.Administrator;
        }

        var clean = requestedRole.Trim();
        return RoleAliases.TryGetValue(clean.Replace("-", string.Empty).Replace("_", string.Empty), out var role)
            ? role
            : RolePermissions.ContainsKey(clean)
                ? clean
                : RoleNames.DemoViewer;
    }

    public static IReadOnlySet<string> GetPermissionsForRole(string role)
    {
        return RolePermissions.TryGetValue(role, out var permissions)
            ? permissions
            : RolePermissions[RoleNames.DemoViewer];
    }

    public static IReadOnlySet<string> ResolvePermissions(HttpContext context)
    {
        var role = ResolveRole(context);
        var values = new HashSet<string>(GetPermissionsForRole(role), StringComparer.OrdinalIgnoreCase);
        var explicitPermissions = context.Request.Headers["X-DriveTrace-Permissions"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitPermissions))
        {
            foreach (var permission in explicitPermissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (PermissionNames.All.Contains(permission, StringComparer.OrdinalIgnoreCase))
                {
                    values.Add(permission);
                }
            }
        }

        return values;
    }

    public static ObjectResult Forbidden(string permission)
    {
        return new ObjectResult(new
        {
            error = "Não tem permissão para executar esta operação.",
            requiredPermission = permission
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    private static string RoleLabel(string role)
    {
        return role switch
        {
            RoleNames.Administrator => "Administrador",
            RoleNames.Supervisor => "Supervisor",
            RoleNames.Operator => "Operador",
            RoleNames.QualityTechnician => "Técnico de qualidade",
            RoleNames.Logistics => "Logística",
            RoleNames.Customer => "Cliente",
            RoleNames.DemoViewer => "Visualizador demo",
            _ => role
        };
    }

    private static string PermissionLabel(string permission)
    {
        return permission switch
        {
            PermissionNames.UsersManage => "Gerir utilizadores",
            PermissionNames.MasterDataManage => "Gerir dados mestre",
            PermissionNames.OrdersView => "Ver ordens",
            PermissionNames.OrdersManage => "Gerir ordens",
            PermissionNames.ProductUnitsView => "Ver unidades",
            PermissionNames.ProductUnitsTransfer => "Transferir unidades",
            PermissionNames.ProductUnitsTrace => "Ver trace",
            PermissionNames.SupportsManage => "Gerir suportes",
            PermissionNames.QualityView => "Ver qualidade",
            PermissionNames.QualityRecord => "Registar qualidade",
            PermissionNames.QualityDecide => "Decidir qualidade",
            PermissionNames.RacksView => "Ver racks",
            PermissionNames.RacksManage => "Gerir racks",
            PermissionNames.MaterialsView => "Ver materiais",
            PermissionNames.MaterialsManage => "Gerir materiais",
            PermissionNames.GrafanaView => "Ver Grafana",
            PermissionNames.FiwareView => "Ver FIWARE",
            PermissionNames.FiwareManage => "Gerir FIWARE",
            PermissionNames.SimulationManage => "Gerir simulação",
            PermissionNames.CustomerPortalView => "Ver portal de cliente",
            PermissionNames.OperationalEventsView => "Ver eventos operacionais",
            _ => permission
        };
    }
}

public sealed record DemoUserProfile(
    string Username,
    string Name,
    string Password,
    string Role,
    string Email,
    string Department,
    string? LineCode,
    string? SectionCode,
    string? CustomerCode);

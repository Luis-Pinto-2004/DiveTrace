using DriveTraceCore.Api.Services;
using Xunit;

namespace DriveTraceCore.Api.Tests;

/// <summary>
/// Testes unitários de lógica pura de perfis e permissões.
/// Não dependem de base de dados nem de HttpContext.
/// </summary>
public class PermissionCatalogServiceTests
{
    [Theory]
    [InlineData(RoleNames.Administrator, "admin")]
    [InlineData(RoleNames.Supervisor, "supervisor")]
    [InlineData(RoleNames.Operator, "operator")]
    [InlineData(RoleNames.QualityTechnician, "quality")]
    [InlineData(RoleNames.Logistics, "logistics")]
    [InlineData(RoleNames.Customer, "client")]
    public void ToRoleKey_MapeiaPapelParaChaveDeFrontend(string role, string expectedKey)
    {
        Assert.Equal(expectedKey, PermissionCatalogService.ToRoleKey(role));
    }

    [Fact]
    public void ToRoleKey_PapelDesconhecido_DevolveDemoViewer()
    {
        Assert.Equal("demoViewer", PermissionCatalogService.ToRoleKey("Inexistente"));
    }

    [Fact]
    public void ResolveRole_Vazio_DevolveAdministradorPorDefeito()
    {
        Assert.Equal(RoleNames.Administrator, PermissionCatalogService.ResolveRole((string?)null));
        Assert.Equal(RoleNames.Administrator, PermissionCatalogService.ResolveRole(""));
    }

    [Fact]
    public void ResolveRole_PapelValido_EhPreservado()
    {
        Assert.Equal(RoleNames.QualityTechnician, PermissionCatalogService.ResolveRole(RoleNames.QualityTechnician));
    }

    [Fact]
    public void GetPermissionsForRole_Administrador_TemGestaoDeDadosMestre()
    {
        var permissions = PermissionCatalogService.GetPermissionsForRole(RoleNames.Administrator);
        Assert.Contains(PermissionNames.MasterDataManage, permissions);
        Assert.Contains(PermissionNames.UsersManage, permissions);
    }

    [Fact]
    public void GetPermissionsForRole_Cliente_NaoTemPermissoesInternas()
    {
        var permissions = PermissionCatalogService.GetPermissionsForRole(RoleNames.Customer);
        Assert.DoesNotContain(PermissionNames.MasterDataManage, permissions);
        Assert.DoesNotContain(PermissionNames.SimulationRun, permissions);
    }

    [Fact]
    public void GetPermissionsForRole_PapelDesconhecido_RecorreAoDemoViewer()
    {
        var unknown = PermissionCatalogService.GetPermissionsForRole("Inexistente");
        var demo = PermissionCatalogService.GetPermissionsForRole(RoleNames.DemoViewer);
        Assert.Equal(demo, unknown);
    }
}

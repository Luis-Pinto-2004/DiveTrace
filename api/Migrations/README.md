# DriveTrace Core migrations

This V1 uses Entity Framework Core `EnsureCreated` for a clean academic demo database and automatic seed insertion at API startup.

For a stricter migration workflow, run these commands after installing the EF CLI:

```bash
cd api
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialDriveTraceSchema
dotnet ef database update
```

The domain model, relationships and indexes are defined in `Data/DriveTraceDbContext.cs`.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DriveTraceCore.Api.csproj ./
RUN dotnet restore DriveTraceCore.Api.csproj
COPY . ./
RUN dotnet publish DriveTraceCore.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "DriveTraceCore.Api.dll"]

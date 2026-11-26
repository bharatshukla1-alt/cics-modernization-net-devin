# Multi-stage build; runtime runs as the non-root "app" user shipped in the .NET images.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/UserAdminService.Api/UserAdminService.Api.csproj src/UserAdminService.Api/
RUN dotnet restore src/UserAdminService.Api/UserAdminService.Api.csproj
COPY src/ src/
RUN dotnet publish src/UserAdminService.Api/UserAdminService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
USER $APP_UID
EXPOSE 5002
ENTRYPOINT ["dotnet", "UserAdminService.Api.dll"]

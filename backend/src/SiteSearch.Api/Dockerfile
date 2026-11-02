FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore dependencies
COPY src/SiteSearch.Api/SiteSearch.Api.csproj src/SiteSearch.Api/
RUN dotnet restore src/SiteSearch.Api/SiteSearch.Api.csproj

# Copy code and build
COPY src/SiteSearch.Api/ src/SiteSearch.Api/
RUN dotnet publish src/SiteSearch.Api/SiteSearch.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "SiteSearch.Api.dll"]

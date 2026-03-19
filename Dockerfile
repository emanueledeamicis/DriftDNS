FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY DriftDNS.slnx .
COPY src/DriftDNS.Core/DriftDNS.Core.csproj src/DriftDNS.Core/
COPY src/DriftDNS.Infrastructure/DriftDNS.Infrastructure.csproj src/DriftDNS.Infrastructure/
COPY src/DriftDNS.Providers.Route53/DriftDNS.Providers.Route53.csproj src/DriftDNS.Providers.Route53/
COPY src/DriftDNS.Providers.Cloudflare/DriftDNS.Providers.Cloudflare.csproj src/DriftDNS.Providers.Cloudflare/
COPY src/DriftDNS.App/DriftDNS.App.csproj src/DriftDNS.App/
COPY tests/DriftDNS.Tests/DriftDNS.Tests.csproj tests/DriftDNS.Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish src/DriftDNS.App/DriftDNS.App.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data && \
    adduser --disabled-password --gecos '' appuser && \
    chown -R appuser /app

COPY --from=build /app/publish .
RUN chown -R appuser /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DatabasePath=/app/data/app.db

USER appuser

EXPOSE 8080

VOLUME /app/data

ENTRYPOINT ["dotnet", "DriftDNS.App.dll"]

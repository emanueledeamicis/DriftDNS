# DriftDNS

DriftDNS is a self-hosted **Dynamic DNS manager** designed for homelab users and developers running services behind a dynamic public IP.

It automatically detects your public IP address and updates DNS records on supported providers — all through a simple web dashboard.

---

## Why DriftDNS?

Many people run services from home networks (Home Assistant, Plex, VPN servers, self-hosted apps) but most ISPs assign **dynamic public IP addresses**, meaning DNS records must be updated every time the IP changes.

DriftDNS solves this by automatically:

1. Detecting your current public IP
2. Comparing it with the last known value
3. Updating your DNS records when the IP changes

---

## Features

- Self-hosted, single container
- Web dashboard
- Automatic IP detection and DNS updates
- Configurable sync interval
- Sync logs with configurable retention
- Manual sync trigger
- Provider abstraction layer

---

## Supported DNS Providers

| Provider | Status |
|---|---|
| AWS Route53 | ✓ Supported |
| Cloudflare | ✓ Supported |
| Azure DNS | Planned |
| Google Cloud DNS | Planned |

---

## Quick Start

### Docker Compose (recommended)

```yaml
services:
  driftdns:
    image: catokx/driftdns:latest
    container_name: driftdns
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - driftdns-data:/app/data

volumes:
  driftdns-data:
```

```bash
docker compose up -d
```

### Docker Run

```bash
docker run -d \
  --name driftdns \
  --restart unless-stopped \
  -p 8080:8080 \
  -v driftdns-data:/app/data \
  catokx/driftdns:latest
```

Then open: **http://localhost:8080**

---

## Configuration

| Environment Variable | Default | Description |
|---|---|---|
| `DatabasePath` | `/app/data/app.db` | Path to the SQLite database file |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening address |

Sync interval and log retention can be configured directly from the Settings page in the dashboard.

---

## Updating

**Docker Compose:**
```bash
docker compose pull
docker compose up -d
```

**Docker Run:**
```bash
docker pull catokx/driftdns:latest
docker stop driftdns
docker rm driftdns
docker run -d \
  --name driftdns \
  --restart unless-stopped \
  -p 8080:8080 \
  -v driftdns-data:/app/data \
  catokx/driftdns:latest
```

The named volume `driftdns-data` persists across updates, so your data is safe.

---

## Architecture

Built with:

- **ASP.NET Core 9** + **Blazor Server**
- **Entity Framework Core** + **SQLite**

```
src/
  DriftDNS.App                 # Blazor Server application
  DriftDNS.Core                # Models and interfaces
  DriftDNS.Infrastructure      # EF Core, services
  DriftDNS.Providers.Route53   # AWS Route53 provider

tests/
  DriftDNS.Tests               # NUnit test suite
```

---

## Development

Requirements: **.NET 9**, **Docker**

```bash
dotnet run --project src/DriftDNS.App
```

---

## Roadmap

- Azure DNS provider
- Google Cloud DNS provider
- IPv6 support
- Notification system
- Metrics and monitoring

---

## License

MIT License

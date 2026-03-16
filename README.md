# DriftDNS

DriftDNS is a self-hosted **Dynamic DNS manager** designed for homelab users and developers running services behind a dynamic public IP.

It automatically detects your public IP address and updates DNS records on supported DNS providers.

DriftDNS includes a **web dashboard**, **background synchronization engine**, and a **provider abstraction system** for supporting multiple DNS providers.

---

# Why DriftDNS?

Many people run services from home networks:

- Home Assistant
- Plex
- VPN servers
- self-hosted apps
- NAS services

However most ISPs assign **dynamic public IP addresses**.

This means your domain DNS records must be updated every time your IP changes.

DriftDNS solves this problem by automatically:

1. Detecting your current public IP
2. Comparing it with the last known value
3. Updating your DNS records when the IP changes

All through a **simple web dashboard**.

---

# Features

- self-hosted
- Docker friendly
- web dashboard
- automatic IP detection
- automatic DNS updates
- provider abstraction layer
- synchronization logs
- manual sync trigger

---

# Planned DNS Providers

Initial support:

- AWS Route53

Planned providers:

- Cloudflare
- Azure DNS
- Google Cloud DNS
- DuckDNS

---

# Architecture

DriftDNS is built using:

- **ASP.NET Core**
- **Blazor Server**
- **SQLite**
- **Entity Framework Core**

The application runs as a **single container service** containing:

- Web dashboard
- Background sync worker
- DNS provider integrations
- SQLite database

---

# Quick Start (Docker)

Example docker run:

```bash
docker run -d \
  -p 8080:8080 \
  -v driftdns_data:/app/data \
  ghcr.io/your-org/driftdns

Then open:

http://localhost:8080

Configuration

Important environment variables:

DRIFTDNS__SYNC_INTERVAL_MINUTES=5
DRIFTDNS__ENCRYPTION_KEY=your-secret-key

Project Structure
src/

  DriftDNS.App
  DriftDNS.Core
  DriftDNS.Infrastructure
  DriftDNS.Providers.Route53
  DriftDNS.Providers.Cloudflare

tests/

  DriftDNS.Tests

  Development

Requirements:

.NET 8 or .NET 9

Docker

Node is NOT required

Run locally:

dotnet run --project src/DriftDNS.App

Roadmap

Planned features:

Cloudflare provider

IPv6 support

notification system

metrics and monitoring

provider plugin system

Contributing

Contributions are welcome.

Before implementing features please read:

/docs/AI_PROJECT_BRIEF.md
/docs/ARCHITECTURE.md
/docs/INITIAL_TASKS.md

License

MIT License
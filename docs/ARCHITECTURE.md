# Architecture – Dynamic DNS Manager

## Overview

The application is a self-hosted Dynamic DNS manager built with:

- ASP.NET Core
- Blazor Server
- SQLite
- Entity Framework Core

The system runs as a **single container application** containing:

- Web UI
- API
- Background sync worker
- DNS provider integrations

---

# High Level Architecture

User Browser  
↓  
Blazor Server UI  
↓  
Application Services  
↓  
Domain Services  
↓  
DNS Provider Abstraction  
↓  
Provider Implementation (Route53 / Cloudflare)

Background Worker runs periodically and uses the same services.

---

# Application Layers

## UI Layer

Technology: Blazor Server

Responsibilities:

- configuration UI
- monitoring UI
- logs
- manual sync

UI communicates with application services via dependency injection.

---

## Application Layer

Responsibilities:

- orchestration logic
- use cases
- coordination between services

Examples:

- SyncDnsService
- EndpointManagementService
- ProviderManagementService

---

## Domain Layer

Contains:

- domain models
- interfaces
- business rules

Examples:

ProviderAccount  
DnsEndpoint  
SyncState  
SyncLog  

Interfaces:

IDnsProvider  
IPublicIpResolver  
IDnsSyncService  

---

## Infrastructure Layer

Responsibilities:

- EF Core database access
- provider implementations
- HTTP clients
- encryption utilities

---

# DNS Sync Flow

BackgroundWorker  
↓  
PublicIpResolver  
↓  
Load Enabled Endpoints  
↓  
Compare Last Applied IP  
↓  
If changed → call DNS Provider  
↓  
Update database state  
↓  
Write log entry  

---

# Provider System

Providers must implement:

IDnsProvider

Providers are selected dynamically based on ProviderType.

Example providers:

Route53Provider  
CloudflareProvider  

The system must allow adding new providers without modifying core logic.

---

# Database

Database: SQLite

Tables:

- ProviderAccounts
- DnsEndpoints
- SyncStates
- SyncLogs

---

# Background Worker

Implementation:

BackgroundService

Responsibilities:

- periodic sync
- error handling
- logging

Default interval:

5 minutes

Configurable via environment variable.

---

# Security

Credentials must be encrypted.

Use:

ASP.NET Core Data Protection

Encryption key provided via environment variable.

---

# Container Architecture

Single container includes:

- ASP.NET runtime
- Blazor UI
- Worker
- SQLite database

Database path:

/app/data/app.db

Mounted as Docker volume.

---

# Future Extensions

Possible future additions:

- additional DNS providers
- IPv6 support
- notification system
- Prometheus metrics
- API tokens
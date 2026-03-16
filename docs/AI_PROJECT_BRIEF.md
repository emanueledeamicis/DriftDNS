AI Coding Brief – Dynamic DNS Manager
Project Overview

This project is an open-source self-hosted Dynamic DNS manager designed for users who run services on networks with dynamic public IP addresses (e.g. home servers, homelabs, NAS, self-hosted services).

The application periodically detects the public IP of the host machine and updates configured DNS records on supported DNS providers.

The application includes:

a web dashboard

a background synchronization engine

DNS provider integrations

persistent configuration storage

logging and monitoring of updates

The entire application runs in a single Docker container.

Core Goals

Primary goals:

Detect public IP address

Compare it with last known value

Update DNS records if IP changed

Provide a web UI to configure everything

Support multiple DNS providers

Run easily in Docker for self-hosting

The project should be designed so that new DNS providers can be added easily.

Target Users

Typical users:

homelab owners

developers running services at home

people exposing services like:

Home Assistant

Plex

VPN

Nextcloud

self-hosted apps

They typically have:

dynamic ISP IP

their own domain

DNS hosted on providers like Route53 or Cloudflare.

Technology Stack

The application must use the following stack.

Backend:

.NET 8 or .NET 9

ASP.NET Core

Blazor Server

Database:

SQLite

Entity Framework Core

Architecture:

Single ASP.NET Core application

Blazor UI + API + background worker inside same process

Deployment:

Docker

single container

SQLite database stored in mounted volume

High Level Architecture

The system consists of these main parts:

1. Web UI

Blazor Server dashboard.

Features:

manage DNS providers

manage DNS records

view synchronization logs

see current detected public IP

trigger manual sync

2. Background Worker

A BackgroundService periodically runs a synchronization process.

Responsibilities:

resolve public IP

check configured DNS endpoints

compare IP with last applied value

update DNS if necessary

write logs

3. Provider System

DNS providers must be implemented through an abstraction layer.

Initial providers:

AWS Route53

Cloudflare (future milestone)

4. Database

SQLite database storing:

provider accounts

DNS endpoints

sync state

logs

Solution Structure

The solution should follow this structure.

src/

  DynamicDns.App
      ASP.NET Core host
      Blazor UI
      API controllers
      background worker

  DynamicDns.Core
      domain models
      interfaces
      core services

  DynamicDns.Infrastructure
      EF Core
      repositories
      database configuration

  DynamicDns.Providers.Route53
      AWS Route53 implementation

  DynamicDns.Providers.Cloudflare
      Cloudflare provider (future)

tests/

  DynamicDns.Tests
Core Domain Concepts
ProviderAccount

Represents credentials for a DNS provider.

Fields:

Id

Name

ProviderType

EncryptedCredentials

IsEnabled

CreatedAt

ProviderType examples:

Route53

Cloudflare

DnsEndpoint

Represents a DNS record to keep updated.

Fields:

Id

Hostname

ZoneName

RecordType (A or AAAA)

ProviderAccountId

TTL

Enabled

CreatedAt

Example:

Hostname:

home.example.com
SyncState

Stores last known sync status.

Fields:

EndpointId

LastKnownPublicIp

LastAppliedIp

LastCheckAt

LastSuccessAt

LastError

SyncLog

Historical logs.

Fields:

Id

EndpointId

Timestamp

OldIp

NewIp

Action

Details

Action values:

NoChange

Updated

Failed

Provider Abstraction

All DNS providers must implement this interface.

public interface IDnsProvider
{
    string Name { get; }

    Task ValidateCredentialsAsync(
        ProviderAccount account,
        CancellationToken cancellationToken);

    Task UpsertRecordAsync(
        ProviderAccount account,
        DnsEndpoint endpoint,
        string ipAddress,
        CancellationToken cancellationToken);
}

The system will resolve the correct provider based on ProviderType.

Public IP Detection

The application must detect the current public IP.

Primary methods:

HTTP services such as:

https://checkip.amazonaws.com

https://api.ipify.org

Implementation rules:

try multiple sources

validate returned value

support IPv4 initially

IPv6 support later

Service interface:

IPublicIpResolver
Sync Process

The background worker executes periodically.

Flow:

resolve public IP

load enabled DNS endpoints

for each endpoint:

check last applied IP

if IP changed → update DNS provider

save sync result

write logs

Background Worker

Implementation:

Use:

BackgroundService

Configurable interval.

Default:

5 minutes

Environment variable should allow override.

Security

Credentials must never be stored in plain text.

Implement:

encryption using ASP.NET Core Data Protection

encryption key provided through environment variable

Example env variable:

APP_ENCRYPTION_KEY
Configuration

Configuration sources:

appsettings.json

environment variables

Docker environment

Important settings:

SyncIntervalMinutes
EncryptionKey
DatabasePath
Blazor UI Pages

The dashboard should include the following pages.

Overview

Shows:

current detected public IP

number of endpoints

last sync time

system status

DNS Endpoints

CRUD page.

Fields:

hostname

provider

record type

TTL

enabled

Providers

Manage provider accounts.

Fields depend on provider.

Route53 example:

AccessKey

SecretKey

Region

Logs

Table of recent synchronization logs.

Columns:

timestamp

endpoint

action

message

API

Expose minimal REST API for automation.

Endpoints example:

GET /api/status
POST /api/sync
GET /api/endpoints
POST /api/endpoints
Docker Requirements

The application must run inside a single container.

SQLite database stored in volume.

Example structure:

/app
/app/data
/app/data/app.db

Container port:

8080
Code Quality Guidelines

The coding agent must follow these rules.

Use dependency injection everywhere.

Keep services small and testable.

Prefer interfaces for all services.

Avoid large classes.

Use async methods.

Log important operations.

Logging

Use built-in ASP.NET Core logging.

Log:

sync start

sync success

sync failure

provider API calls

First Implementation Milestone

Initial MVP must include:

SQLite database

EF Core migrations

Blazor dashboard

public IP detection

background worker

Route53 provider

endpoint CRUD

manual sync button

logging page

Dockerfile

Cloudflare support can be added later.

Future Roadmap

Future features:

Cloudflare provider

IPv6 support

notifications (email / Telegram)

Prometheus metrics

API tokens

provider plugin system

Design Principles

This project must prioritize:

simplicity

self-hosting

reliability

clean architecture

extensibility for DNS providers

Avoid unnecessary complexity.

The goal is a robust self-hosted dynamic DNS controller.

Important Development Rule

Always prioritize:

clear architecture

maintainable code

extensibility

over premature optimization.

End of Brief